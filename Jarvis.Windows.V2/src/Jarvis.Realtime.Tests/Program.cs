using System.Text.Json;
using Jarvis.Developer;
using Jarvis.Governance;
using Jarvis.Realtime;
using System.Net;

var tests = new RealtimeTests();
await tests.RunAll();

internal sealed class RealtimeTests
{
    private int passed;
    private int failed;

    public async Task RunAll()
    {
        await Run("natural request reaches developer tool", NaturalRequestReachesDeveloperTool);
        await Run("malformed tool arguments rejected", MalformedToolArgumentsRejected);
        await Run("unauthorized workspace rejected by Developer governance", UnauthorizedWorkspaceRejectedByDeveloperGovernance);
        await Run("permission escalation rejected before Developer execution", PermissionEscalationRejected);
        await Run("developer active project workspace routes without asking", DeveloperActiveProjectWorkspaceRoutesWithoutAsking);
        await Run("developer missing workspace recovers from active context", DeveloperMissingWorkspaceRecoversFromActiveContext);
        await Run("developer multiple workspace candidates asks disambiguation", DeveloperMultipleWorkspaceCandidatesAsksDisambiguation);
        await Run("developer no authorized workspace reports exact prerequisite", DeveloperNoAuthorizedWorkspaceReportsExactPrerequisite);
        await Run("developer workspace blocker persists to development context", DeveloperWorkspaceBlockerPersistsToDevelopmentContext);
        await Run("developer workspace blocker avoids generic fallback", DeveloperWorkspaceBlockerAvoidsGenericFallback);
        await Run("developer context cannot bypass workspace authorization", DeveloperContextCannotBypassWorkspaceAuthorization);
        await Run("unknown tool rejected", UnknownToolRejected);
        await Run("cancellation returns cancelled tool result", CancellationReturnsCancelled);
        await Run("duplicate tool calls remain correlated", DuplicateToolCallsRemainCorrelated);
        await Run("timeout result is preserved", TimeoutResultPreserved);
        await Run("secret leakage is redacted from audit", SecretLeakageRedacted);
        await Run("Azure GA provider reports physical evidence requirement", AzureProviderReportsPhysicalEvidenceRequirement);
        await Run("WebRTC physical readiness requires complete evidence", WebRtcPhysicalReadinessRequiresCompleteEvidence);
        await Run("WebRTC browser credential rejects long lived secret", WebRtcBrowserCredentialRejectsLongLivedSecret);
        await Run("WebRTC evidence records interruption", WebRtcEvidenceRecordsInterruption);
        await Run("WebRTC transport asset contains real peer connection path", WebRtcTransportAssetContainsRealPeerConnectionPath);
        await Run("ephemeral broker posts GA client secret request", EphemeralBrokerPostsGaClientSecretRequest);
        await Run("ephemeral broker emits English input transcription configuration only in body", EphemeralBrokerEmitsEnglishInputTranscriptionConfigurationOnlyInBody);
        await Run("session update preserves realtime tools and instructions", SessionUpdatePreservesRealtimeToolsAndInstructions);
        await Run("executive initiative instructions cover acceptance policy", ExecutiveInitiativeInstructionsCoverAcceptancePolicy);
        await Run("executive initiative instructions preserve governed action boundary", ExecutiveInitiativeInstructionsPreserveGovernedActionBoundary);
        await Run("durable realtime configuration reads secure store", DurableRealtimeConfigurationReadsSecureStore);
        await Run("durable realtime configuration environment overrides local and secret stores", DurableRealtimeConfigurationEnvironmentOverridesLocalAndSecretStores);
        await Run("durable realtime configuration reports missing secret without leaking values", DurableRealtimeConfigurationReportsMissingSecretWithoutLeakingValues);
        await Run("durable realtime configuration ignores malformed local config", DurableRealtimeConfigurationIgnoresMalformedLocalConfig);
        await Run("durable realtime configuration status redacts sentinel secret", DurableRealtimeConfigurationStatusRedactsSentinelSecret);
        await Run("data channel routes function call through governed bridge", DataChannelRoutesFunctionCallThroughGovernedBridge);
        await Run("data channel routes function call arguments done event", DataChannelRoutesFunctionCallArgumentsDoneEvent);
        await Run("data channel duplicate call id executes once", DataChannelDuplicateCallIdExecutesOnce);
        await Run("tool result preserves unavailable and evidence", ToolResultPreservesUnavailableAndEvidence);
        await Run("tool result preserves verification failed status", ToolResultPreservesVerificationFailedStatus);
        await Run("explicit computer launch tool is advertised", ExplicitComputerLaunchToolIsAdvertised);
        await Run("narrated computer action without tool call is detected", NarratedComputerActionWithoutToolCallIsDetected);
        await Run("capability inventory returns governed tool definitions", CapabilityInventoryReturnsGovernedToolDefinitions);
        await Run("emergency provider unavailable never claims contact", EmergencyProviderUnavailableNeverClaimsContact);
        await Run("data channel ignores non tool events", DataChannelIgnoresNonToolEvents);
        await Run("UI projector maps input transcription completion", UiProjectorMapsInputTranscriptionCompletion);
        await Run("UI projector live user sequence renders every delta", UiProjectorLiveUserSequenceRendersEveryDelta);
        await Run("UI projector accumulates multiple user transcript deltas", UiProjectorAccumulatesMultipleUserTranscriptDeltas);
        await Run("UI projector reconciles user final without duplication", UiProjectorReconcilesUserFinalWithoutDuplication);
        await Run("UI projector user final without text retains accumulator", UiProjectorUserFinalWithoutTextRetainsAccumulator);
        await Run("UI projector maps nested user conversation item transcript", UiProjectorMapsNestedUserConversationItemTranscript);
        await Run("UI projector live assistant sequence renders every delta", UiProjectorLiveAssistantSequenceRendersEveryDelta);
        await Run("UI projector streams assistant transcript deltas", UiProjectorStreamsAssistantTranscriptDeltas);
        await Run("UI projector streams assistant deltas after nested GA response id", UiProjectorStreamsAssistantDeltasAfterNestedGaResponseId);
        await Run("UI projector reconciles assistant final without duplication", UiProjectorReconcilesAssistantFinalWithoutDuplication);
        await Run("UI projector assistant final without text retains accumulator", UiProjectorAssistantFinalWithoutTextRetainsAccumulator);
        await Run("UI projector stale assistant delta cannot corrupt active response", UiProjectorStaleAssistantDeltaCannotCorruptActiveResponse);
        await Run("UI projector maps response done cancelled as interrupted", UiProjectorMapsResponseDoneCancelledAsInterrupted);
        await Run("UI projector maps nested assistant conversation item transcript", UiProjectorMapsNestedAssistantConversationItemTranscript);
        await Run("UI projector maps realtime session connection state", UiProjectorMapsRealtimeSessionConnectionState);
        await Run("UI projector ignores high frequency server events without projection", UiProjectorIgnoresHighFrequencyServerEventsWithoutProjection);
        await Run("UI projector new turn does not inherit previous transcript", UiProjectorNewTurnDoesNotInheritPreviousTranscript);
        await Run("UI projector stale previous turn delta cannot corrupt active turn", UiProjectorStalePreviousTurnDeltaCannotCorruptActiveTurn);
        await Run("UI projector interrupted assistant retains received transcript", UiProjectorInterruptedAssistantRetainsReceivedTranscript);
        await Run("UI projector malformed provider event fails safely", UiProjectorMalformedProviderEventFailsSafely);
        await Run("turn taking short thinking pause remains same turn", TurnTakingShortThinkingPauseRemainsSameTurn);
        await Run("turn taking multiple thinking pauses remain same turn", TurnTakingMultipleThinkingPausesRemainSameTurn);
        await Run("turn taking genuine completion commits one response", TurnTakingGenuineCompletionCommitsOneResponse);
        await Run("turn taking assistant speaking user starts immediate barge in", TurnTakingAssistantSpeakingUserStartsImmediateBargeIn);
        await Run("turn taking records assistant response completion evidence", TurnTakingRecordsAssistantResponseCompletionEvidence);
        await Run("turn taking does not duplicate response commits", TurnTakingDoesNotDuplicateResponseCommits);
        await Run("user turn gate rejects empty duplicate and playback echo", UserTurnGateRejectsEmptyDuplicateAndPlaybackEcho);
        await Run("user turn gate allows barge in transcript while playback active", UserTurnGateAllowsBargeInTranscriptWhilePlaybackActive);
        await Run("user turn gate permits one response per accepted turn", UserTurnGatePermitsOneResponsePerAcceptedTurn);
        Console.WriteLine($"REALTIME_TESTS_TOTAL passed={passed} failed={failed}");
        if (failed > 0) Environment.Exit(1);
    }

    private async Task NaturalRequestReachesDeveloperTool()
    {
        var root = CreateGitWorkspace();
        var runner = new FakeDeveloperRunner { ResultFactory = request => Evidence(request, DeveloperTaskStatus.Succeeded, "built and tested") };
        var audit = new MemoryAudit();
        var registry = new GovernedCapabilityRegistry([new DeveloperGovernedTool(runner)], new RealtimeGovernanceAuditAdapter(audit));
        var bridge = new RealtimeGovernedToolBridge(registry);
        await using var session = new ScriptedRealtimeSession(bridge, audit);
        var events = new List<RealtimeConversationEvent>();
        session.EventReceived += (_, e) => events.Add(e);
        await session.StartAsync(new(Guid.NewGuid(), "fake", "fake-realtime", bridge.Tools));
        await session.SendUserTextAsync($"Please improve this. workspace: {root} objective: add priority and run tests");
        Assert(runner.Requests.Count == 1, "developer runner should be invoked once");
        Assert(runner.Requests[0].ApprovedWorkspace == root, "workspace should pass to Developer request");
        Assert(runner.Requests[0].IterationLimit == 1, "iteration limit should remain bounded");
        Assert(events.Any(e => e.Kind == RealtimeEventKind.ToolRequested), "tool request event should be emitted");
        Assert(events.Any(e => e.Kind == RealtimeEventKind.AssistantText && e.Text!.Contains("built and tested", StringComparison.Ordinal)), "assistant text should include Developer result");
    }

    private async Task MalformedToolArgumentsRejected()
    {
        var bridge = Bridge(new FakeDeveloperRunner(), out _);
        var result = await bridge.InvokeAsync(Call("run_developer_task", new { objective = "missing workspace" }));
        Assert(result.Status == RealtimeToolStatus.Rejected, "malformed args should be rejected");
        Assert(result.FailureReason == "approved_workspace_required", "missing workspace reason should be explicit");
    }

    private async Task UnauthorizedWorkspaceRejectedByDeveloperGovernance()
    {
        var runner = new FakeDeveloperRunner { ResultFactory = request => Evidence(request, DeveloperTaskStatus.PolicyRejected, "", "workspace_not_git_repository") };
        var bridge = Bridge(runner, out _);
        var result = await bridge.InvokeAsync(Call("run_developer_task", new { approved_workspace = "C:\\NotApproved", objective = "do work" }));
        Assert(result.Status == RealtimeToolStatus.Rejected, "developer policy rejection should become rejected tool result");
        Assert(result.FailureReason == "workspace_not_git_repository", "developer failure reason should be preserved");
    }

    private async Task PermissionEscalationRejected()
    {
        var runner = new FakeDeveloperRunner();
        var bridge = Bridge(runner, out _);
        var result = await bridge.InvokeAsync(Call("run_developer_task", new { approved_workspace = "C:\\Repo", objective = "setx API_KEY and deploy outside the workspace" }));
        Assert(result.Status == RealtimeToolStatus.Rejected, "permission expansion should be rejected");
        Assert(runner.Requests.Count == 0, "developer runner must not run for expansion attempt");
    }

    private async Task DeveloperActiveProjectWorkspaceRoutesWithoutAsking()
    {
        var root = CreateGitWorkspace();
        var runner = new FakeDeveloperRunner();
        var provider = DevelopmentContext(new DevelopmentWorkspaceCandidate(root, "active_project"));
        var tool = new DeveloperGovernedTool(runner, provider);

        var result = await tool.InvokeAsync(new(Guid.NewGuid(), Guid.NewGuid(), DeveloperGovernedTool.Name, JsonSerializer.SerializeToElement(new { objective = "add diagnostics" })));

        Assert(result.Status == GovernedToolStatus.Succeeded, "single active project workspace should execute");
        Assert(runner.Requests.Count == 1, "developer runner should be called once");
        Assert(runner.Requests[0].ApprovedWorkspace == Path.GetFullPath(root), "authorized workspace should be recovered");
    }

    private async Task DeveloperMissingWorkspaceRecoversFromActiveContext()
    {
        var root = CreateGitWorkspace();
        var runner = new FakeDeveloperRunner();
        var bridge = Bridge(runner, DevelopmentContext(new DevelopmentWorkspaceCandidate(root, "builder_context")), out _);

        var result = await bridge.InvokeAsync(Call("run_developer_task", new { objective = "repair the failing view" }));

        Assert(result.Status == RealtimeToolStatus.Succeeded, "missing workspace should recover from deterministic context");
        Assert(runner.Requests.Count == 1, "developer runner should execute once");
        Assert(runner.Requests[0].ApprovedWorkspace == Path.GetFullPath(root), "recovered workspace should be canonical and authorized");
    }

    private async Task DeveloperMultipleWorkspaceCandidatesAsksDisambiguation()
    {
        var first = CreateGitWorkspace();
        var second = CreateGitWorkspace();
        var runner = new FakeDeveloperRunner();
        var bridge = Bridge(runner, DevelopmentContext(new DevelopmentWorkspaceCandidate(first, "active_project"), new DevelopmentWorkspaceCandidate(second, "environment")), out _);

        var result = await bridge.InvokeAsync(Call("run_developer_task", new { objective = "continue the build" }));

        Assert(result.Status == RealtimeToolStatus.Rejected, "multiple workspace candidates should not auto-run");
        Assert(result.FailureReason == "workspace_disambiguation_required", "disambiguation reason should be explicit");
        Assert(result.Message.Contains(Path.GetFullPath(first), StringComparison.OrdinalIgnoreCase), "first candidate should be listed");
        Assert(result.Message.Contains(Path.GetFullPath(second), StringComparison.OrdinalIgnoreCase), "second candidate should be listed");
        Assert(runner.Requests.Count == 0, "developer runner must not run until user chooses");
    }

    private async Task DeveloperNoAuthorizedWorkspaceReportsExactPrerequisite()
    {
        var runner = new FakeDeveloperRunner();
        var bridge = Bridge(runner, DevelopmentContext(), out _);

        var result = await bridge.InvokeAsync(Call("run_developer_task", new { objective = "implement the app" }));
        var evidence = JsonSerializer.Serialize(result.Evidence);

        Assert(result.Status == RealtimeToolStatus.Rejected, "no authorized workspace should reject");
        Assert(result.FailureReason == "approved_workspace_required", "missing prerequisite should be exact");
        Assert(evidence.Contains("approved_workspace", StringComparison.Ordinal), "evidence should name the missing prerequisite");
        Assert(runner.Requests.Count == 0, "developer runner must not run without authorization");
    }

    private async Task DeveloperWorkspaceBlockerPersistsToDevelopmentContext()
    {
        var provider = DevelopmentContext();
        var bridge = Bridge(new FakeDeveloperRunner(), provider, out _);

        await bridge.InvokeAsync(Call("run_developer_task", new { objective = "fix the project" }));

        var context = provider.Current();
        Assert(context.PreviousBlocker is not null, "context should retain the previous blocker");
        Assert(context.PreviousBlocker.FailureCategory == "approved_workspace_required", "blocker reason should be retained");
    }

    private async Task DeveloperWorkspaceBlockerAvoidsGenericFallback()
    {
        var bridge = Bridge(new FakeDeveloperRunner(), DevelopmentContext(), out _);

        var result = await bridge.InvokeAsync(Call("run_developer_task", new { objective = "build Jarvis" }));
        var text = result.Message + " " + JsonSerializer.Serialize(result.Evidence);

        Assert(!text.Contains("contact your development team", StringComparison.OrdinalIgnoreCase), "response should not refer to a development team");
        Assert(!text.Contains("development tools", StringComparison.OrdinalIgnoreCase), "response should not ask generic tooling questions");
        Assert(!text.Contains("platforms", StringComparison.OrdinalIgnoreCase), "response should not ask generic platform questions");
    }

    private async Task DeveloperContextCannotBypassWorkspaceAuthorization()
    {
        var unauthorized = Path.Combine(Path.GetTempPath(), "jarvis-realtime-no-git-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(unauthorized);
        var runner = new FakeDeveloperRunner();
        var bridge = Bridge(runner, DevelopmentContext(new DevelopmentWorkspaceCandidate(unauthorized, "active_project")), out _);

        var result = await bridge.InvokeAsync(Call("run_developer_task", new { objective = "change code" }));

        Assert(result.Status == RealtimeToolStatus.Rejected, "unauthorized context candidate should be rejected");
        Assert(result.FailureReason == "approved_workspace_required", "unauthorized candidates should not satisfy approved workspace");
        Assert(runner.Requests.Count == 0, "developer runner must not run for unauthorized context path");
    }

    private async Task UnknownToolRejected()
    {
        var bridge = Bridge(new FakeDeveloperRunner(), out _);
        var result = await bridge.InvokeAsync(Call("delete_everything", new { }));
        Assert(result.Status == RealtimeToolStatus.Rejected, "unknown tool should be rejected");
        Assert(result.FailureReason == "unknown_tool", "unknown tool reason should be explicit");
    }

    private async Task CancellationReturnsCancelled()
    {
        var runner = new FakeDeveloperRunner { ThrowCancellation = true };
        var bridge = Bridge(runner, out _);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var result = await bridge.InvokeAsync(Call("run_developer_task", new { approved_workspace = "C:\\Repo", objective = "do work" }), cts.Token);
        Assert(result.Status == RealtimeToolStatus.Cancelled, "cancelled invocation should return cancelled status");
    }

    private async Task DuplicateToolCallsRemainCorrelated()
    {
        var runner = new FakeDeveloperRunner { ResultFactory = request => Evidence(request, DeveloperTaskStatus.Succeeded, "ok") };
        var bridge = Bridge(runner, out _);
        var first = Call("run_developer_task", new { approved_workspace = "C:\\Repo", objective = "one" });
        var second = first with { CorrelationId = Guid.NewGuid() };
        var a = await bridge.InvokeAsync(first);
        var b = await bridge.InvokeAsync(second);
        Assert(a.CorrelationId != b.CorrelationId, "duplicate calls should retain distinct correlation ids");
        Assert(runner.Requests.Count == 2, "duplicates are auditable separate requests");
    }

    private async Task TimeoutResultPreserved()
    {
        var runner = new FakeDeveloperRunner { ResultFactory = request => Evidence(request, DeveloperTaskStatus.TimedOut, "", "codex_timeout", timedOut: true) };
        var bridge = Bridge(runner, out _);
        var result = await bridge.InvokeAsync(Call("run_developer_task", new { approved_workspace = "C:\\Repo", objective = "do work" }));
        Assert(result.Status == RealtimeToolStatus.TimedOut, "timeout should be preserved");
        Assert(result.FailureReason == "codex_timeout", "timeout reason should be preserved");
    }

    private async Task SecretLeakageRedacted()
    {
        var auditPath = Path.Combine(Path.GetTempPath(), "jarvis-realtime-test-" + Guid.NewGuid().ToString("N"), "audit.jsonl");
        var audit = new JsonRealtimeAudit(auditPath);
        var bridge = new DeveloperRealtimeToolBridge(new FakeDeveloperRunner(), audit);
        await bridge.InvokeAsync(Call("run_developer_task", new { approved_workspace = "C:\\Repo", objective = "FISH_AUDIO_API_KEY=do-not-write-this" }));
        var log = await File.ReadAllTextAsync(auditPath);
        Assert(!log.Contains("do-not-write-this", StringComparison.Ordinal), "secret value should not be written");
        Assert(log.Contains("tool_rejected", StringComparison.Ordinal), "secret-bearing tool request should be rejected and audited");
    }

    private Task AzureProviderReportsPhysicalEvidenceRequirement()
    {
        var status = new AzureRealtimeProviderBoundary(new("https://example.openai.azure.com", "gpt-realtime-2", "secret", true)).GetStatus();
        Assert(status.Configured, "configured provider should report configured");
        Assert(!status.PhysicalAudioReady, "provider boundary should not claim native audio readiness");
        Assert(status.Reason == "awaiting_webrtc_physical_evidence", "physical WebRTC evidence requirement should be explicit");
        return Task.CompletedTask;
    }

    private Task WebRtcPhysicalReadinessRequiresCompleteEvidence()
    {
        var sessionId = Guid.NewGuid();
        var gate = new WebRtcReadinessGate();
        var incomplete = new WebRtcTransportEvidenceBuilder(sessionId)
            .Apply(new("webview.ready", sessionId))
            .Apply(new("mic.stream.started", sessionId))
            .Build();
        Assert(!gate.IsPhysicalRealtimeReady(incomplete), "mic-only evidence must not be physical ready");

        var complete = new WebRtcTransportEvidenceBuilder(sessionId)
            .Apply(new("mic.stream.started", sessionId))
            .Apply(new("peer.connection.established", sessionId))
            .Apply(new("data.channel.open", sessionId))
            .Apply(new("remote.audio.track", sessionId))
            .Apply(new("remote.audio.playing", sessionId))
            .Build();
        Assert(gate.IsPhysicalRealtimeReady(complete), "complete WebRTC evidence should be physical ready");
        Assert(gate.ToProviderStatus(complete).PhysicalAudioReady, "provider status should expose physical readiness");
        return Task.CompletedTask;
    }

    private Task WebRtcBrowserCredentialRejectsLongLivedSecret()
    {
        var safe = new RealtimeEphemeralCredential("ephemeral-client-secret", new Uri("https://example.test/realtime/calls"), DateTimeOffset.UtcNow.AddMinutes(1));
        var unsafeKey = new RealtimeEphemeralCredential("OPENAI_API_KEY=do-not-expose", new Uri("https://example.test/realtime/calls"), DateTimeOffset.UtcNow.AddMinutes(1));
        var expired = safe with { ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1) };
        Assert(WebRtcCredentialPolicy.IsSafeForBrowser(safe), "ephemeral credential should be safe");
        Assert(!WebRtcCredentialPolicy.IsSafeForBrowser(unsafeKey), "long-lived API key marker should not be safe for browser");
        Assert(!WebRtcCredentialPolicy.IsSafeForBrowser(expired), "expired credential should not be safe for browser");
        return Task.CompletedTask;
    }

    private Task WebRtcEvidenceRecordsInterruption()
    {
        var sessionId = Guid.NewGuid();
        var evidence = new WebRtcTransportEvidenceBuilder(sessionId)
            .Apply(new("mic.stream.started", sessionId))
            .Apply(new("interrupt.sent", sessionId))
            .Build();
        Assert(evidence.State == WebRtcTransportState.Interrupted, "interruption state should be recorded");
        Assert(evidence.InterruptSupported, "interrupt evidence should be true");
        return Task.CompletedTask;
    }

    private async Task WebRtcTransportAssetContainsRealPeerConnectionPath()
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Jarvis.App", "RealtimeWeb", "realtime-transport.html")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Jarvis.App", "RealtimeWeb", "realtime-transport.html")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "src", "Jarvis.App", "RealtimeWeb", "realtime-transport.html")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Jarvis.App", "RealtimeWeb", "realtime-transport.html"))
        };
        var asset = candidates.FirstOrDefault(File.Exists) ?? candidates[0];
        var html = await File.ReadAllTextAsync(asset);
        Assert(html.Contains("navigator.mediaDevices.getUserMedia", StringComparison.Ordinal), "asset should request microphone media stream");
        Assert(html.Contains("echoCancellation: true", StringComparison.Ordinal), "asset should request browser echo cancellation");
        Assert(html.Contains("noiseSuppression: true", StringComparison.Ordinal), "asset should request browser noise suppression");
        Assert(html.Contains("autoGainControl: true", StringComparison.Ordinal), "asset should request browser auto gain control");
        Assert(html.Contains("new RTCPeerConnection", StringComparison.Ordinal), "asset should create peer connection");
        Assert(html.Contains("createDataChannel", StringComparison.Ordinal), "asset should create data channel");
        Assert(html.Contains("remoteAudio.play()", StringComparison.Ordinal), "asset should start remote audio playback");
        Assert(!html.Contains("OPENAI_API_KEY", StringComparison.OrdinalIgnoreCase), "asset must not contain long-lived API key name");
    }

    private async Task EphemeralBrokerPostsGaClientSecretRequest()
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(2).ToUnixTimeSeconds();
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { value = "ephemeral-client-secret", expires_at = expires }))
        });
        var http = new HttpClient(handler);
        var audit = new MemoryAudit();
        var broker = new AzureRealtimeEphemeralSecretBroker(
            http,
            new AzureRealtimeOptions("https://jarvis-test.openai.azure.com", "gpt-realtime-2", "server-api-key-do-not-log", true),
            audit);

        var credential = await broker.MintAsync(Guid.NewGuid(), [new("computer_operation", "Operate computer through governance.", ["kind", "action"])]);

        Assert(handler.RequestUri?.AbsolutePath == "/openai/v1/realtime/client_secrets", "broker should call GA client secret endpoint");
        Assert(handler.RequestBody.Contains("\"type\":\"realtime\"", StringComparison.Ordinal), "session.type realtime should be sent");
        Assert(handler.RequestBody.Contains("\"model\":\"gpt-realtime-2\"", StringComparison.Ordinal), "deployment should be in session payload");
        Assert(handler.RequestBody.Contains("\"turn_detection\"", StringComparison.Ordinal), "server-side realtime turn detection should be configured");
        Assert(handler.RequestBody.Contains("\"silence_duration_ms\":1800", StringComparison.Ordinal), "thinking pause grace should be bounded around 1.8s");
        Assert(handler.RequestBody.Contains("\"create_response\":false", StringComparison.Ordinal), "server VAD must not auto-create responses before native turn acceptance");
        Assert(handler.RequestBody.Contains("\"interrupt_response\":true", StringComparison.Ordinal), "barge-in must remain enabled");
        Assert(handler.RequestBody.Contains("\"language\":\"en\"", StringComparison.Ordinal), "English transcription language should be emitted");
        Assert(handler.RequestBody.Contains("computer_operation", StringComparison.Ordinal), "tool definition should be included");
        Assert(!handler.RequestBody.Contains("server-api-key-do-not-log", StringComparison.Ordinal), "server API key must not be in request body");
        Assert(handler.ApiKeyHeader == "server-api-key-do-not-log", "server API key should stay in broker-only header");
        Assert(credential.ClientSecret == "ephemeral-client-secret", "ephemeral credential should be parsed");
        Assert(credential.CallsEndpoint.AbsoluteUri == "https://jarvis-test.openai.azure.com/openai/v1/realtime/calls", "calls endpoint must preserve function-call and session events by omitting the browser event filter");
        Assert(audit.Entries.All(entry => !entry.Contains("server-api-key-do-not-log", StringComparison.Ordinal)), "audit should not contain server API key");
    }

    private async Task EphemeralBrokerEmitsEnglishInputTranscriptionConfigurationOnlyInBody()
    {
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { client_secret = new { value = "ephemeral-client-secret", expires_at = DateTimeOffset.UtcNow.AddMinutes(2).ToUnixTimeSeconds() } }))
        });
        var http = new HttpClient(handler);
        var audit = new MemoryAudit();
        var broker = new AzureRealtimeEphemeralSecretBroker(
            http,
            new AzureRealtimeOptions("https://jarvis-test.openai.azure.com", "gpt-realtime-2", "server-api-key-do-not-log", true, TranscriptionModel: "gpt-4o-mini-transcribe", TranscriptionLanguage: "en"),
            audit);

        await broker.MintAsync(Guid.NewGuid(), []);

        using var request = JsonDocument.Parse(handler.RequestBody);
        var input = request.RootElement.GetProperty("session").GetProperty("audio").GetProperty("input");
        var transcription = input.GetProperty("transcription");
        Assert(transcription.GetProperty("model").GetString() == "gpt-4o-mini-transcribe", "configured transcription deployment should be emitted");
        Assert(transcription.GetProperty("language").GetString() == "en", "English transcription language should be emitted at the supported session input transcription boundary");
        Assert(!handler.RequestBody.Contains("clientSecret", StringComparison.OrdinalIgnoreCase), "ephemeral client secret should never be echoed in session body");
        Assert(audit.Entries.All(entry => !entry.Contains("server-api-key-do-not-log", StringComparison.Ordinal)), "broker audit should not leak API key");
    }

    private Task SessionUpdatePreservesRealtimeToolsAndInstructions()
    {
        var payload = RealtimeSessionContract.BuildSessionUpdatePayload(
            new AzureRealtimeOptions("https://jarvis-test.openai.azure.com", "gpt-realtime-2", "server-api-key-do-not-log", true),
            [new("computer_operation", "Operate computer through governance.", ["kind", "action"])]);
        var json = JsonSerializer.Serialize(payload);

        Assert(json.Contains("\"type\":\"session.update\"", StringComparison.Ordinal), "session update should be emitted");
        Assert(json.Contains("computer_operation", StringComparison.Ordinal), "session update must preserve tool schema");
        Assert(json.Contains("governed Windows operating assistant", StringComparison.Ordinal), "session update must preserve action-oriented instructions");
        Assert(json.Contains("senior technical operator", StringComparison.Ordinal), "session update must carry executive operator instructions");
        Assert(json.Contains("\"tool_choice\":\"auto\"", StringComparison.Ordinal), "session update should allow model tool calls");
        Assert(!json.Contains("server-api-key-do-not-log", StringComparison.Ordinal), "session update must not contain API key");
        return Task.CompletedTask;
    }

    private Task ExecutiveInitiativeInstructionsCoverAcceptancePolicy()
    {
        var instructions = RealtimeSessionContract.DefaultInstructions;
        Assert(instructions.Contains("Classify every user turn", StringComparison.Ordinal), "instructions should require intent classification");
        Assert(instructions.Contains("outcome request", StringComparison.Ordinal), "instructions should distinguish outcome requests");
        Assert(instructions.Contains("direct command", StringComparison.Ordinal), "instructions should distinguish direct commands");
        Assert(instructions.Contains("decision request", StringComparison.Ordinal), "instructions should distinguish decision requests");
        Assert(instructions.Contains("factual information request", StringComparison.Ordinal), "instructions should distinguish factual requests");
        Assert(instructions.Contains("instead of asking where to start", StringComparison.Ordinal), "outcome requests should not reflexively ask where to start");
        Assert(instructions.Contains("already established an active session objective", StringComparison.Ordinal), "established objectives should guide follow-up starting points");
        Assert(instructions.Contains("fresh unrelated tasks", StringComparison.Ordinal), "fresh unrelated tasks should not inherit stale domain context");
        Assert(instructions.Contains("state the assumption briefly and proceed", StringComparison.Ordinal), "reversible defaults should continue with an explicit assumption");
        Assert(instructions.Contains("answer directly and briefly without creating a project plan", StringComparison.Ordinal), "simple factual questions should avoid project plans");
        Assert(instructions.Contains("ask a focused decision question", StringComparison.Ordinal), "material path differences should ask a focused decision question");
        return Task.CompletedTask;
    }

    private Task ExecutiveInitiativeInstructionsPreserveGovernedActionBoundary()
    {
        var payload = RealtimeSessionContract.BuildClientSecretPayload(
            new AzureRealtimeOptions("https://jarvis-test.openai.azure.com", "gpt-realtime-2", "server-api-key-do-not-log", true),
            [new("computer_operation", "Operate computer through governance.", ["kind", "action"])]);
        var json = JsonSerializer.Serialize(payload);

        Assert(json.Contains("\"type\":\"realtime\"", StringComparison.Ordinal), "client secret request should preserve realtime session type");
        Assert(json.Contains("\"model\":\"gpt-realtime-2\"", StringComparison.Ordinal), "client secret request should preserve deployment");
        Assert(json.Contains("\"audio\"", StringComparison.Ordinal), "client secret request should preserve audio transport configuration");
        Assert(json.Contains("\"create_response\":false", StringComparison.Ordinal), "client secret request should preserve native response gate");
        Assert(json.Contains("\"interrupt_response\":true", StringComparison.Ordinal), "client secret request should preserve barge-in");
        Assert(json.Contains("\"tool_choice\":\"auto\"", StringComparison.Ordinal), "client secret request should preserve automatic governed tool calls");
        Assert(json.Contains("do not bypass governance, permissions, approval gates, workspace boundaries, authentication, destructive confirmation, Computer controls, or Developer controls", StringComparison.Ordinal), "instructions should preserve governed action boundaries");
        Assert(json.Contains("AUTHORIZATION_REQUIRED", StringComparison.Ordinal), "instructions should stop on authorization boundaries");
        Assert(json.Contains("computer_operation", StringComparison.Ordinal), "client secret request should preserve governed tool schema");
        Assert(!json.Contains("server-api-key-do-not-log", StringComparison.Ordinal), "client secret request body must not contain API key");
        return Task.CompletedTask;
    }

    private Task DurableRealtimeConfigurationReadsSecureStore()
    {
        WithCleanRealtimeEnvironment(() =>
        {
            var provider = new JarvisRealtimeConfigurationProvider(
                new FakeLocalConfigStore(new("https://example.openai.azure.com", "gpt-realtime-2", "gpt-4o-mini-transcribe")),
                new FakeSecretStore("sentinel-secret-do-not-leak"));

            var configuration = provider.Resolve();

            Assert(configuration.Status.Configured, "durable local config and secure store should configure realtime");
            Assert(configuration.Status.ApiKeyPresent, "secure store secret should be present");
            Assert(configuration.Options.ApiKey == "sentinel-secret-do-not-leak", "secure store value should feed native broker only");
            Assert(!JsonSerializer.Serialize(configuration.Status).Contains("sentinel-secret-do-not-leak", StringComparison.Ordinal), "status must not leak secure store value");
        });
        return Task.CompletedTask;
    }

    private Task DurableRealtimeConfigurationEnvironmentOverridesLocalAndSecretStores()
    {
        WithCleanRealtimeEnvironment(() =>
        {
            Environment.SetEnvironmentVariable("AZURE_OPENAI_REALTIME_ENDPOINT", "https://env.example.openai.azure.com");
            Environment.SetEnvironmentVariable("AZURE_OPENAI_REALTIME_DEPLOYMENT", "env-deployment");
            Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", "env-secret-do-not-leak");
            Environment.SetEnvironmentVariable("AZURE_OPENAI_REALTIME_TRANSCRIPTION_DEPLOYMENT", "env-transcribe");
            var provider = new JarvisRealtimeConfigurationProvider(
                new FakeLocalConfigStore(new("https://local.example.openai.azure.com", "local-deployment", "local-transcribe")),
                new FakeSecretStore("store-secret-do-not-leak"));

            var configuration = provider.Resolve();

            Assert(configuration.Options.Endpoint == "https://env.example.openai.azure.com", "endpoint env should override local config");
            Assert(configuration.Options.Deployment == "env-deployment", "deployment env should override local config");
            Assert(configuration.Options.ApiKey == "env-secret-do-not-leak", "api key env should override secret store");
            Assert(configuration.Options.TranscriptionModel == "env-transcribe", "transcription env should override local config");
            var status = JsonSerializer.Serialize(configuration.Status);
            Assert(!status.Contains("env-secret-do-not-leak", StringComparison.Ordinal), "env secret should not leak in status");
            Assert(!status.Contains("store-secret-do-not-leak", StringComparison.Ordinal), "store secret should not leak in status");
        });
        return Task.CompletedTask;
    }

    private Task DurableRealtimeConfigurationReportsMissingSecretWithoutLeakingValues()
    {
        WithCleanRealtimeEnvironment(() =>
        {
            var provider = new JarvisRealtimeConfigurationProvider(
                new FakeLocalConfigStore(new("https://example.openai.azure.com", "gpt-realtime-2", "gpt-4o-mini-transcribe")),
                new FakeSecretStore(null));

            var configuration = provider.Resolve();
            var status = JsonSerializer.Serialize(configuration.Status);

            Assert(!configuration.Status.Configured, "missing secret should prevent configured status");
            Assert(configuration.Status.MissingConfiguration.Contains("AZURE_OPENAI_API_KEY"), "missing API key should be named");
            Assert(status.Contains("AZURE_OPENAI_API_KEY", StringComparison.Ordinal), "status should identify missing name");
            Assert(!status.Contains("secret", StringComparison.OrdinalIgnoreCase), "status should not include secret material");
        });
        return Task.CompletedTask;
    }

    private Task DurableRealtimeConfigurationIgnoresMalformedLocalConfig()
    {
        WithCleanRealtimeEnvironment(() =>
        {
            var path = Path.Combine(Path.GetTempPath(), "jarvis-realtime-config-" + Guid.NewGuid().ToString("N"), "realtime.local.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "{malformed");
            var provider = new JarvisRealtimeConfigurationProvider(new JsonRealtimeLocalConfigStore(path), new FakeSecretStore("sentinel-secret-do-not-leak"));

            var configuration = provider.Resolve();

            Assert(!configuration.Status.EndpointPresent, "malformed config should not provide endpoint");
            Assert(configuration.Status.MissingConfiguration.Contains("AZURE_OPENAI_REALTIME_ENDPOINT"), "malformed config should safely report missing endpoint");
            Assert(!JsonSerializer.Serialize(configuration.Status).Contains("sentinel-secret-do-not-leak", StringComparison.Ordinal), "malformed config status should not leak secret");
        });
        return Task.CompletedTask;
    }

    private Task DurableRealtimeConfigurationStatusRedactsSentinelSecret()
    {
        WithCleanRealtimeEnvironment(() =>
        {
            Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", "SENTINEL-SECRET-NEVER-LOG");
            var provider = new JarvisRealtimeConfigurationProvider(
                new FakeLocalConfigStore(new("https://example.openai.azure.com", "gpt-realtime-2", "whisper-1")),
                new FakeSecretStore("OTHER-SENTINEL-SECRET-NEVER-LOG"));

            var configuration = provider.Resolve();
            var report = JsonSerializer.Serialize(new
            {
                configuration.Status,
                Ui = configuration.Status.Configured ? "Realtime configuration present." : "Realtime configuration missing.",
                Evidence = configuration.Status.MissingConfiguration
            });

            Assert(!report.Contains("SENTINEL-SECRET-NEVER-LOG", StringComparison.Ordinal), "status report should redact environment sentinel secret");
            Assert(!report.Contains("OTHER-SENTINEL-SECRET-NEVER-LOG", StringComparison.Ordinal), "status report should redact store sentinel secret");
        });
        return Task.CompletedTask;
    }

    private async Task DataChannelRoutesFunctionCallThroughGovernedBridge()
    {
        var sessionId = Guid.NewGuid();
        var bridge = new RecordingToolBridge();
        var audit = new MemoryAudit();
        var router = new RealtimeDataChannelToolRouter(bridge, audit);
        var serverEvent = JsonSerializer.Serialize(new
        {
            type = "response.output_item.done",
            item = new
            {
                type = "function_call",
                call_id = "call_123",
                name = "computer_operation",
                arguments = JsonSerializer.Serialize(new { kind = "System", action = "status" })
            }
        });

        var responses = await router.HandleServerEventAsync(sessionId, serverEvent);

        Assert(bridge.Calls.Count == 1, "tool bridge should be invoked once");
        Assert(bridge.Calls[0].SessionId == sessionId, "tool call should keep realtime session id");
        Assert(bridge.Calls[0].Name == "computer_operation", "tool name should be routed");
        Assert(bridge.Calls[0].Arguments.GetProperty("action").GetString() == "status", "tool arguments should be parsed");
        Assert(responses.Count == 2, "tool output and response.create should be sent");
        Assert(responses[0].Contains("\"function_call_output\"", StringComparison.Ordinal), "function call output should be returned to same conversation");
        Assert(responses[0].Contains("\"call_id\":\"call_123\"", StringComparison.Ordinal), "tool result should preserve call id");
        Assert(responses[1].Contains("\"response.create\"", StringComparison.Ordinal), "response.create should follow tool output");
    }

    private async Task DataChannelRoutesFunctionCallArgumentsDoneEvent()
    {
        var sessionId = Guid.NewGuid();
        var bridge = new RecordingToolBridge();
        var router = new RealtimeDataChannelToolRouter(bridge, new MemoryAudit());
        var serverEvent = JsonSerializer.Serialize(new
        {
            type = "response.function_call_arguments.done",
            call_id = "call_456",
            name = "computer_operation",
            arguments = JsonSerializer.Serialize(new { kind = "Application", action = "launch", name = "Calculator" })
        });

        var responses = await router.HandleServerEventAsync(sessionId, serverEvent);

        Assert(bridge.Calls.Count == 1, "arguments.done event should invoke bridge once");
        Assert(bridge.Calls[0].Arguments.GetProperty("name").GetString() == "Calculator", "arguments.done should parse function arguments");
        Assert(responses[0].Contains("\"call_id\":\"call_456\"", StringComparison.Ordinal), "arguments.done result should preserve call id");
    }

    private async Task ToolResultPreservesUnavailableAndEvidence()
    {
        var bridge = new RecordingToolBridge
        {
            ResultFactory = call => new(call.SessionId, call.CorrelationId, call.Name, RealtimeToolStatus.Unavailable, "Application was not found.", new { status = "UNAVAILABLE" }, "application_not_found")
        };
        var router = new RealtimeDataChannelToolRouter(bridge, new MemoryAudit());
        var serverEvent = JsonSerializer.Serialize(new
        {
            type = "tool.call",
            call_id = "call_unavailable",
            name = "computer_operation",
            arguments = new { kind = "Application", action = "launch", name = "MissingApp" }
        });

        var responses = await router.HandleServerEventAsync(Guid.NewGuid(), serverEvent);

        Assert(responses[0].Contains("Unavailable", StringComparison.Ordinal), "unavailable status should be returned to model");
        Assert(responses[0].Contains("application_not_found", StringComparison.Ordinal), "failure reason should be returned to model");
        Assert(responses[0].Contains("UNAVAILABLE", StringComparison.Ordinal), "sanitized evidence should be returned to model");
    }

    private async Task DataChannelDuplicateCallIdExecutesOnce()
    {
        var sessionId = Guid.NewGuid();
        var bridge = new RecordingToolBridge();
        var audit = new MemoryAudit();
        var router = new RealtimeDataChannelToolRouter(bridge, audit);
        var serverEvent = JsonSerializer.Serialize(new
        {
            type = "response.output_item.done",
            item = new
            {
                type = "function_call",
                call_id = "call_repeat",
                name = "computer_launch_application",
                arguments = JsonSerializer.Serialize(new { name = "Calculator" })
            }
        });

        var first = await router.HandleServerEventAsync(sessionId, serverEvent);
        var second = await router.HandleServerEventAsync(sessionId, serverEvent);

        Assert(bridge.Calls.Count == 1, "duplicate realtime call id must not execute twice");
        Assert(first[0] == second[0], "duplicate call should return cached function output");
        Assert(audit.Entries.Any(x => x.Contains("TOOL_DUPLICATE_REPLAY_SUPPRESSED", StringComparison.Ordinal)), "duplicate suppression should be audited");
    }

    private async Task ToolResultPreservesVerificationFailedStatus()
    {
        var bridge = new RecordingToolBridge
        {
            ResultFactory = call => new(call.SessionId, call.CorrelationId, call.Name, RealtimeToolStatus.VerificationFailed, "Launch was not verified.", new { verification = "missing_window" }, "launch_verification_failed")
        };
        var router = new RealtimeDataChannelToolRouter(bridge, new MemoryAudit());
        var serverEvent = JsonSerializer.Serialize(new
        {
            type = "tool.call",
            call_id = "call_verify_failed",
            name = "computer_launch_application",
            arguments = new { name = "Calculator" }
        });

        var responses = await router.HandleServerEventAsync(Guid.NewGuid(), serverEvent);

        Assert(responses[0].Contains("VerificationFailed", StringComparison.Ordinal), "verification failure status should return to model");
        Assert(responses[0].Contains("launch_verification_failed", StringComparison.Ordinal), "verification failure reason should return to model");
        Assert(responses[1].Contains("VERIFICATION_FAILED", StringComparison.Ordinal), "truthful response instruction should name verification failure");
    }

    private Task ExplicitComputerLaunchToolIsAdvertised()
    {
        var registry = new GovernedCapabilityRegistry(
            [new DummyTool(new("computer_launch_application", "Launch an installed application through governance.", ["name"]))],
            new RealtimeGovernanceAuditAdapter(new MemoryAudit()));
        var bridge = new RealtimeGovernedToolBridge(registry);

        Assert(bridge.Tools.Any(tool => tool.Name == "computer_launch_application" && tool.RequiredArguments.Contains("name")), "explicit launch tool should be exposed to realtime");
        return Task.CompletedTask;
    }

    private Task NarratedComputerActionWithoutToolCallIsDetected()
    {
        Assert(RealtimeToolNarrationGuard.LooksLikeNarratedToolUse("I'm going to check the status. Word is still launching."), "narrated Word launch/status text should be detected as a bridge miss");
        Assert(!RealtimeToolNarrationGuard.LooksLikeNarratedToolUse("The weather is pleasant today."), "ordinary conversation should not be flagged");
        return Task.CompletedTask;
    }

    private async Task CapabilityInventoryReturnsGovernedToolDefinitions()
    {
        var registry = new GovernedCapabilityRegistry(
            [new EmergencyAssistanceGovernedTool()],
            new RealtimeGovernanceAuditAdapter(new MemoryAudit()));
        var tool = new CapabilityInventoryGovernedTool(() => registry.Definitions);

        var result = await tool.InvokeAsync(new(Guid.NewGuid(), Guid.NewGuid(), CapabilityInventoryGovernedTool.Name, JsonSerializer.SerializeToElement(new { })));
        var evidence = JsonSerializer.Serialize(result.Evidence);

        Assert(result.Status == GovernedToolStatus.Succeeded, "inventory should succeed");
        Assert(evidence.Contains(EmergencyAssistanceGovernedTool.Name, StringComparison.Ordinal), "inventory should include governed tool names");
    }

    private async Task EmergencyProviderUnavailableNeverClaimsContact()
    {
        var result = await new EmergencyAssistanceGovernedTool().InvokeAsync(new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            EmergencyAssistanceGovernedTool.Name,
            JsonSerializer.SerializeToElement(new { requested_help = "call emergency services" })));

        var evidence = JsonSerializer.Serialize(result.Evidence);
        Assert(result.Status == GovernedToolStatus.ProviderUnavailable, "emergency tool should report unavailable without provider");
        Assert(result.FailureReason == "EMERGENCY_PROVIDER_UNAVAILABLE", "emergency unavailable reason should be explicit");
        Assert(evidence.Contains("not_attempted", StringComparison.Ordinal), "emergency evidence should show no outbound contact was attempted");
    }

    private async Task DataChannelIgnoresNonToolEvents()
    {
        var router = new RealtimeDataChannelToolRouter(new RecordingToolBridge(), new MemoryAudit());
        var responses = await router.HandleServerEventAsync(Guid.NewGuid(), JsonSerializer.Serialize(new { type = "response.output_text.delta", delta = "hello" }));
        Assert(responses.Count == 0, "non tool events should not invoke tools");
    }

    private Task UiProjectorMapsInputTranscriptionCompletion()
    {
        var projector = new RealtimeUiProjector();
        var projection = projector.ProjectServerEvent(JsonSerializer.Serialize(new
        {
            type = "conversation.item.input_audio_transcription.completed",
            transcript = "Jarvis, can you hear me?"
        }));

        Assert(projection.Kind == RealtimeUiProjectionKind.UserTranscriptCompleted, "input transcription should update latest user transcript");
        Assert(projection.Text == "Jarvis, can you hear me?", "input transcription text should be preserved");
        return Task.CompletedTask;
    }

    private Task UiProjectorLiveUserSequenceRendersEveryDelta()
    {
        var projector = new RealtimeUiProjector();
        var visible = string.Empty;

        Apply(projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "input_audio_buffer.speech_started", item_id = "user-live" })), ref visible);
        Apply(projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "conversation.item.input_audio_transcription.delta", item_id = "user-live", delta = "Okay" })), ref visible);
        Assert(visible == "Okay", "first user delta should be visible immediately");
        Apply(projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "conversation.item.input_audio_transcription.delta", item_id = "user-live", delta = ", what" })), ref visible);
        Assert(visible == "Okay, what", "second user delta should extend visible transcript immediately");
        Apply(projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "conversation.item.input_audio_transcription.delta", item_id = "user-live", delta = " is your" })), ref visible);
        Assert(visible == "Okay, what is your", "third user delta should extend visible transcript immediately");
        Apply(projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "conversation.item.input_audio_transcription.completed", item_id = "user-live", transcript = "Okay, what is your capabilities, Jarvis?" })), ref visible);
        Assert(visible == "Okay, what is your capabilities, Jarvis?", "user final should replace provisional transcript exactly once");
        Assert(!visible.Contains("Okay, what is yourOkay", StringComparison.Ordinal), "user final should not duplicate provisional text");
        return Task.CompletedTask;
    }

    private Task UiProjectorAccumulatesMultipleUserTranscriptDeltas()
    {
        var projector = new RealtimeUiProjector();
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "input_audio_buffer.speech_started", item_id = "user-1" }));
        var first = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "conversation.item.input_audio_transcription.delta", item_id = "user-1", delta = "Jarvis, " }));
        var second = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "conversation.item.input_audio_transcription.delta", item_id = "user-1", delta = "can you hear me?" }));

        Assert(first.Kind == RealtimeUiProjectionKind.UserTranscriptUpdated, "user transcript deltas should stream to the UI");
        Assert(first.Text == "Jarvis, ", "first user delta should appear immediately");
        Assert(second.Text == "Jarvis, can you hear me?", "multiple user deltas should accumulate in order");
        return Task.CompletedTask;
    }

    private Task UiProjectorReconcilesUserFinalWithoutDuplication()
    {
        var projector = new RealtimeUiProjector();
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "input_audio_buffer.speech_started", item_id = "user-1" }));
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "conversation.item.input_audio_transcription.delta", item_id = "user-1", delta = "Jarvis, " }));
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "conversation.item.input_audio_transcription.delta", item_id = "user-1", delta = "can you hear me?" }));
        var final = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "conversation.item.input_audio_transcription.completed", item_id = "user-1", transcript = "Jarvis, can you hear me?" }));

        Assert(final.Kind == RealtimeUiProjectionKind.UserTranscriptCompleted, "user final transcript should complete the utterance");
        Assert(final.Text == "Jarvis, can you hear me?", "final user transcript should replace provisional text without duplicate words");
        return Task.CompletedTask;
    }

    private Task UiProjectorUserFinalWithoutTextRetainsAccumulator()
    {
        var projector = new RealtimeUiProjector();
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "input_audio_buffer.speech_started", item_id = "user-1" }));
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "conversation.item.input_audio_transcription.delta", item_id = "user-1", delta = "Still visible" }));
        var final = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "conversation.item.input_audio_transcription.completed", item_id = "user-1" }));

        Assert(final.Kind == RealtimeUiProjectionKind.UserTranscriptCompleted, "empty user final should still complete accumulated transcript");
        Assert(final.Text == "Still visible", "empty user final should retain accumulated transcript");
        return Task.CompletedTask;
    }

    private Task UiProjectorMapsNestedUserConversationItemTranscript()
    {
        var projector = new RealtimeUiProjector();
        var projection = projector.ProjectServerEvent(JsonSerializer.Serialize(new
        {
            type = "conversation.item.done",
            item = new
            {
                role = "user",
                content = new[] { new { type = "input_audio", transcript = "What's your capabilities?" } }
            }
        }));

        Assert(projection.Kind == RealtimeUiProjectionKind.UserTranscriptCompleted, "nested user item transcript should update latest user transcript");
        Assert(projection.Text == "What's your capabilities?", "nested user transcript text should be preserved");
        return Task.CompletedTask;
    }

    private Task UiProjectorStreamsAssistantTranscriptDeltas()
    {
        var projector = new RealtimeUiProjector();
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.created", response_id = "resp-1" }));
        var first = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.output_audio_transcript.delta", response_id = "resp-1", delta = "Yes, " }));
        var second = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.output_audio_transcript.delta", response_id = "resp-1", delta = "I can hear you." }));
        var done = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.output_audio_transcript.done", response_id = "resp-1" }));

        Assert(first.Kind == RealtimeUiProjectionKind.AssistantResponseUpdated, "assistant delta should stream response text");
        Assert(first.Text == "Yes, ", "first assistant delta should appear");
        Assert(second.Text == "Yes, I can hear you.", "assistant deltas should accumulate");
        Assert(done.Kind == RealtimeUiProjectionKind.AssistantResponseCompleted, "assistant done should complete response text");
        Assert(done.Text == "Yes, I can hear you.", "completed response should retain accumulated text");
        return Task.CompletedTask;
    }

    private Task UiProjectorLiveAssistantSequenceRendersEveryDelta()
    {
        var projector = new RealtimeUiProjector();
        var visible = string.Empty;

        Apply(projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.created", response = new { id = "resp-live", status = "in_progress" } })), ref visible);
        Apply(projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.audio_transcript.delta", response_id = "resp-live", item_id = "item-1", delta = "Great" })), ref visible);
        Assert(visible == "Great", "first assistant delta should be visible immediately");
        Apply(projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.audio_transcript.delta", response_id = "resp-live", item_id = "item-1", delta = " question" })), ref visible);
        Assert(visible == "Great question", "second assistant delta should extend visible response immediately");
        Apply(projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.audio_transcript.done", response_id = "resp-live", item_id = "item-1", transcript = "Great question." })), ref visible);
        Assert(visible == "Great question.", "assistant final should replace provisional response exactly once");
        Assert(!visible.Contains("Great questionGreat", StringComparison.Ordinal), "assistant final should not duplicate provisional text");
        return Task.CompletedTask;
    }

    private Task UiProjectorStreamsAssistantDeltasAfterNestedGaResponseId()
    {
        var projector = new RealtimeUiProjector();
        var started = projector.ProjectServerEvent(JsonSerializer.Serialize(new
        {
            type = "response.created",
            response = new { id = "resp-ga-1", status = "in_progress" }
        }));
        var first = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.audio_transcript.delta", response_id = "resp-ga-1", item_id = "item-1", delta = "Live " }));
        var second = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.audio_transcript.delta", response_id = "resp-ga-1", item_id = "item-1", delta = "text" }));

        Assert(started.Kind == RealtimeUiProjectionKind.AssistantResponseStarted, "nested GA response id should start assistant response");
        Assert(first.Text == "Live ", "first assistant transcript delta should bind after nested response.created id");
        Assert(second.Text == "Live text", "assistant transcript deltas should accumulate after nested response.created id");
        return Task.CompletedTask;
    }

    private Task UiProjectorReconcilesAssistantFinalWithoutDuplication()
    {
        var projector = new RealtimeUiProjector();
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.created", response_id = "resp-1" }));
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.output_audio_transcript.delta", response_id = "resp-1", delta = "I can " }));
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.output_audio_transcript.delta", response_id = "resp-1", delta = "help." }));
        var final = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.output_audio_transcript.done", response_id = "resp-1", transcript = "I can help." }));

        Assert(final.Kind == RealtimeUiProjectionKind.AssistantResponseCompleted, "assistant final transcript should complete response text");
        Assert(final.Text == "I can help.", "assistant final transcript should replace provisional text without duplicate words");
        return Task.CompletedTask;
    }

    private Task UiProjectorAssistantFinalWithoutTextRetainsAccumulator()
    {
        var projector = new RealtimeUiProjector();
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.created", response = new { id = "resp-1", status = "in_progress" } }));
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.audio_transcript.delta", response_id = "resp-1", delta = "Retain this" }));
        var final = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.audio_transcript.done", response_id = "resp-1" }));

        Assert(final.Kind == RealtimeUiProjectionKind.AssistantResponseCompleted, "empty assistant final should complete accumulated response");
        Assert(final.Text == "Retain this", "empty assistant final should retain accumulated response");
        return Task.CompletedTask;
    }

    private Task UiProjectorStaleAssistantDeltaCannotCorruptActiveResponse()
    {
        var projector = new RealtimeUiProjector();
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.created", response = new { id = "resp-1", status = "in_progress" } }));
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.audio_transcript.delta", response_id = "resp-1", delta = "Old" }));
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.created", response = new { id = "resp-2", status = "in_progress" } }));
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.audio_transcript.delta", response_id = "resp-2", delta = "New" }));
        var stale = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.audio_transcript.delta", response_id = "resp-1", delta = " stale" }));

        Assert(stale.Text == "New", "stale previous assistant delta should not corrupt active response");
        return Task.CompletedTask;
    }

    private Task UiProjectorMapsResponseDoneCancelledAsInterrupted()
    {
        var projector = new RealtimeUiProjector();
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.created", response = new { id = "resp-1", status = "in_progress" } }));
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.audio_transcript.delta", response_id = "resp-1", delta = "Partial spoken response" }));
        var cancelled = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.done", response = new { id = "resp-1", status = "cancelled" } }));

        Assert(cancelled.Kind == RealtimeUiProjectionKind.AssistantInterrupted, "cancelled response.done should project interruption");
        Assert(cancelled.Text == "Partial spoken response", "cancelled response.done should retain spoken transcript portion");
        return Task.CompletedTask;
    }

    private Task UiProjectorMapsNestedAssistantConversationItemTranscript()
    {
        var projector = new RealtimeUiProjector();
        var projection = projector.ProjectServerEvent(JsonSerializer.Serialize(new
        {
            type = "response.output_item.done",
            item = new
            {
                role = "assistant",
                content = new[] { new { type = "audio", transcript = "I can use governed computer, developer, and media tools." } }
            }
        }));

        Assert(projection.Kind == RealtimeUiProjectionKind.AssistantResponseCompleted, "nested assistant transcript should update latest Jarvis response");
        Assert(projection.Text == "I can use governed computer, developer, and media tools.", "nested assistant transcript text should be preserved");
        return Task.CompletedTask;
    }

    private Task UiProjectorMapsRealtimeSessionConnectionState()
    {
        var projector = new RealtimeUiProjector();

        Assert(projector.ProjectTransport("data.channel.open").Kind == RealtimeUiProjectionKind.Connected, "data channel open should mark realtime online");
        Assert(projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "session.created" })).Kind == RealtimeUiProjectionKind.Connected, "session created should mark realtime online");
        Assert(projector.ProjectTransport("closed").Kind == RealtimeUiProjectionKind.Disconnected, "closed transport should mark realtime offline");
        return Task.CompletedTask;
    }

    private Task UiProjectorIgnoresHighFrequencyServerEventsWithoutProjection()
    {
        var projector = new RealtimeUiProjector();
        var projection = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.audio.delta", delta = "base64-audio" }));

        Assert(projection.Kind == RealtimeUiProjectionKind.None, "raw audio/data deltas should not spam visible UI projection");
        return Task.CompletedTask;
    }

    private Task UiProjectorNewTurnDoesNotInheritPreviousTranscript()
    {
        var projector = new RealtimeUiProjector();
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "input_audio_buffer.speech_started", item_id = "user-1" }));
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "conversation.item.input_audio_transcription.delta", item_id = "user-1", delta = "First turn" }));
        var started = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "input_audio_buffer.speech_started", item_id = "user-2" }));
        var next = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "conversation.item.input_audio_transcription.delta", item_id = "user-2", delta = "Second turn" }));

        Assert(started.Kind == RealtimeUiProjectionKind.UserTranscriptStarted, "new speech should start a new user transcript");
        Assert(next.Text == "Second turn", "new turn should not inherit previous transcript text");
        return Task.CompletedTask;
    }

    private Task UiProjectorStalePreviousTurnDeltaCannotCorruptActiveTurn()
    {
        var projector = new RealtimeUiProjector();
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "input_audio_buffer.speech_started", item_id = "user-1" }));
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "conversation.item.input_audio_transcription.delta", item_id = "user-1", delta = "First turn" }));
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "input_audio_buffer.speech_started", item_id = "user-2" }));
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "conversation.item.input_audio_transcription.delta", item_id = "user-2", delta = "Second" }));
        var stale = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "conversation.item.input_audio_transcription.delta", item_id = "user-1", delta = " stale" }));

        Assert(stale.Text == "Second", "stale previous turn delta should not corrupt active transcript");
        return Task.CompletedTask;
    }

    private Task UiProjectorInterruptedAssistantRetainsReceivedTranscript()
    {
        var projector = new RealtimeUiProjector();
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.created", response_id = "resp-1" }));
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.output_audio_transcript.delta", response_id = "resp-1", delta = "I can explain the " }));
        projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.output_audio_transcript.delta", response_id = "resp-1", delta = "first part" }));
        var interrupted = projector.ProjectServerEvent(JsonSerializer.Serialize(new { type = "response.cancelled", response_id = "resp-1" }));

        Assert(interrupted.Kind == RealtimeUiProjectionKind.AssistantInterrupted, "cancelled response should project interruption");
        Assert(interrupted.Text == "I can explain the first part", "interrupted assistant should retain received transcript portion");
        return Task.CompletedTask;
    }

    private Task UiProjectorMalformedProviderEventFailsSafely()
    {
        var projector = new RealtimeUiProjector();
        var projection = projector.ProjectServerEvent("{not-json");

        Assert(projection.Kind == RealtimeUiProjectionKind.None, "malformed provider event should fail safely");
        return Task.CompletedTask;
    }

    private Task TurnTakingShortThinkingPauseRemainsSameTurn()
    {
        var policy = new RealtimeTurnTakingPolicy();
        var first = policy.Observe("input_audio_buffer.speech_stopped");
        var resumed = policy.Observe("input_audio_buffer.speech_started");

        Assert(first.Any(e => e.Kind == RealtimeTurnEvidenceKind.ThinkingGraceEntered), "speech stop should enter thinking grace");
        Assert(resumed.Any(e => e.Kind == RealtimeTurnEvidenceKind.SpeechResumedDuringGrace), "speech resumed during grace should stay same user turn");
        Assert(!resumed.Any(e => e.Kind == RealtimeTurnEvidenceKind.GraceExpiredUserTurnCommitted), "short pause must not commit the turn");
        return Task.CompletedTask;
    }

    private Task TurnTakingMultipleThinkingPausesRemainSameTurn()
    {
        var policy = new RealtimeTurnTakingPolicy();
        var events = new List<RealtimeTurnEvidence>();
        events.AddRange(policy.Observe("input_audio_buffer.speech_stopped"));
        events.AddRange(policy.Observe("input_audio_buffer.speech_started"));
        events.AddRange(policy.Observe("input_audio_buffer.speech_stopped"));
        events.AddRange(policy.Observe("input_audio_buffer.speech_started"));

        Assert(events.Count(e => e.Kind == RealtimeTurnEvidenceKind.SpeechResumedDuringGrace) == 2, "each resumed thinking pause should stay in the same user turn");
        Assert(!events.Any(e => e.Kind == RealtimeTurnEvidenceKind.GraceExpiredUserTurnCommitted), "multiple thinking pauses must not create fragment commits");
        return Task.CompletedTask;
    }

    private Task TurnTakingGenuineCompletionCommitsOneResponse()
    {
        var policy = new RealtimeTurnTakingPolicy();
        policy.Observe("input_audio_buffer.speech_stopped");
        var committed = policy.Observe("input_audio_buffer.committed");
        var response = policy.Observe("response.created");

        Assert(committed.Count(e => e.Kind == RealtimeTurnEvidenceKind.GraceExpiredUserTurnCommitted) == 1, "genuine completion should commit exactly once");
        Assert(response.Count(e => e.Kind == RealtimeTurnEvidenceKind.AssistantResponseStarted) == 1, "one assistant response should start after commit");
        Assert(!response.Any(e => e.Kind == RealtimeTurnEvidenceKind.GraceExpiredUserTurnCommitted), "response start should not duplicate an existing commit");
        return Task.CompletedTask;
    }

    private Task TurnTakingAssistantSpeakingUserStartsImmediateBargeIn()
    {
        var policy = new RealtimeTurnTakingPolicy();
        policy.Observe("response.created");
        var interrupted = policy.Observe("input_audio_buffer.speech_started");

        Assert(interrupted.Any(e => e.Kind == RealtimeTurnEvidenceKind.AssistantInterrupted), "user speech while assistant speaks should immediately register barge-in");
        Assert(interrupted.Any(e => e.Kind == RealtimeTurnEvidenceKind.AssistantCancellationRequested), "barge-in should request assistant cancellation");
        Assert(interrupted.Any(e => e.Kind == RealtimeTurnEvidenceKind.AssistantAudioFlushRequested), "barge-in should record provider-managed audio flush evidence");
        Assert(interrupted.Any(e => e.Kind == RealtimeTurnEvidenceKind.NewUserUtteranceAccepted), "barge-in should accept the replacement utterance");
        Assert(interrupted.Any(e => e.Kind == RealtimeTurnEvidenceKind.UserSpeechStarted), "barge-in should accept new user speech");
        return Task.CompletedTask;
    }

    private Task TurnTakingRecordsAssistantResponseCompletionEvidence()
    {
        var policy = new RealtimeTurnTakingPolicy();
        policy.Observe("response.created");
        var done = policy.Observe("response.done");

        Assert(done.Any(e => e.Kind == RealtimeTurnEvidenceKind.AssistantResponseCompleted), "response.done should record assistant response completion evidence");
        return Task.CompletedTask;
    }

    private Task TurnTakingDoesNotDuplicateResponseCommits()
    {
        var policy = new RealtimeTurnTakingPolicy();
        policy.Observe("input_audio_buffer.speech_stopped");
        var committed = policy.Observe("input_audio_buffer.committed");
        var response = policy.Observe("response.created");
        var duplicate = policy.Observe("response.created");

        var commitCount = committed.Concat(response).Concat(duplicate)
            .Count(e => e.Kind == RealtimeTurnEvidenceKind.GraceExpiredUserTurnCommitted);
        Assert(commitCount == 1, "one completed utterance should produce one commit evidence event");
        return Task.CompletedTask;
    }

    private Task UserTurnGateRejectsEmptyDuplicateAndPlaybackEcho()
    {
        var gate = new RealtimeUserTurnGate();
        var accepted = gate.EvaluateInputTranscriptionCompleted(UserFinal("item-1", "Jarvis, tell me your capabilities."));
        var duplicateItem = gate.EvaluateInputTranscriptionCompleted(UserFinal("item-1", "Jarvis, tell me your capabilities."));
        var empty = gate.EvaluateInputTranscriptionCompleted(UserFinal("item-2", ""));
        gate.SetAssistantPlaybackActive(true);
        gate.ObserveAssistantTranscript("I hope you enjoyed this video. Thank you for watching. See you in the next video.");
        var echo = gate.EvaluateInputTranscriptionCompleted(UserFinal("item-3", "I hope you enjoyed this video. Thank you for watching. See you in the next video."));

        Assert(accepted.Accepted, "first real transcript should be accepted");
        Assert(!duplicateItem.Accepted && duplicateItem.Reason == "duplicate", "duplicate item should be rejected");
        Assert(!empty.Accepted && empty.Reason == "empty", "empty transcript should be rejected");
        Assert(!echo.Accepted && echo.Reason == "playback_echo", "near-identical assistant playback transcript should be rejected");
        return Task.CompletedTask;
    }

    private Task UserTurnGateAllowsBargeInTranscriptWhilePlaybackActive()
    {
        var gate = new RealtimeUserTurnGate();
        gate.SetAssistantPlaybackActive(true);
        gate.ObserveAssistantTranscript("I can list the current capabilities in a concise way.");
        var decision = gate.EvaluateInputTranscriptionCompleted(UserFinal("barge-1", "Stop. Tell me only the first three."));

        Assert(decision.Accepted, "non-echo barge-in transcript should remain accepted while playback is active");
        return Task.CompletedTask;
    }

    private Task UserTurnGatePermitsOneResponsePerAcceptedTurn()
    {
        var gate = new RealtimeUserTurnGate();
        var decision = gate.EvaluateInputTranscriptionCompleted(UserFinal("item-1", "Jarvis, tell me what capabilities you currently have."));

        Assert(decision.Accepted, "real transcript should be accepted");
        Assert(gate.TryMarkResponseStarted(decision.TurnId), "first response for turn should be allowed");
        Assert(!gate.TryMarkResponseStarted(decision.TurnId), "second normal response for same turn should be suppressed");
        return Task.CompletedTask;
    }

    private static string UserFinal(string itemId, string transcript) =>
        JsonSerializer.Serialize(new { type = "conversation.item.input_audio_transcription.completed", item_id = itemId, transcript });

    private static DeveloperRealtimeToolBridge Bridge(FakeDeveloperRunner runner, out MemoryAudit audit)
    {
        audit = new MemoryAudit();
        return new(runner, audit);
    }

    private static DeveloperRealtimeToolBridge Bridge(FakeDeveloperRunner runner, StaticDevelopmentContextProvider provider, out MemoryAudit audit)
    {
        audit = new MemoryAudit();
        return new(runner, audit, provider);
    }

    private static StaticDevelopmentContextProvider DevelopmentContext(params DevelopmentWorkspaceCandidate[] candidates) =>
        new(() => new DevelopmentContextSnapshot(
            "project-1",
            "Jarvis",
            "Build the active app",
            null,
            candidates.Length == 0 ? "none_configured" : "candidate",
            "Home",
            "r1",
            "implement",
            null,
            candidates,
            ["inspect_development_context", "ask_for_approved_workspace"]));

    private static RealtimeToolCall Call(string name, object args) =>
        new(Guid.NewGuid(), Guid.NewGuid(), name, JsonSerializer.SerializeToElement(args));

    private static DeveloperTaskEvidence Evidence(DeveloperTaskRequest request, DeveloperTaskStatus status, string report, string? reason = null, bool timedOut = false) =>
        new(request.TaskId, request.ApprovedWorkspace, request.Objective, status, 1, status == DeveloperTaskStatus.Succeeded ? 0 : -1, timedOut, ["src/a.cs"], ["dotnet test"], report, reason);

    private static void WithCleanRealtimeEnvironment(Action action)
    {
        var names = new[]
        {
            "AZURE_OPENAI_REALTIME_ENDPOINT",
            "AZURE_OPENAI_REALTIME_DEPLOYMENT",
            "AZURE_OPENAI_API_KEY",
            "AZURE_OPENAI_REALTIME_TRANSCRIPTION_DEPLOYMENT"
        };
        var prior = names.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var name in names)
                Environment.SetEnvironmentVariable(name, null);
            action();
        }
        finally
        {
            foreach (var (name, value) in prior)
                Environment.SetEnvironmentVariable(name, value);
        }
    }

    private static void Apply(RealtimeUiProjection projection, ref string visible)
    {
        if (projection.Kind is RealtimeUiProjectionKind.UserTranscriptUpdated or
            RealtimeUiProjectionKind.UserTranscriptCompleted or
            RealtimeUiProjectionKind.AssistantResponseUpdated or
            RealtimeUiProjectionKind.AssistantResponseCompleted or
            RealtimeUiProjectionKind.AssistantInterrupted)
        {
            visible = projection.Text;
        }
    }

    private static string CreateGitWorkspace()
    {
        var root = Path.Combine(Path.GetTempPath(), "jarvis-realtime-git-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        return root;
    }

    private async Task Run(string name, Func<Task> test)
    {
        try
        {
            await test();
            passed++;
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            failed++;
            Console.WriteLine($"FAIL {name}: {ex.Message}");
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakeDeveloperRunner : IDeveloperTaskRunner
    {
        public List<DeveloperTaskRequest> Requests { get; } = [];
        public bool ThrowCancellation { get; init; }
        public Func<DeveloperTaskRequest, DeveloperTaskEvidence> ResultFactory { get; init; } = request => Evidence(request, DeveloperTaskStatus.Succeeded, "ok");

        public Task<DeveloperTaskEvidence> RunAsync(DeveloperTaskRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ThrowCancellation) throw new OperationCanceledException(cancellationToken);
            Requests.Add(request);
            return Task.FromResult(ResultFactory(request));
        }
    }

    private sealed class MemoryAudit : IRealtimeAuditSink
    {
        public List<string> Entries { get; } = [];
        public void Write(Guid sessionId, Guid correlationId, string name, object? data = null) =>
            Entries.Add(JsonSerializer.Serialize(new { sessionId, correlationId, name, data }));
    }

    private sealed class RecordingToolBridge : IRealtimeToolBridge
    {
        public List<RealtimeToolCall> Calls { get; } = [];
        public Func<RealtimeToolCall, RealtimeToolResult> ResultFactory { get; init; } =
            call => new(call.SessionId, call.CorrelationId, call.Name, RealtimeToolStatus.Succeeded, "computer status ok", new { status = "SUCCEEDED" });
        public IReadOnlyList<RealtimeToolDefinition> Tools { get; } =
            [new("computer_operation", "Operate computer through governance.", ["kind", "action"])];

        public Task<RealtimeToolResult> InvokeAsync(RealtimeToolCall call, CancellationToken cancellationToken = default)
        {
            Calls.Add(call);
            return Task.FromResult(ResultFactory(call));
        }
    }

    private sealed class DummyTool(GovernedToolDefinition definition) : IGovernedTool
    {
        public GovernedToolDefinition Definition { get; } = definition;

        public Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GovernedToolResult(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Succeeded, "ok"));
    }

    private sealed class FakeSecretStore(string? secret) : IJarvisSecretStore
    {
        public string Identifier => "fake-test-secret-store";
        public string? ReadSecret() => secret;
    }

    private sealed class FakeLocalConfigStore(RealtimeLocalConfiguration configuration) : IJarvisLocalConfigStore
    {
        public string Location => "fake-test-local-config";
        public RealtimeLocalConfiguration Read() => configuration;
    }

    private sealed class CaptureHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string RequestBody { get; private set; } = string.Empty;
        public string? ApiKeyHeader { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            RequestBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            ApiKeyHeader = request.Headers.TryGetValues("api-key", out var values) ? values.SingleOrDefault() : null;
            return response;
        }
    }
}
