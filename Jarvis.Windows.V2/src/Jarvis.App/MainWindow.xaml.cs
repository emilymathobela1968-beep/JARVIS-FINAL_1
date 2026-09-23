using Jarvis.AppBuilder;
using System.Text.Json;
using Jarvis.Voice;
using Jarvis.Realtime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Windows.System;
using Windows.UI.Core;

namespace Jarvis.App;

public sealed partial class MainWindow : Window
{
    private enum DesktopState
    {
        Offline,
        Starting,
        Listening,
        UserSpeaking,
        Thinking,
        Speaking,
        Interrupting,
        Recovering,
        Faulted
    }

    private readonly DesktopLifecycleLog lifecycleLog = new(Path.Combine(AppContext.BaseDirectory, "logs", "desktop-lifecycle.jsonl"));
    private readonly DesktopVoiceRuntime voiceRuntime;
    private readonly AppBuilderService appBuilder;
    private readonly JarvisUiContextProvider uiContext = new();
    private AppBuilderProject? builderProject;
    private MockArtifact? builderCandidate;
    private ActiveBuilderSelection? builderSelection;
    private DesktopState state = DesktopState.Offline;
    private bool voiceRunning;
    private bool developerTaskRunning;
    private bool realtimeRunning;
    private bool realtimeFoundationStarted;
    private BarehandsWindow? barehandsWindow;
    private JarvisWorkspace activeWorkspace = JarvisWorkspace.HOME;

    public MainWindow()
    {
        InitializeComponent();
        Title = "JARVIS V2";
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1180, 720));
        appBuilder = new AppBuilderService(new AppBuilderStore(Path.Combine(AppContext.BaseDirectory, "app-builder")));
        voiceRuntime = new DesktopVoiceRuntime(lifecycleLog, () => uiContext.Current, NavigateWorkspaceAsync);
        voiceRuntime.StateChanged += change => DispatcherQueue.TryEnqueue(() => OnVoiceStateChanged(change));
        voiceRuntime.TranscriptReceived += transcript => DispatcherQueue.TryEnqueue(() => UpdateLatestTranscript(string.IsNullOrWhiteSpace(transcript) ? "No transcript received." : transcript));
        voiceRuntime.ResponseReceived += response => DispatcherQueue.TryEnqueue(() => UpdateLatestResponse(response));
        voiceRuntime.ActivityReceived += activity => DispatcherQueue.TryEnqueue(() => AddActivity(activity));
        voiceRuntime.RealtimeUiProjectionReceived += projection => DispatcherQueue.TryEnqueue(() => OnRealtimeUiProjectionReceived(projection));
        voiceRuntime.AppBuilderMockRendered += evidence => DispatcherQueue.TryEnqueue(async () => await OnAppBuilderMockRenderedAsync(evidence));
        voiceRuntime.Faulted += fault => DispatcherQueue.TryEnqueue(() =>
        {
            SetState(DesktopState.Faulted, fault);
            AddActivity("Recoverable voice fault");
        });

        Closed += MainWindow_Closed;
        lifecycleLog.Write("app_start");
        SetState(DesktopState.Offline, "Desktop shell ready. Voice is stopped.");
        AddActivity("UI ready");
        AddConversation("Jarvis", "Ready.");
        lifecycleLog.Write("ui_ready");
        _ = RecoverBuilderContextAsync();
        BuilderPreviewWebView.WebMessageReceived += BuilderPreviewWebView_WebMessageReceived;
    }

    private async void StartVoiceButton_Click(object sender, RoutedEventArgs e)
    {
        StartVoiceButton.IsEnabled = false;
        lifecycleLog.Write("voice_start_requested");
        SetState(DesktopState.Starting, "Starting voice runtime.");

        var result = await voiceRuntime.StartAsync();
        if (result == StartVoiceResult.Started)
        {
            voiceRunning = true;
            AddActivity("Voice runtime started");
            SetState(DesktopState.Listening, "Listening. Speak a Gate 5.1 application launch request.");
        }
        else if (result == StartVoiceResult.ConfigurationRequired)
        {
            voiceRunning = false;
            SetState(DesktopState.Offline, "Configuration required: launch from the Fish-configured PowerShell session, or set FISH_AUDIO_API_KEY and FISH_AUDIO_REFERENCE_ID.");
            AddActivity("Voice configuration required");
        }
        else
        {
            voiceRunning = false;
            SetState(DesktopState.Faulted, "No Windows microphone input device is available.");
            AddActivity("Microphone unavailable");
        }

        RefreshButtons();
    }

    private void HomeNav_Click(object sender, RoutedEventArgs e) => SetWorkspace(JarvisWorkspace.HOME);
    private void ComputerNav_Click(object sender, RoutedEventArgs e) => SetWorkspace(JarvisWorkspace.COMPUTER);
    private void DeveloperNav_Click(object sender, RoutedEventArgs e) => SetWorkspace(JarvisWorkspace.DEVELOPER);
    private void MediaNav_Click(object sender, RoutedEventArgs e) => SetWorkspace(JarvisWorkspace.MEDIA);
    private void BuilderNav_Click(object sender, RoutedEventArgs e) => SetWorkspace(JarvisWorkspace.BUILDER);
    private void SystemNav_Click(object sender, RoutedEventArgs e) => SetWorkspace(JarvisWorkspace.SYSTEM);
    private void BarehandsNav_Click(object sender, RoutedEventArgs e)
    {
        activeWorkspace = JarvisWorkspace.BAREHANDS;
        PublishUiContext();
        barehandsWindow ??= new BarehandsWindow();
        barehandsWindow.Closed += (_, _) => barehandsWindow = null;
        barehandsWindow.Activate();
    }

    private Task NavigateWorkspaceAsync(JarvisWorkspace workspace)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        DispatcherQueue.TryEnqueue(() =>
        {
            if (workspace == JarvisWorkspace.BAREHANDS)
            {
                BarehandsNav_Click(this, new RoutedEventArgs());
            }
            else
            {
                SetWorkspace(workspace);
            }
            completion.SetResult();
        });
        return completion.Task;
    }

    private void SetWorkspace(JarvisWorkspace workspace)
    {
        activeWorkspace = workspace;
        switch (workspace)
        {
            case JarvisWorkspace.HOME: ShowView(HomeView); break;
            case JarvisWorkspace.COMPUTER: ShowView(ComputerView); break;
            case JarvisWorkspace.DEVELOPER: ShowView(DeveloperView); break;
            case JarvisWorkspace.MEDIA: ShowView(MediaView); break;
            case JarvisWorkspace.BUILDER: ShowView(BuilderView); break;
            case JarvisWorkspace.SYSTEM: ShowView(SystemView); break;
        }
        PublishUiContext();
    }

    private void PublishUiContext()
    {
        var spec = builderProject?.CurrentScreenDesignSpec;
        var candidate = builderCandidate ?? builderProject?.GeneratedArtifacts.LastOrDefault(a => a.AcceptanceState == ArtifactAcceptanceState.Candidate);
        uiContext.Set(new JarvisUiContext(activeWorkspace, builderProject?.Id, builderProject?.Name, spec?.Metadata.ScreenId, spec?.Metadata.ScreenName, spec?.Metadata.RevisionId, candidate?.ArtifactPath, candidate?.AcceptanceState.ToString(), spec?.ComponentTree.Select(c => c.SemanticId).ToArray() ?? [], builderSelection?.SemanticId, DateTimeOffset.UtcNow));
    }

    private async Task RecoverBuilderContextAsync()
    {
        try
        {
            builderProject = await appBuilder.LoadLatestActiveProjectAsync();
            if (builderProject is null) return;
            var context = await appBuilder.LoadLatestContextAsync();
            builderSelection = context?.Selection;
            builderCandidate = builderProject.GeneratedArtifacts.LastOrDefault(a => a.AcceptanceState == ArtifactAcceptanceState.Candidate) ?? builderProject.GeneratedArtifacts.LastOrDefault();
            RefreshBuilderWorkspace(null);
            if (builderCandidate is not null && File.Exists(builderCandidate.ArtifactPath))
                BuilderPreviewWebView.Source = new Uri(builderCandidate.ArtifactPath);
            PublishUiContext();
            AddActivity("Builder context recovered");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            AddActivity("Builder context recovery skipped");
            lifecycleLog.Write("recoverable_fault", new { subsystem = "builder_context", exceptionType = ex.GetType().Name, message = ex.Message });
        }
    }

    private void ShowView(UIElement view)
    {
        HomeView.Visibility = Visibility.Collapsed;
        ComputerView.Visibility = Visibility.Collapsed;
        DeveloperView.Visibility = Visibility.Collapsed;
        MediaView.Visibility = Visibility.Collapsed;
        BuilderView.Visibility = Visibility.Collapsed;
        SystemView.Visibility = Visibility.Collapsed;
        view.Visibility = Visibility.Visible;
    }

    private async void CreateBuilderProjectButton_Click(object sender, RoutedEventArgs e)
    {
        builderProject = await appBuilder.CreateOrLoadAlexisProjectAsync();
        builderCandidate = builderProject.GeneratedArtifacts.LastOrDefault(a => a.AcceptanceState == ArtifactAcceptanceState.Candidate);
        var context = await appBuilder.LoadLatestContextAsync();
        builderSelection = context?.Selection;
        RefreshBuilderWorkspace(null);
        PublishUiContext();
        AddActivity("Builder ALEXIS project loaded");
    }

    private async void GenerateBuilderMockButton_Click(object sender, RoutedEventArgs e)
    {
        builderProject ??= await appBuilder.CreateOrLoadAlexisProjectAsync();
        var result = await appBuilder.GenerateSystemScanMockAsync(builderProject.Id, "ALEXIS System Scan visual mock");
        builderProject = result.Project;
        builderCandidate = result.Artifact;
        builderSelection = null;
        RefreshBuilderWorkspace(result.Evaluation);
        BuilderPreviewWebView.Source = new Uri(result.Artifact.ArtifactPath);
        activeWorkspace = JarvisWorkspace.BUILDER;
        PublishUiContext();
        AddActivity("Builder System Scan mock rendered");
    }

    private async Task ExecuteBuilderPromptAsync(string text)
    {
        builderProject ??= await appBuilder.CreateOrLoadAlexisProjectAsync();
        var context = await appBuilder.LoadLatestContextAsync();
        var decision = appBuilder.ResolveInitiative(builderProject, context, text);
        if (!string.IsNullOrWhiteSpace(decision.ClarifyingQuestion))
        {
            UpdateLatestResponse(decision.ClarifyingQuestion);
            AddActivity("Builder clarification required");
            return;
        }

        if (decision.Action is BuilderInitiativeActionKind.ReviseSelection or BuilderInitiativeActionKind.ContinueExistingObjective)
        {
            var revised = await appBuilder.ReviseSystemScanAsync(builderProject.Id, decision.Arguments.GetValueOrDefault("objective", text));
            builderProject = revised.Project;
            builderCandidate = revised.Artifact;
            builderSelection = (await appBuilder.LoadLatestContextAsync())?.Selection;
            RefreshBuilderWorkspace(null);
            BuilderPreviewWebView.Source = new Uri(revised.Artifact.ArtifactPath);
            UpdateLatestResponse($"Builder revised {revised.Artifact.Id}, revision {revised.Artifact.Revision}.");
            AddActivity("Builder prompt routed to revision");
            return;
        }

        if (decision.Action is BuilderInitiativeActionKind.RenderCandidate)
        {
            var rendered = await appBuilder.GenerateSystemScanMockAsync(builderProject.Id, decision.Arguments.GetValueOrDefault("objective", text));
            builderProject = rendered.Project;
            builderCandidate = rendered.Artifact;
            builderSelection = null;
            RefreshBuilderWorkspace(rendered.Evaluation);
            BuilderPreviewWebView.Source = new Uri(rendered.Artifact.ArtifactPath);
            UpdateLatestResponse($"Builder rendered {rendered.Artifact.Id}, revision {rendered.Artifact.Revision}.");
            AddActivity("Builder prompt routed to render");
            return;
        }

        UpdateLatestResponse("Builder request is governed but no reversible action was available.");
        AddActivity("Builder prompt ended without mutation");
    }

    private async void ReviseBuilderMockButton_Click(object sender, RoutedEventArgs e)
    {
        builderProject ??= await appBuilder.CreateOrLoadAlexisProjectAsync();
        await appBuilder.ApplyConversationalRevisionAsync(builderProject.Id, "Make the topology larger, make the left panel narrower and remove the bright borders.");
        var result = await appBuilder.GenerateSystemScanMockAsync(builderProject.Id, "Revision 2: larger topology, narrower left panel, muted borders");
        builderProject = result.Project;
        builderCandidate = result.Artifact;
        builderSelection = null;
        RefreshBuilderWorkspace(result.Evaluation);
        BuilderPreviewWebView.Source = new Uri(result.Artifact.ArtifactPath);
        activeWorkspace = JarvisWorkspace.BUILDER;
        PublishUiContext();
        AddActivity("Builder revision rendered");
    }

    private async void AcceptBuilderMockButton_Click(object sender, RoutedEventArgs e)
    {
        if (builderProject is null || builderCandidate is null) return;
        builderProject = await appBuilder.MarkArtifactAsync(builderProject.Id, builderCandidate.Id, ArtifactAcceptanceState.Accepted, humanConfirmed: true);
        builderCandidate = builderProject.GeneratedArtifacts.Last(a => a.Id == builderCandidate.Id);
        RefreshBuilderWorkspace(null);
        PublishUiContext();
        AddActivity("Builder candidate accepted by human action");
    }

    private async void RejectBuilderMockButton_Click(object sender, RoutedEventArgs e)
    {
        if (builderProject is null || builderCandidate is null) return;
        builderProject = await appBuilder.MarkArtifactAsync(builderProject.Id, builderCandidate.Id, ArtifactAcceptanceState.Rejected, humanConfirmed: true);
        builderCandidate = builderProject.GeneratedArtifacts.Last(a => a.Id == builderCandidate.Id);
        RefreshBuilderWorkspace(null);
        PublishUiContext();
        AddActivity("Builder candidate rejected by human action");
    }

    private void RefreshBuilderWorkspace(VisualEvaluation? evaluation)
    {
        if (builderProject is null)
        {
            return;
        }

        var inventory = appBuilder.GetCapabilityInventory();
        BuilderProjectText.Text = $"{builderProject.Name} / {builderProject.CurrentPhase} / {builderProject.Product.Purpose}";
        BuilderSummaryText.Text = $"{builderProject.Requirements.Count} requirements, {builderProject.DesignAuthority.ColorTokens.Count} color tokens.\nDesign: {builderProject.DesignAuthority.VisualDirection}";
        BuilderCapabilityText.Text = string.Join("\n", inventory.Capabilities.Take(8).Select(c => $"{c.Id}: {c.State}"));
        var activeSpec = builderProject.CurrentScreenDesignSpec;
        BuilderRevisionText.Text = builderCandidate is null
            ? "No candidate yet."
            : $"{activeSpec?.Metadata.ScreenName ?? builderCandidate.ScreenObjective}\nRevision {builderCandidate.Revision} / {builderCandidate.AcceptanceState}\n{activeSpec?.TaskModel.UserGoal ?? "System Scan candidate"}\n{builderCandidate.ArtifactPath}";
        BuilderEvaluationText.Text = evaluation is null
            ? "Evaluation evidence loads when a mock is rendered."
            : $"{string.Join("; ", evaluation.Findings)}\nBoundary: {string.Join("; ", evaluation.Boundaries)}";
        BuilderSelectionText.Text = builderSelection is null
            ? "None."
            : $"{builderSelection.SemanticId}\n{builderSelection.ComponentType}\nEditable: {string.Join(", ", builderSelection.SafeEditableProperties.Keys)}";
    }

    private async Task OnAppBuilderMockRenderedAsync(AppBuilderMockEvidence evidence)
    {
        builderProject = await appBuilder.CreateOrLoadAlexisProjectAsync();
        builderCandidate = builderProject.GeneratedArtifacts.LastOrDefault(a => a.Id == evidence.ArtifactId);
        var context = await appBuilder.LoadLatestContextAsync();
        builderSelection = context?.Selection;
        VisualEvaluation? evaluation = null;
        if (File.Exists(evidence.EvaluationPath))
        {
            await using var stream = File.OpenRead(evidence.EvaluationPath);
            evaluation = await JsonSerializer.DeserializeAsync<VisualEvaluation>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }

        RefreshBuilderWorkspace(evaluation);
        ShowView(BuilderView);
        activeWorkspace = JarvisWorkspace.BUILDER;
        PublishUiContext();
        BuilderPreviewWebView.Source = new Uri(evidence.ArtifactPath);
            UpdateLatestResponse($"Rendered {evidence.ArtifactId}, revision {evidence.Revision}, state {evidence.AcceptanceState}.");
        AddActivity("Builder System Scan mock rendered from voice tool");
        lifecycleLog.Write("app_builder_mock_displayed", new
        {
            evidence.ArtifactId,
            evidence.Revision,
            evidence.ArtifactPath,
            evidence.AcceptanceState
        });
    }

    private async void BuilderPreviewWebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (builderProject is null) return;
        try
        {
            using var document = JsonDocument.Parse(args.WebMessageAsJson);
            var root = document.RootElement;
            if (!root.TryGetProperty("type", out var type) || type.GetString() != "builder.selection") return;
            var semanticId = root.GetProperty("semanticId").GetString();
            if (string.IsNullOrWhiteSpace(semanticId)) return;
            builderSelection = await appBuilder.SelectComponentAsync(builderProject.Id, semanticId);
            BuilderSelectionText.Text = $"{builderSelection.SemanticId}\n{builderSelection.ComponentType}\nEditable: {string.Join(", ", builderSelection.SafeEditableProperties.Keys)}";
            PublishUiContext();
            AddActivity($"Builder selected {builderSelection.SemanticId}");
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            AddActivity("Builder selection ignored");
            lifecycleLog.Write("recoverable_fault", new { subsystem = "builder_selection", exceptionType = ex.GetType().Name, message = ex.Message });
        }
    }

    private void CollapseBuilderLeft_Click(object sender, RoutedEventArgs e)
    {
        BuilderLeftPanel.Visibility = BuilderLeftPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        BuilderLeftColumn.Width = BuilderLeftPanel.Visibility == Visibility.Visible ? new GridLength(260) : new GridLength(0);
    }

    private void CollapseBuilderRight_Click(object sender, RoutedEventArgs e)
    {
        BuilderRightPanel.Visibility = BuilderRightPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        BuilderRightColumn.Width = BuilderRightPanel.Visibility == Visibility.Visible ? new GridLength(300) : new GridLength(0);
    }

    private void BuilderDesignMode_Click(object sender, RoutedEventArgs e) => BuilderModeText.Text = "DESIGN";
    private void BuilderPreviewMode_Click(object sender, RoutedEventArgs e) => BuilderModeText.Text = "PREVIEW";
    private void BuilderInspectMode_Click(object sender, RoutedEventArgs e) => BuilderModeText.Text = "INSPECT";

    private async void StopVoiceButton_Click(object sender, RoutedEventArgs e)
    {
        await StopVoiceAsync("voice stop requested");
    }

    private async void StartRealtimeButton_Click(object sender, RoutedEventArgs e)
    {
        StartRealtimeButton.IsEnabled = false;
        try
        {
            var status = await voiceRuntime.StartRealtimeFoundationAsync(RealtimeTransportWebView);
            realtimeFoundationStarted = status.Configured;
            realtimeRunning = status.PhysicalAudioReady;
            UpdateLatestResponse(status.PhysicalAudioReady
                ? "Realtime conversation is connected."
                : $"Realtime foundation ready, but physical audio is not connected: {status.Reason}.");
            if (status.PhysicalAudioReady)
                SetState(DesktopState.Listening, "Realtime WebRTC session connected.");
            AddActivity($"Realtime: {status.Reason}");
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or IOException or UnauthorizedAccessException)
        {
            realtimeFoundationStarted = false;
            realtimeRunning = false;
            UpdateLatestResponse("Realtime foundation did not start.");
            SetState(DesktopState.Faulted, "Realtime startup fault. No physical session is running.");
            lifecycleLog.Write("recoverable_fault", new { subsystem = "realtime", exceptionType = ex.GetType().Name, message = ex.Message });
        }
        finally
        {
            RefreshButtons();
        }
    }

    private async void StopRealtimeButton_Click(object sender, RoutedEventArgs e)
    {
        await voiceRuntime.StopRealtimeFoundationAsync();
        realtimeFoundationStarted = false;
        realtimeRunning = false;
        UpdateLatestResponse("Realtime foundation stopped.");
        SetState(voiceRunning ? DesktopState.Listening : DesktopState.Offline, voiceRunning ? "Listening." : "Realtime stopped. Desktop shell ready.");
        RefreshButtons();
    }

    private async void SendCommandButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteTypedCommandAsync();
    }

    private async void ComposerMicButton_Click(object sender, RoutedEventArgs e)
    {
        if (realtimeFoundationStarted)
        {
            await voiceRuntime.InterruptRealtimeFoundationAsync("composer microphone interrupt");
            AddActivity("Realtime interrupt requested from composer");
            return;
        }

        StartRealtimeButton_Click(sender, e);
    }

    private void CommandTextBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {
        SendCommandButton.IsEnabled = !string.IsNullOrWhiteSpace(CommandTextBox.Text);
    }

    private async void RunDeveloperTaskButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteDeveloperTaskAsync();
    }

    private async void GenerateMediaButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteMediaOperationAsync(() => voiceRuntime.GenerateMediaImageAsync(ReadMediaScene(), "Generate ALEXIS promo scene candidate using locked visual direction."));
    }

    private async void RegisterMediaButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteMediaOperationAsync(() => voiceRuntime.RegisterMediaCandidateAsync(ReadMediaScene(), MediaCandidateTextBox.Text.Trim(), "Externally generated ALEXIS promo candidate."));
    }

    private async void ApproveMediaButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteMediaOperationAsync(() => voiceRuntime.ApproveMediaCandidateAsync(ReadMediaScene(), MediaCandidateTextBox.Text.Trim()));
    }

    private async void RejectMediaButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteMediaOperationAsync(() => voiceRuntime.RejectMediaCandidateAsync(ReadMediaScene(), MediaCandidateTextBox.Text.Trim(), "Rejected by Leon."));
    }

    private async void CommandTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var shiftDown = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & CoreVirtualKeyStates.Down) != 0;
        if (e.Key != VirtualKey.Enter || shiftDown)
        {
            return;
        }

        e.Handled = true;
        await ExecuteTypedCommandAsync();
    }

    private async Task ExecuteTypedCommandAsync()
    {
        var text = CommandTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        CommandTextBox.Text = string.Empty;
        SendCommandButton.IsEnabled = false;
        AddConversation("Leon", text);
        SetState(DesktopState.Thinking, "Evaluating Jarvis command.");

        try
        {
            var route = JarvisShellCommandRouter.Route(uiContext.Current, text);
            AddActivity($"Composer routed: {route.Intent} / {route.Reason}");
            if (route.Workspace != activeWorkspace)
                SetWorkspace(route.Workspace);

            if (route.Intent == JarvisShellCommandIntent.Builder)
            {
                await ExecuteBuilderPromptAsync(text);
            }
            else if (route.Intent is JarvisShellCommandIntent.Developer)
            {
                UpdateLatestResponse("Developer workspace opened. Use the bounded workspace and objective fields, or continue through governed Realtime tools.");
            }
            else if (route.Intent is JarvisShellCommandIntent.Media)
            {
                UpdateLatestResponse("Media workspace opened. Governed media operations are ready.");
            }
            else if (route.Intent is JarvisShellCommandIntent.System)
            {
                UpdateLatestResponse("Diagnostics workspace opened.");
            }
            else
            {
                await voiceRuntime.ExecuteTypedLaunchAsync(text);
            }
            SetState(voiceRunning ? DesktopState.Listening : DesktopState.Offline, voiceRunning ? "Listening." : "Desktop shell ready. Voice is stopped.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            UpdateLatestResponse("The desktop capability operation failed safely.");
            SetState(DesktopState.Faulted, "Capability fault. The desktop app remains open.");
            AddActivity("Recoverable capability fault");
            lifecycleLog.Write("recoverable_fault", new { subsystem = "capability", exceptionType = ex.GetType().Name, message = ex.Message });
        }
        finally
        {
            SendCommandButton.IsEnabled = true;
            RefreshButtons();
        }
    }

    private async Task ExecuteDeveloperTaskAsync()
    {
        var workspace = DeveloperWorkspaceTextBox.Text.Trim();
        var objective = DeveloperObjectiveTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(workspace) || string.IsNullOrWhiteSpace(objective) || developerTaskRunning)
        {
            return;
        }

        developerTaskRunning = true;
        RunDeveloperTaskButton.IsEnabled = false;
        SetState(DesktopState.Thinking, "Running bounded developer task.");
        AddActivity("Developer task queued");

        try
        {
            var result = await voiceRuntime.ExecuteDeveloperTaskAsync(workspace, objective);
            UpdateLatestResponse(result.Status switch
            {
                Jarvis.Developer.DeveloperTaskStatus.Succeeded => $"Developer task completed. {result.FinalReport}",
                Jarvis.Developer.DeveloperTaskStatus.PolicyRejected => $"Developer task stopped: {result.FailureReason}",
                Jarvis.Developer.DeveloperTaskStatus.TimedOut => "Developer task timed out safely.",
                _ => $"Developer task ended: {result.Status}. {result.FailureReason}"
            });
            SetState(voiceRunning ? DesktopState.Listening : DesktopState.Offline, voiceRunning ? "Listening." : "Desktop shell ready. Voice is stopped.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            UpdateLatestResponse("The developer task stopped safely.");
            SetState(DesktopState.Faulted, "Developer task fault. The desktop app remains open.");
            AddActivity("Recoverable developer task fault");
            lifecycleLog.Write("recoverable_fault", new { subsystem = "developer", exceptionType = ex.GetType().Name, message = ex.Message });
        }
        finally
        {
            developerTaskRunning = false;
            RunDeveloperTaskButton.IsEnabled = true;
            RefreshButtons();
        }
    }

    private async Task ExecuteMediaOperationAsync(Func<Task<Jarvis.Governance.GovernedToolResult>> operation)
    {
        try
        {
            var result = await operation();
            MediaStatusText.Text = $"{result.Status}: {result.Message}\n{voiceRuntime.GetMediaStatus()}";
            AddActivity($"Media: {result.Status}");
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            MediaStatusText.Text = "Media operation stopped safely.";
            AddActivity("Recoverable media fault");
            lifecycleLog.Write("recoverable_fault", new { subsystem = "media", exceptionType = ex.GetType().Name, message = ex.Message });
        }
    }

    private int ReadMediaScene() => int.TryParse(MediaSceneTextBox.Text.Trim(), out var scene) ? scene : 1;

    private void OnVoiceStateChanged(VoiceStateChanged change)
    {
        var mapped = change.Current switch
        {
            VoiceSessionState.Stopped => DesktopState.Offline,
            VoiceSessionState.Starting => DesktopState.Starting,
            VoiceSessionState.Listening => DesktopState.Listening,
            VoiceSessionState.UserSpeaking => DesktopState.UserSpeaking,
            VoiceSessionState.Transcribing => DesktopState.Thinking,
            VoiceSessionState.Thinking => DesktopState.Thinking,
            VoiceSessionState.Speaking => DesktopState.Speaking,
            VoiceSessionState.Interrupting => DesktopState.Interrupting,
            VoiceSessionState.Recovering => DesktopState.Recovering,
            VoiceSessionState.Faulted => DesktopState.Faulted,
            _ => DesktopState.Faulted
        };
        SetState(mapped, change.Reason);
        lifecycleLog.Write("voice_state", new { from = change.Previous.ToString(), to = change.Current.ToString(), turnId = change.TurnId, change.Reason });
    }

    private async Task StopVoiceAsync(string reason)
    {
        lifecycleLog.Write("voice_stop_requested", new { reason });
        SetState(DesktopState.Interrupting, "Stopping voice runtime.");
        await voiceRuntime.StopAsync(reason);
        voiceRunning = false;
        SetState(DesktopState.Offline, "Voice stopped. Desktop shell remains open.");
        AddActivity("Voice stopped");
        RefreshButtons();
    }

    private void OnRealtimeUiProjectionReceived(RealtimeUiProjection projection)
    {
        switch (projection.Kind)
        {
            case RealtimeUiProjectionKind.Connected:
                realtimeFoundationStarted = true;
                realtimeRunning = true;
                SetState(DesktopState.Listening, "Realtime WebRTC session connected.");
                break;
            case RealtimeUiProjectionKind.Disconnected:
                realtimeRunning = false;
                SetState(voiceRunning ? DesktopState.Listening : DesktopState.Offline, voiceRunning ? "Listening." : "Realtime disconnected. Desktop shell ready.");
                break;
            case RealtimeUiProjectionKind.UserTranscriptStarted:
                UpdateLatestTranscript(string.Empty);
                SetState(DesktopState.UserSpeaking, "Realtime user speech detected.");
                break;
            case RealtimeUiProjectionKind.UserTranscriptUpdated:
                UpdateLatestTranscript(projection.Text);
                WriteRealtimeUiApplied(projection);
                SetState(DesktopState.UserSpeaking, "Realtime user transcript streaming.");
                break;
            case RealtimeUiProjectionKind.UserTranscriptCompleted:
                UpdateLatestTranscript(projection.Text);
                AddConversation("Leon", projection.Text);
                WriteRealtimeUiApplied(projection);
                SetState(DesktopState.Thinking, "Realtime user utterance received.");
                break;
            case RealtimeUiProjectionKind.AssistantResponseStarted:
                UpdateLatestResponse(string.Empty);
                SetState(DesktopState.Speaking, "Realtime assistant response started.");
                break;
            case RealtimeUiProjectionKind.AssistantResponseUpdated:
                UpdateLatestResponse(projection.Text);
                WriteRealtimeUiApplied(projection);
                SetState(DesktopState.Speaking, "Realtime assistant responding.");
                break;
            case RealtimeUiProjectionKind.AssistantResponseCompleted:
                UpdateLatestResponse(projection.Text);
                AddConversation("Jarvis", projection.Text);
                WriteRealtimeUiApplied(projection);
                SetState(DesktopState.Listening, "Realtime assistant response completed.");
                break;
            case RealtimeUiProjectionKind.AssistantInterrupted:
                UpdateLatestResponse(projection.Text);
                WriteRealtimeUiApplied(projection);
                SetState(DesktopState.Interrupting, "Realtime assistant interrupted.");
                break;
        }
    }

    private void WriteRealtimeUiApplied(RealtimeUiProjection projection)
    {
        lifecycleLog.Write("realtime_transcript_ui_applied", new
        {
            projectionKind = projection.Kind.ToString(),
            projection.Id,
            visibleCharacterCount = projection.Text.Length
        });
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        lifecycleLog.Write("app_shutdown_requested");
        await voiceRuntime.DisposeAsync();
        lifecycleLog.Write("app_shutdown");
    }

    private void SetState(DesktopState next, string detail)
    {
        state = next;
        CurrentStateText.Text = next.ToString().ToUpperInvariant();
        FaultStatusText.Text = detail;
        ComposerMicButton.Content = realtimeFoundationStarted ? "STOP" : "MIC";
        StatusDot.Fill = new SolidColorBrush(next switch
        {
            DesktopState.Faulted => ParseColor("#C96A5B"),
            DesktopState.Offline => ParseColor("#A8AAA5"),
            DesktopState.Interrupting or DesktopState.Recovering => ParseColor("#C96A5B"),
            _ => ParseColor("#D6965A")
        });
        CurrentStateText.Foreground = StatusDot.Fill;
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        StartVoiceButton.IsEnabled = !voiceRunning && state is DesktopState.Offline or DesktopState.Faulted;
        StopVoiceButton.IsEnabled = voiceRunning && state is not DesktopState.Offline;
        StartRealtimeButton.IsEnabled = !realtimeFoundationStarted;
        StopRealtimeButton.IsEnabled = realtimeFoundationStarted;
        ComposerMicButton.Content = realtimeFoundationStarted ? "STOP" : "MIC";
    }

    private void AddActivity(string text)
    {
        ActivityLogList.Items.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss}  {text}");
        while (ActivityLogList.Items.Count > 60)
        {
            ActivityLogList.Items.RemoveAt(ActivityLogList.Items.Count - 1);
        }
    }

    private void UpdateLatestTranscript(string text)
    {
        LatestTranscriptText.Text = string.IsNullOrWhiteSpace(text) ? "No transcript yet." : text;
        HomeLatestUserText.Text = string.IsNullOrWhiteSpace(text) ? "No user turn yet." : text;
    }

    private void UpdateLatestResponse(string text)
    {
        LatestResponseText.Text = string.IsNullOrWhiteSpace(text) ? "Ready." : text;
        HomeLatestJarvisText.Text = string.IsNullOrWhiteSpace(text) ? "Ready." : text;
    }

    private void AddConversation(string speaker, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;
        ConversationList.Items.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss}  {speaker}: {text}");
        while (ConversationList.Items.Count > 40)
        {
            ConversationList.Items.RemoveAt(ConversationList.Items.Count - 1);
        }
    }

    private static Windows.UI.Color ParseColor(string hex) => Windows.UI.Color.FromArgb(
        255,
        Convert.ToByte(hex.Substring(1, 2), 16),
        Convert.ToByte(hex.Substring(3, 2), 16),
        Convert.ToByte(hex.Substring(5, 2), 16));
}
