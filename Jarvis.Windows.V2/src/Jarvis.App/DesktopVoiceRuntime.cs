using Jarvis.ApplicationCapabilities;
using Jarvis.AppBuilder;
using Jarvis.Computer;
using Jarvis.Core;
using Jarvis.Developer;
using Jarvis.Governance;
using Jarvis.Media;
using Jarvis.Realtime;
using Jarvis.Voice;
using Microsoft.UI.Xaml.Controls;
using NAudio.Wave;

namespace Jarvis.App;

internal sealed class DesktopVoiceRuntime : IAsyncDisposable
{
    private readonly DesktopLifecycleLog lifecycleLog;
    private readonly IVoiceDiagnosticSink diagnostics;
    private readonly ApplicationLaunchExecutor launchExecutor;
    private readonly DeveloperTaskOrchestrator developerTasks;
    private readonly JsonRealtimeAudit realtimeAudit;
    private readonly AppBuilderService appBuilder;
    private readonly MediaJobStore mediaJobs;
    private readonly GovernedCapabilityRegistry mediaRegistry;
    private readonly GovernedCapabilityRegistry computerRegistry;
    private readonly GovernedCapabilityRegistry realtimeRegistry;
    private readonly ComputerControlService computerControl;
    private readonly MediaJob alexisPromo;
    private readonly ApplicationLaunchIntentRouter router = new();
    private readonly SemaphoreSlim lifecycle = new(1, 1);
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(2) };
    private CancellationTokenSource? runCancellation;
    private Task? loop;
    private VoiceSessionController? session;
    private RealtimeWebViewHost? realtimeHost;
    private bool disposed;

    public DesktopVoiceRuntime(DesktopLifecycleLog lifecycleLog, Func<JarvisUiContext>? uiContext = null, Func<JarvisWorkspace, Task>? navigateWorkspace = null)
    {
        this.lifecycleLog = lifecycleLog;
        diagnostics = new JsonLineVoiceDiagnosticSink(Path.Combine(AppContext.BaseDirectory, "logs", "desktop-voice.jsonl"));
        launchExecutor = new ApplicationLaunchExecutor(
            new Jarvis.ApplicationCapabilities.WindowsApplicationHost(),
            new AllowListedApplicationAuthorization(),
            new JsonLaunchAudit(Path.Combine(AppContext.BaseDirectory, "logs", "desktop-launch.jsonl")));
        var developerPolicy = new DeveloperCommandPolicy();
        developerTasks = new DeveloperTaskOrchestrator(
            new WorkspaceAuthorization(),
            developerPolicy,
            new CodexAgentClient(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "OpenAI", "Codex", "bin", "codex.exe"),
            new ProcessCodexRunner()),
            new JsonDeveloperAudit(Path.Combine(AppContext.BaseDirectory, "logs", "developer-tasks.jsonl"), developerPolicy));
        realtimeAudit = new JsonRealtimeAudit(Path.Combine(AppContext.BaseDirectory, "logs", "realtime-conversation.jsonl"));
        realtimeAudit.Write(Guid.NewGuid(), Guid.NewGuid(), "REALTIME_DEPLOYMENT_IDENTITY", new
        {
            processId = Environment.ProcessId,
            baseDirectory = AppContext.BaseDirectory,
            realtimeModuleVersionId = typeof(AzureRealtimeEphemeralSecretBroker).Module.ModuleVersionId,
            appModuleVersionId = typeof(DesktopVoiceRuntime).Module.ModuleVersionId
        });
        appBuilder = new AppBuilderService(new AppBuilderStore(Path.Combine(AppContext.BaseDirectory, "app-builder")));
        mediaJobs = new MediaJobStore(new JsonMediaAudit(Path.Combine(AppContext.BaseDirectory, "logs", "media-jobs.jsonl")));
        var mediaInput = Path.Combine(AppContext.BaseDirectory, "media", "alexis-promo-001", "candidates");
        var mediaOutput = Path.Combine(AppContext.BaseDirectory, "media", "alexis-promo-001", "locked");
        alexisPromo = new AlexisPromoManifestFactory().CreateAlexisPromo001(mediaInput, mediaOutput);
        mediaJobs.Save(alexisPromo);
        var mediaAudit = new JsonGovernedToolAudit(Path.Combine(AppContext.BaseDirectory, "logs", "media-tools.jsonl"));
        var imageProvider = MediaImageProviderSelector.Select(http, AzureImageProviderConfiguration.Load());
        var currentUiContext = uiContext ?? (() => new JarvisUiContext(JarvisWorkspace.HOME, null, null, null, null, null, null, null, [], null, DateTimeOffset.UtcNow));
        var developmentContext = new AppDevelopmentContextProvider(
            currentUiContext,
            () =>
            {
                try
                {
                    return appBuilder.LoadLatestContextAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
                {
                    return null;
                }
            },
            ReadApprovedWorkspaceCandidates());
        mediaRegistry = new GovernedCapabilityRegistry(
            [
                new MediaGovernedTools(mediaJobs, imageProvider),
                new GetMediaJobTool(mediaJobs),
                new RegisterMediaCandidateTool(mediaJobs),
                new ApproveMediaCandidateTool(mediaJobs),
                new RejectMediaCandidateTool(mediaJobs)
            ],
            mediaAudit);
        var computerAudit = new JsonComputerAudit(Path.Combine(AppContext.BaseDirectory, "logs", "computer-operations.jsonl"));
        computerControl = ComputerControlFactory.CreateDefault([Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), AppContext.BaseDirectory], computerAudit);
        computerRegistry = new GovernedCapabilityRegistry(
            [
                new ComputerGovernedTool(computerControl.ExecuteAsync),
                new ComputerLaunchApplicationGovernedTool(computerControl.ExecuteAsync),
                new ComputerStatusGovernedTool(computerControl.ExecuteAsync)
            ],
            new JsonGovernedToolAudit(Path.Combine(AppContext.BaseDirectory, "logs", "computer-tools.jsonl")));
        List<IGovernedTool> realtimeTools = [];
        realtimeTools.AddRange(
        [
                new DeveloperGovernedTool(new DeveloperTaskRunnerAdapter(developerTasks), developmentContext),
                new DevelopmentContextGovernedTool(developmentContext),
                new GetBuilderCapabilityInventoryTool(appBuilder),
                new GetBuilderProjectContextTool(appBuilder),
                new AppBuilderDesignSystemScanMockTool(appBuilder, evidence => AppBuilderMockRendered?.Invoke(evidence)),
                new AppBuilderSystemScanRevisionTool(appBuilder, evidence => AppBuilderMockRendered?.Invoke(evidence)),
                new SelectBuilderComponentTool(appBuilder),
                new SubmitBuilderImplementationTaskTool(appBuilder, new BuilderDeveloperTaskRunnerAdapter(developerTasks)),
                new GetJarvisUiContextTool(currentUiContext),
                new NavigateJarvisWorkspaceTool(navigateWorkspace ?? (_ => Task.CompletedTask)),
                new ComputerGovernedTool(computerControl.ExecuteAsync),
                new ComputerLaunchApplicationGovernedTool(computerControl.ExecuteAsync),
                new ComputerStatusGovernedTool(computerControl.ExecuteAsync),
                new MediaGovernedTools(mediaJobs, imageProvider),
                new GetMediaJobTool(mediaJobs),
                new RegisterMediaCandidateTool(mediaJobs),
                new ApproveMediaCandidateTool(mediaJobs),
                new RejectMediaCandidateTool(mediaJobs),
                new EmergencyAssistanceGovernedTool()
        ]);
        realtimeTools.Add(new CapabilityInventoryGovernedTool(() => realtimeTools.Select(tool => tool.Definition).ToArray()));
        realtimeRegistry = new GovernedCapabilityRegistry(
            realtimeTools,
            new RealtimeGovernanceAuditAdapter(realtimeAudit));
    }

    public event Action<VoiceStateChanged>? StateChanged;
    public event Action<string>? TranscriptReceived;
    public event Action<string>? ResponseReceived;
    public event Action<string>? ActivityReceived;
    public event Action<RealtimeUiProjection>? RealtimeUiProjectionReceived;
    public event Action<AppBuilderMockEvidence>? AppBuilderMockRendered;
    public event Action<string>? Faulted;

    public async Task<StartVoiceResult> StartAsync(CancellationToken cancellationToken = default)
    {
        await lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (loop is { IsCompleted: false })
            {
                return StartVoiceResult.Started;
            }

            var fish = FishAudioOptions.FromEnvironment();
            if (!fish.IsConfigured)
            {
                lifecycleLog.Write("recoverable_fault", new { subsystem = "voice", reason = "fish_configuration_missing" });
                return StartVoiceResult.ConfigurationRequired;
            }

            if (WaveIn.DeviceCount < 1)
            {
                lifecycleLog.Write("recoverable_fault", new { subsystem = "voice", reason = "microphone_missing" });
                return StartVoiceResult.MicrophoneUnavailable;
            }

            runCancellation = new CancellationTokenSource();
            session = new VoiceSessionController();
            session.StateChanged += (_, change) => StateChanged?.Invoke(change);
            session.StaleTurnRejected += (_, stale) =>
            {
                diagnostics.Write("stale_turn_rejected", new Dictionary<string, object?> { ["turnId"] = stale.TurnId, ["callback"] = stale.Callback });
                lifecycleLog.Write("stale_turn_rejected", new { turnId = stale.TurnId, stale.Callback });
            };

            await session.StartAsync(cancellationToken).ConfigureAwait(false);
            loop = RunLoopAsync(fish, runCancellation.Token);
            lifecycleLog.Write("voice_started");
            return StartVoiceResult.Started;
        }
        finally
        {
            lifecycle.Release();
        }
    }

    public async Task StopAsync(string reason = "operator stop")
    {
        await lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            runCancellation?.Cancel();
            if (session is not null)
            {
                await session.InterruptAsync(reason).ConfigureAwait(false);
                await session.StopAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            lifecycle.Release();
        }

        if (loop is not null)
        {
            try { await loop.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }

        await lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            if (session is not null)
            {
                await session.DisposeAsync().ConfigureAwait(false);
                session = null;
            }

            runCancellation?.Dispose();
            runCancellation = null;
            loop = null;
            lifecycleLog.Write("voice_stopped");
        }
        finally
        {
            lifecycle.Release();
        }
    }

    public async Task<LaunchResult?> ExecuteTypedLaunchAsync(string text, CancellationToken cancellationToken = default)
    {
        TranscriptReceived?.Invoke(text);
        if (!router.TryRoute(text, out var request, out var reason))
        {
            var rejection = "I could not recognize a single approved application launch command. No application was launched.";
            ResponseReceived?.Invoke(rejection);
            ActivityReceived?.Invoke($"Rejected typed command: {reason}");
            lifecycleLog.Write("capability_rejected", new { reason });
            return null;
        }

        var typedRequest = request ?? throw new InvalidOperationException("Recognized launch request was not provided.");
        ActivityReceived?.Invoke($"Capability requested: {typedRequest.Application}");
        lifecycleLog.Write("capability_requested", new { application = typedRequest.Application.ToString() });
        var result = await launchExecutor.ExecuteAsync(typedRequest, TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
        ResponseReceived?.Invoke(result.Message);
        ActivityReceived?.Invoke($"{result.Application}: {result.Status}");
        lifecycleLog.Write("capability_result", new { application = result.Application.ToString(), status = result.Status.ToString(), result.LaunchAttempted });
        return result;
    }

    public async Task<DeveloperTaskEvidence> ExecuteDeveloperTaskAsync(string workspace, string objective, CancellationToken cancellationToken = default)
    {
        var request = new DeveloperTaskRequest(
            Guid.NewGuid(),
            workspace,
            objective,
            new HashSet<DeveloperOperationClass>
            {
                DeveloperOperationClass.Inspect,
                DeveloperOperationClass.Search,
                DeveloperOperationClass.EditSource,
                DeveloperOperationClass.Build,
                DeveloperOperationClass.Test
            },
            [
                "no credential changes",
                "no security changes",
                "no deployment or publishing",
                "no Jarvis governance changes",
                "no legacy BackTalk or Kokoro changes",
                "no accepted Gates 1-4 changes",
                "no Fish Audio configuration changes",
                "no faster-whisper configuration changes"
            ],
            IterationLimit: 1,
            Timeout: TimeSpan.FromMinutes(20));

        ActivityReceived?.Invoke("Developer task requested");
        lifecycleLog.Write("developer_task_requested", new { request.TaskId, request.ApprovedWorkspace, request.Objective });
        var result = await developerTasks.RunAsync(request, cancellationToken).ConfigureAwait(false);
        ActivityReceived?.Invoke($"Developer task: {result.Status}");
        lifecycleLog.Write("developer_task_finished", new { result.TaskId, status = result.Status.ToString(), result.FailureReason, result.Iterations });
        return result;
    }

    public async Task<RealtimeProviderStatus> StartRealtimeFoundationAsync(WebView2 webView, CancellationToken cancellationToken = default)
    {
        var options = AzureRealtimeOptionsProvider.FromEnvironment();
        var status = new AzureRealtimeProviderBoundary(options).GetStatus();
        var sessionId = Guid.NewGuid();
        realtimeAudit.Write(sessionId, Guid.NewGuid(), "realtime_start_requested", new { status.Provider, status.Configured, status.PhysicalAudioReady, status.Reason, status.RequiredConfiguration });
        if (!status.Configured)
        {
            ActivityReceived?.Invoke("Realtime configuration required");
            lifecycleLog.Write("realtime_start_requested", new { status.Provider, status.Configured, status.PhysicalAudioReady, status.Reason });
            return status;
        }

        realtimeHost ??= CreateRealtimeWebViewHost(webView, options);
        var hostStatus = await realtimeHost.StartAsync(cancellationToken).ConfigureAwait(true);
        ActivityReceived?.Invoke($"Realtime transport: {hostStatus.Reason}");
        lifecycleLog.Write("realtime_start_requested", new { status.Provider, status.Configured, status.PhysicalAudioReady, status.Reason });
        return hostStatus;
    }

    public async Task StopRealtimeFoundationAsync()
    {
        if (realtimeHost is not null)
        {
            await realtimeHost.StopAsync().ConfigureAwait(true);
        }

        realtimeAudit.Write(Guid.NewGuid(), Guid.NewGuid(), "realtime_stop_requested");
        ActivityReceived?.Invoke("Realtime foundation stopped");
        lifecycleLog.Write("realtime_stop_requested");
    }

    public async Task InterruptRealtimeFoundationAsync(string reason)
    {
        if (realtimeHost is not null)
        {
            await realtimeHost.InterruptAsync(reason).ConfigureAwait(true);
        }
    }

    private RealtimeWebViewHost CreateRealtimeWebViewHost(WebView2 webView, AzureRealtimeOptions options)
    {
        var host = new RealtimeWebViewHost(
            webView,
            new AzureRealtimeEphemeralSecretBroker(http, options, realtimeAudit),
            options,
            new RealtimeGovernedToolBridge(realtimeRegistry),
            realtimeAudit);
        host.ActivityReceived += activity => ActivityReceived?.Invoke($"Realtime WebRTC: {activity}");
        host.UiProjectionReceived += projection => RealtimeUiProjectionReceived?.Invoke(projection);
        return host;
    }

    public async Task<GovernedToolResult> ExecuteComputerOperationAsync(ComputerOperationKind kind, string action, IReadOnlyDictionary<string, string> arguments, bool confirmed = false, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, string>(arguments, StringComparer.OrdinalIgnoreCase)
        {
            ["kind"] = kind.ToString(),
            ["action"] = action
        };
        var args = System.Text.Json.JsonSerializer.SerializeToElement(new
        {
            kind = payload["kind"],
            action = payload["action"],
            confirmed
        });
        if (arguments.Count > 0)
        {
            var expanded = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["kind"] = payload["kind"],
                ["action"] = payload["action"],
                ["confirmed"] = confirmed
            };
            foreach (var item in arguments)
            {
                expanded[item.Key] = item.Value;
            }

            args = System.Text.Json.JsonSerializer.SerializeToElement(expanded);
        }

        ActivityReceived?.Invoke($"Computer operation requested: {kind}.{action}");
        var result = await computerRegistry.DispatchAsync(new(Guid.NewGuid(), Guid.NewGuid(), "computer_operation", args), cancellationToken).ConfigureAwait(false);
        ActivityReceived?.Invoke($"Computer operation: {result.Status}");
        lifecycleLog.Write("computer_operation_result", new { kind = kind.ToString(), action, status = result.Status.ToString(), result.FailureReason });
        return result;
    }

    public string GetMediaStatus()
    {
        var job = mediaJobs.Get("ALEXIS_PROMO_001") ?? alexisPromo;
        var states = string.Join(", ", job.Scenes.Select(scene => $"{scene.Index:00}:{scene.State}"));
        return $"{job.JobId}: state={job.State}; {states}; failure={job.LastFailureReason ?? "none"}";
    }

    public async Task<GovernedToolResult> GenerateMediaImageAsync(int sceneIndex, string prompt, CancellationToken cancellationToken = default)
    {
        var args = System.Text.Json.JsonSerializer.SerializeToElement(new
        {
            job_id = "ALEXIS_PROMO_001",
            scene_index = sceneIndex,
            prompt,
            output_directory = Path.Combine(AppContext.BaseDirectory, "media", "alexis-promo-001", "candidates")
        });
        return await mediaRegistry.DispatchAsync(new(Guid.NewGuid(), Guid.NewGuid(), "generate_media_image", args), cancellationToken).ConfigureAwait(false);
    }

    public async Task<GovernedToolResult> RegisterMediaCandidateAsync(int sceneIndex, string candidatePath, string prompt, CancellationToken cancellationToken = default)
    {
        var args = System.Text.Json.JsonSerializer.SerializeToElement(new
        {
            job_id = "ALEXIS_PROMO_001",
            scene_index = sceneIndex,
            candidate_path = candidatePath,
            provider = "external",
            prompt
        });
        return await mediaRegistry.DispatchAsync(new(Guid.NewGuid(), Guid.NewGuid(), "register_media_candidate", args), cancellationToken).ConfigureAwait(false);
    }

    public async Task<GovernedToolResult> ApproveMediaCandidateAsync(int sceneIndex, string candidateId, CancellationToken cancellationToken = default)
    {
        var args = System.Text.Json.JsonSerializer.SerializeToElement(new { job_id = "ALEXIS_PROMO_001", scene_index = sceneIndex, candidate_id = candidateId });
        return await mediaRegistry.DispatchAsync(new(Guid.NewGuid(), Guid.NewGuid(), "approve_media_candidate", args), cancellationToken).ConfigureAwait(false);
    }

    public async Task<GovernedToolResult> RejectMediaCandidateAsync(int sceneIndex, string candidateId, string reason, CancellationToken cancellationToken = default)
    {
        var args = System.Text.Json.JsonSerializer.SerializeToElement(new { job_id = "ALEXIS_PROMO_001", scene_index = sceneIndex, candidate_id = candidateId, reason });
        return await mediaRegistry.DispatchAsync(new(Guid.NewGuid(), Guid.NewGuid(), "reject_media_candidate", args), cancellationToken).ConfigureAwait(false);
    }

    private async Task RunLoopAsync(FishAudioOptions fish, CancellationToken cancellationToken)
    {
        try
        {
            var activeSession = session ?? throw new InvalidOperationException("Voice session was not initialized.");
            var orchestrator = new Gate4Orchestrator(activeSession, diagnostics);
            var stt = new ReportingSttProvider(new FasterWhisperSttProvider(diagnostics), TranscriptReceived);
            var reasoner = new DesktopApplicationReasoner(router, launchExecutor, ActivityReceived, ResponseReceived, lifecycleLog);
            var tts = new FishAudioTtsProvider(http, fish, diagnostics);
            var playback = new WindowsAudioPlayback(diagnostics);

            while (!cancellationToken.IsCancellationRequested)
            {
                var turn = await activeSession.BeginTurnAsync(cancellationToken).ConfigureAwait(false);
                SttAudio? audio = null;
                try
                {
                    ActivityReceived?.Invoke("Listening");
                    audio = await DesktopMicrophoneCapture.CaptureUtteranceAsync(turn.Id, diagnostics, cancellationToken).ConfigureAwait(false);
                    var completed = await orchestrator.RunTurnAsync(
                        turn,
                        audio,
                        stt,
                        reasoner,
                        (id, text, token) => tts.SynthesizeAsync(text, token),
                        (id, speech, token) => playback.PlayAsync(speech, id, token)).ConfigureAwait(false);

                    if (!completed && !cancellationToken.IsCancellationRequested)
                    {
                        Faulted?.Invoke("Voice turn did not complete.");
                        lifecycleLog.Write("recoverable_fault", new { subsystem = "voice_turn", reason = "incomplete_turn" });
                    }
                }
                finally
                {
                    if (audio is not null)
                    {
                        try { File.Delete(audio.WavePath); } catch { }
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or HttpRequestException)
        {
            Faulted?.Invoke(ex.Message);
            lifecycleLog.Write("recoverable_fault", new { subsystem = "voice_runtime", exceptionType = ex.GetType().Name, message = ex.Message });
        }
    }

    public async ValueTask DisposeAsync()
    {
        disposed = true;
        await StopAsync("app shutdown").ConfigureAwait(true);
        if (realtimeHost is not null)
        {
            await realtimeHost.DisposeAsync().ConfigureAwait(true);
            realtimeHost = null;
        }

        http.Dispose();
        lifecycle.Dispose();
    }

    private sealed class ReportingSttProvider(ISttProvider inner, Action<string>? transcriptReceived) : ISttProvider
    {
        public async Task<SttResult> TranscribeAsync(SttAudio audio, CancellationToken cancellationToken = default)
        {
            var result = await inner.TranscribeAsync(audio, cancellationToken).ConfigureAwait(false);
            transcriptReceived?.Invoke(result.Transcript);
            return result;
        }
    }

    private sealed class DesktopApplicationReasoner(
        ApplicationLaunchIntentRouter router,
        ApplicationLaunchExecutor executor,
        Action<string>? activity,
        Action<string>? response,
        DesktopLifecycleLog lifecycleLog) : IJarvisReasoner
    {
        public async Task<string> RespondAsync(string transcript, Guid turnId, CancellationToken cancellationToken = default)
        {
            if (!router.TryRoute(transcript, out var request, out var reason))
            {
                var rejection = string.IsNullOrWhiteSpace(transcript)
                    ? "I did not receive any words. No application was requested."
                    : "I could not recognize a single approved application launch command. No application was launched.";
                activity?.Invoke($"Rejected voice command: {reason}");
                lifecycleLog.Write("capability_rejected", new { turnId, reason });
                response?.Invoke(rejection);
                return rejection;
            }

            var typedRequest = request ?? throw new InvalidOperationException("Recognized launch request was not provided.");
            activity?.Invoke($"Capability requested: {typedRequest.Application}");
            lifecycleLog.Write("capability_requested", new { turnId, application = typedRequest.Application.ToString() });
            var result = await executor.ExecuteAsync(typedRequest, TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
            activity?.Invoke($"{result.Application}: {result.Status}");
            lifecycleLog.Write("capability_result", new { turnId, application = result.Application.ToString(), status = result.Status.ToString(), result.LaunchAttempted });
            response?.Invoke(result.Message);
            return result.Message;
        }
    }

    private static IReadOnlyList<DevelopmentWorkspaceCandidate> ReadApprovedWorkspaceCandidates()
    {
        var candidates = new List<DevelopmentWorkspaceCandidate>();
        var single = Environment.GetEnvironmentVariable("JARVIS_APPROVED_WORKSPACE");
        if (!string.IsNullOrWhiteSpace(single))
        {
            candidates.Add(new(single, "JARVIS_APPROVED_WORKSPACE"));
        }

        var many = Environment.GetEnvironmentVariable("JARVIS_APPROVED_WORKSPACES");
        if (!string.IsNullOrWhiteSpace(many))
        {
            candidates.AddRange(many
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(path => new DevelopmentWorkspaceCandidate(path, "JARVIS_APPROVED_WORKSPACES")));
        }

        return candidates
            .GroupBy(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }
}

internal enum StartVoiceResult
{
    Started,
    ConfigurationRequired,
    MicrophoneUnavailable
}

internal static class DesktopMicrophoneCapture
{
    public static async Task<SttAudio> CaptureUtteranceAsync(Guid turnId, IVoiceDiagnosticSink diagnostics, CancellationToken cancellationToken)
    {
        var device = WaveIn.GetCapabilities(0).ProductName;
        var path = Path.Combine(Path.GetTempPath(), $"jarvis-desktop-{turnId:N}.wav");
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var speechSeen = false;
        var quietFrames = 0;
        var totalFrames = 0;

        using var capture = new WaveInEvent { DeviceNumber = 0, WaveFormat = new WaveFormat(16000, 16, 1), BufferMilliseconds = 50 };
        using var writer = new WaveFileWriter(path, capture.WaveFormat);
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        capture.DataAvailable += (_, e) =>
        {
            writer.Write(e.Buffer, 0, e.BytesRecorded);
            totalFrames++;
            var loud = IsLoudEnough(e.Buffer, e.BytesRecorded);
            speechSeen |= loud;
            quietFrames = loud ? 0 : quietFrames + 1;
            if ((speechSeen && quietFrames >= 18 && totalFrames >= 12) || totalFrames >= 240)
            {
                completion.TrySetResult();
            }
        };
        capture.RecordingStopped += (_, args) =>
        {
            if (args.Exception is not null) completion.TrySetException(args.Exception);
            else completion.TrySetResult();
        };

        diagnostics.Write("microphone_initialized", new Dictionary<string, object?> { ["turnId"] = turnId, ["inputDevice"] = device, ["sampleRate"] = 16000 });
        diagnostics.Write("capture_started", new Dictionary<string, object?> { ["turnId"] = turnId, ["inputDevice"] = device });
        capture.StartRecording();
        try
        {
            await completion.Task.ConfigureAwait(false);
        }
        finally
        {
            capture.StopRecording();
        }

        writer.Flush();
        diagnostics.Write("capture_ended", new Dictionary<string, object?> { ["turnId"] = turnId, ["wavePath"] = path, ["byteCount"] = new FileInfo(path).Length, ["speechSeen"] = speechSeen });
        return new SttAudio(path, device, turnId);
    }

    private static bool IsLoudEnough(byte[] buffer, int bytesRecorded)
    {
        long sum = 0;
        var samples = bytesRecorded / 2;
        if (samples == 0) return false;
        for (var index = 0; index < bytesRecorded; index += 2)
        {
            var sample = BitConverter.ToInt16(buffer, index);
            sum += Math.Abs(sample);
        }

        return sum / samples > 650;
    }
}

internal sealed class DesktopLifecycleLog(string path)
{
    private readonly object sync = new();

    public void Write(string name, object? data = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var entry = System.Text.Json.JsonSerializer.Serialize(new
        {
            timestamp = DateTimeOffset.UtcNow,
            name,
            data
        });
        lock (sync)
        {
            File.AppendAllText(path, entry + Environment.NewLine);
        }
    }
}
