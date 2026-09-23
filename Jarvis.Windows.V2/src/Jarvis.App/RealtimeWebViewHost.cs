using System.Text.Json;
using Jarvis.Realtime;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace Jarvis.App;

internal sealed class RealtimeWebViewHost(
    WebView2 webView,
    AzureRealtimeEphemeralSecretBroker broker,
    AzureRealtimeOptions options,
    IRealtimeToolBridge toolBridge,
    IRealtimeAuditSink audit) : IAsyncDisposable
{
    private readonly RealtimeDataChannelToolRouter toolRouter = new(toolBridge, audit);
    private readonly WebRtcReadinessGate readinessGate = new();
    private readonly RealtimeUiProjector uiProjector = new();
    private readonly RealtimeTurnTakingPolicy turnTaking = new();
    private readonly RealtimeUserTurnGate userTurnGate = new();
    private WebRtcTransportEvidenceBuilder? evidence;
    private Guid sessionId;
    private bool initialized;
    private bool currentUserUtteranceSawDelta;
    private bool currentAssistantResponseSawToolCall;
    private string pendingAcceptedTurnId = string.Empty;
    private string activeResponseTurnId = string.Empty;

    public event Action<string>? ActivityReceived;
    public event Action<RealtimeUiProjection>? UiProjectionReceived;
    public RealtimeProviderStatus CurrentStatus { get; private set; } =
        new(false, false, "Azure/OpenAI GA Realtime", "not_started", []);

    public async Task<RealtimeProviderStatus> StartAsync(CancellationToken cancellationToken = default)
    {
        sessionId = Guid.NewGuid();
        evidence = new WebRtcTransportEvidenceBuilder(sessionId);
        await EnsureInitializedAsync().ConfigureAwait(true);

        var credential = await broker.MintAsync(sessionId, toolBridge.Tools, cancellationToken).ConfigureAwait(true);
        var config = JsonSerializer.Serialize(new
        {
            sessionId,
            clientSecret = credential.ClientSecret,
            callsEndpoint = credential.CallsEndpoint.ToString()
        });
        await webView.ExecuteScriptAsync($"window.JarvisRealtimeTransport.startRealtime({config});").AsTask().ConfigureAwait(true);
        CurrentStatus = readinessGate.ToProviderStatus(evidence.Build());
        audit.Write(sessionId, Guid.NewGuid(), "realtime_webview_start_invoked", new { CurrentStatus.Reason });
        return CurrentStatus;
    }

    public async Task InterruptAsync(string reason)
    {
        if (!initialized)
            return;
        audit.Write(sessionId, Guid.NewGuid(), "realtime_interrupt_requested", new { reason });
        await webView.ExecuteScriptAsync("window.JarvisRealtimeTransport.interruptRealtime();").AsTask().ConfigureAwait(true);
    }

    public async Task StopAsync()
    {
        if (!initialized)
            return;
        await webView.ExecuteScriptAsync("window.JarvisRealtimeTransport.stopRealtime();").AsTask().ConfigureAwait(true);
        CurrentStatus = readinessGate.ToProviderStatus(evidence?.Build() ?? new WebRtcTransportEvidence(sessionId, WebRtcTransportState.Closed, false, false, false, false, false, false, "closed"));
        audit.Write(sessionId, Guid.NewGuid(), "realtime_webview_stop_invoked");
    }

    private async Task EnsureInitializedAsync()
    {
        if (initialized)
            return;

        await webView.EnsureCoreWebView2Async().AsTask().ConfigureAwait(true);
        var realtimeRoot = Path.Combine(AppContext.BaseDirectory, "RealtimeWeb");
        webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "jarvis-realtime.local",
            realtimeRoot,
            CoreWebView2HostResourceAccessKind.Allow);
        webView.CoreWebView2.PermissionRequested += (_, args) =>
        {
            if (args.PermissionKind == CoreWebView2PermissionKind.Microphone &&
                string.Equals(new Uri(args.Uri).Host, "jarvis-realtime.local", StringComparison.OrdinalIgnoreCase))
            {
                args.State = CoreWebView2PermissionState.Allow;
            }
        };
        webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
        webView.Source = new Uri("https://jarvis-realtime.local/realtime-transport.html");
        initialized = true;
    }

    private async void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            using var document = JsonDocument.Parse(args.WebMessageAsJson);
            var root = document.RootElement;
            var type = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() ?? string.Empty : string.Empty;
            var payload = root.TryGetProperty("payload", out var payloadElement)
                ? ReadWebViewPayload(payloadElement)
                : null;
            ObserveTransport(type, payload);
            if (string.Equals(type, "data.channel.open", StringComparison.Ordinal))
            {
                await SendRealtimeEventAsync(BuildRealtimeSessionUpdateJson()).ConfigureAwait(true);
            }

            if (string.Equals(type, "data.channel.message", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(payload))
            {
                PublishRealtimeEvidence(payload);
                WriteTranscriptEvidence("realtime_transcript_event_received", payload);
                PublishTurnTakingEvidence(payload);
                var eventType = TryReadEventType(payload);
                if (string.Equals(eventType, "conversation.item.input_audio_transcription.completed", StringComparison.Ordinal))
                {
                    var decision = userTurnGate.EvaluateInputTranscriptionCompleted(payload);
                    audit.Write(sessionId, Guid.NewGuid(), decision.Accepted ? "USER_TURN_ACCEPTED" : "USER_TURN_REJECTED", new
                    {
                        turnId = decision.TurnId,
                        itemId = decision.ItemId,
                        reason = decision.Accepted ? "accepted" : decision.Reason,
                        transcriptCharacterCount = decision.Transcript.Length
                    });
                    if (!decision.Accepted)
                        return;

                    pendingAcceptedTurnId = decision.TurnId;
                    await SendRealtimeEventAsync(BuildResponseCreateJson()).ConfigureAwait(true);
                }

                var projection = uiProjector.ProjectServerEvent(payload);
                WriteTranscriptProjectionEvidence(projection, payload);
                PublishUiProjection(projection);
                if (projection.Kind is RealtimeUiProjectionKind.AssistantResponseStarted)
                {
                    currentAssistantResponseSawToolCall = false;
                    userTurnGate.ObserveAssistantResponseStarted();
                    if (!string.IsNullOrWhiteSpace(pendingAcceptedTurnId) &&
                        userTurnGate.TryMarkResponseStarted(pendingAcceptedTurnId))
                    {
                        activeResponseTurnId = pendingAcceptedTurnId;
                        audit.Write(sessionId, Guid.NewGuid(), "RESPONSE_STARTED", new { turnId = activeResponseTurnId, projection.Id });
                        pendingAcceptedTurnId = string.Empty;
                    }
                    await webView.ExecuteScriptAsync("window.JarvisRealtimeTransport.resumeRemoteAudio();").AsTask().ConfigureAwait(true);
                }
                if (projection.Kind is RealtimeUiProjectionKind.AssistantResponseUpdated or RealtimeUiProjectionKind.AssistantResponseCompleted)
                    userTurnGate.ObserveAssistantTranscript(projection.Text);
                if (projection.Kind is RealtimeUiProjectionKind.AssistantResponseCompleted or RealtimeUiProjectionKind.AssistantInterrupted)
                {
                    userTurnGate.ObserveAssistantResponseCompleted();
                    if (!string.IsNullOrWhiteSpace(activeResponseTurnId))
                    {
                        audit.Write(sessionId, Guid.NewGuid(), "RESPONSE_COMPLETED", new { turnId = activeResponseTurnId, projectionKind = projection.Kind.ToString() });
                        activeResponseTurnId = string.Empty;
                    }
                }
                var toolResponses = await toolRouter.HandleServerEventAsync(sessionId, payload).ConfigureAwait(true);
                if (toolResponses.Count > 0)
                    currentAssistantResponseSawToolCall = true;
                foreach (var response in toolResponses)
                {
                    await SendRealtimeEventAsync(response).ConfigureAwait(true);
                }
                WriteNarratedToolUseEvidence(projection);
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            audit.Write(sessionId, Guid.NewGuid(), "realtime_webview_message_rejected", new { ex.GetType().Name, ex.Message });
        }
    }

    private void ObserveTransport(string type, string? payload)
    {
        if (evidence is null)
            return;

        evidence.Apply(new WebRtcBridgeMessage(type, sessionId, payload));
        CurrentStatus = readinessGate.ToProviderStatus(evidence.Build());
        if (!string.Equals(type, "data.channel.message", StringComparison.Ordinal))
        {
            if (type is "mic.stream.started")
                audit.Write(sessionId, Guid.NewGuid(), "REALTIME_EFFECTIVE_AUDIO_CONSTRAINTS", ReadAudioConstraintEvidence(payload));
            if (type is "assistant.playback.active" or "remote.audio.playing")
            {
                userTurnGate.SetAssistantPlaybackActive(true);
                audit.Write(sessionId, Guid.NewGuid(), "ASSISTANT_PLAYBACK_ACTIVE", new { active = true, source = type });
            }
            if (type is "assistant.playback.inactive")
            {
                userTurnGate.SetAssistantPlaybackActive(false);
                audit.Write(sessionId, Guid.NewGuid(), "ASSISTANT_PLAYBACK_ACTIVE", new { active = false, reason = payload ?? "" });
            }
            if (type is "remote.audio.flushed")
                audit.Write(sessionId, Guid.NewGuid(), "REMOTE_AUDIO_FLUSHED", new { source = "webview_transport" });
            if (type is "assistant.item.truncate.not_required")
                audit.Write(sessionId, Guid.NewGuid(), "ASSISTANT_ITEM_TRUNCATED", new { required = false, reason = payload ?? "provider_managed_audio" });
            if (type is "interrupt.sent")
                audit.Write(sessionId, Guid.NewGuid(), "ACTIVE_RESPONSE_CANCEL_REQUESTED", new { source = "webview_transport" });
            audit.Write(sessionId, Guid.NewGuid(), "realtime_webview_transport_event", new
            {
                type,
                status = CurrentStatus.Reason,
                CurrentStatus.PhysicalAudioReady
            });
        }

        PublishUiProjection(uiProjector.ProjectTransport(type));

        if (ShouldPublishActivity(type))
            ActivityReceived?.Invoke(type);
    }

    private void PublishUiProjection(RealtimeUiProjection projection)
    {
        if (projection.Kind != RealtimeUiProjectionKind.None)
            UiProjectionReceived?.Invoke(projection);
    }

    private void WriteTranscriptEvidence(string name, string payload)
    {
        var metadata = TryReadTranscriptMetadata(payload);
        if (metadata is not null)
            audit.Write(sessionId, Guid.NewGuid(), name, metadata);
    }

    private void PublishRealtimeEvidence(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            var eventType = ReadString(root, "type");
            switch (eventType)
            {
                case "session.created":
                case "session.updated":
                    audit.Write(sessionId, Guid.NewGuid(), "REALTIME_SESSION_READY", new { eventType });
                    if (string.Equals(eventType, "session.updated", StringComparison.Ordinal))
                        WriteEffectiveSessionValidation(root);
                    break;
                case "input_audio_buffer.speech_started":
                    currentUserUtteranceSawDelta = false;
                    audit.Write(sessionId, Guid.NewGuid(), "USER_SPEECH_STARTED", new { eventType });
                    break;
                case "input_audio_buffer.speech_stopped":
                    audit.Write(sessionId, Guid.NewGuid(), "USER_SPEECH_STOPPED", new { eventType });
                    break;
                case "conversation.item.input_audio_transcription.delta":
                    currentUserUtteranceSawDelta = true;
                    audit.Write(sessionId, Guid.NewGuid(), "USER_TRANSCRIPT_DELTA", TryReadTranscriptMetadata(payload));
                    break;
                case "conversation.item.input_audio_transcription.completed":
                    audit.Write(sessionId, Guid.NewGuid(), "USER_TRANSCRIPT_FINAL", TryReadTranscriptMetadata(payload));
                    if (!currentUserUtteranceSawDelta)
                    {
                        audit.Write(sessionId, Guid.NewGuid(), "USER_LIVE_TRANSCRIPT_PROVIDER_LIMITATION", new
                        {
                            reason = "provider_emitted_final_input_transcription_without_prior_delta",
                            eventType
                        });
                    }
                    break;
                case "response.audio_transcript.delta":
                case "response.output_audio_transcript.delta":
                case "response.text.delta":
                case "response.output_text.delta":
                    audit.Write(sessionId, Guid.NewGuid(), "ASSISTANT_TRANSCRIPT_DELTA", TryReadTranscriptMetadata(payload));
                    break;
            }
        }
        catch (JsonException)
        {
            audit.Write(sessionId, Guid.NewGuid(), "realtime_provider_event_rejected", new { reason = "invalid_json" });
        }
    }

    private void WriteEffectiveSessionValidation(JsonElement root)
    {
        var toolNames = new HashSet<string>(StringComparer.Ordinal);
        var toolChoice = string.Empty;
        if (root.TryGetProperty("session", out var session) && session.ValueKind == JsonValueKind.Object)
        {
            toolChoice = ReadScalarAsString(session, "tool_choice");
            if (session.TryGetProperty("tools", out var tools) && tools.ValueKind == JsonValueKind.Array)
            {
                foreach (var tool in tools.EnumerateArray())
                {
                    if (tool.ValueKind != JsonValueKind.Object ||
                        !string.Equals(ReadString(tool, "type"), "function", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var name = ReadString(tool, "name");
                    if (!string.IsNullOrWhiteSpace(name))
                        toolNames.Add(name);
                }
            }
        }

        var hasComputerOperation = toolNames.Contains("computer_operation");
        var hasComputerLaunch = toolNames.Contains("computer_launch_application");
        var hasComputerStatus = toolNames.Contains("computer_get_status");
        var expectedToolChoice = string.Equals(toolChoice, "auto", StringComparison.Ordinal);
        audit.Write(sessionId, Guid.NewGuid(), "REALTIME_EFFECTIVE_SESSION_VALIDATED", new
        {
            computerToolDefinitionsPresent = hasComputerOperation && hasComputerLaunch && hasComputerStatus,
            hasComputerOperation,
            hasComputerLaunch,
            hasComputerStatus,
            toolChoice = expectedToolChoice ? "auto" : "missing_or_other",
            validation = hasComputerOperation && hasComputerLaunch && hasComputerStatus && expectedToolChoice ? "PASSED" : "FAILED"
        });
    }

    private void WriteTranscriptProjectionEvidence(RealtimeUiProjection projection, string payload)
    {
        if (projection.Kind is not (RealtimeUiProjectionKind.UserTranscriptUpdated or
            RealtimeUiProjectionKind.UserTranscriptCompleted or
            RealtimeUiProjectionKind.AssistantResponseUpdated or
            RealtimeUiProjectionKind.AssistantResponseCompleted or
            RealtimeUiProjectionKind.AssistantInterrupted))
        {
            return;
        }

        var metadata = TryReadTranscriptMetadata(payload);
        audit.Write(sessionId, Guid.NewGuid(), "realtime_transcript_event_projected", new
        {
            projectionKind = projection.Kind.ToString(),
            projection.Id,
            projectedCharacterCount = projection.Text.Length,
            provider = metadata
        });
    }

    private void WriteNarratedToolUseEvidence(RealtimeUiProjection projection)
    {
        if (currentAssistantResponseSawToolCall ||
            projection.Kind is not (RealtimeUiProjectionKind.AssistantResponseUpdated or RealtimeUiProjectionKind.AssistantResponseCompleted) ||
            !RealtimeToolNarrationGuard.LooksLikeNarratedToolUse(projection.Text))
        {
            return;
        }

        audit.Write(sessionId, Guid.NewGuid(), "REALTIME_TOOL_BRIDGE_MISS", new
        {
            reason = "assistant_narrated_computer_action_without_tool_call",
            projectedCharacterCount = projection.Text.Length,
            projection.Id
        });
    }

    private static object? TryReadTranscriptMetadata(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            var eventType = ReadString(root, "type");
            if (!IsTranscriptEvent(eventType))
                return null;

            var deltaField = FirstPresentStringName(root, "delta", "text", "transcript");
            var deltaCharacterCount = deltaField is null ? 0 : ReadString(root, deltaField).Length;
            return new
            {
                eventType,
                itemId = ReadString(root, "item_id"),
                responseId = ReadString(root, "response_id"),
                outputIndex = ReadScalarAsString(root, "output_index"),
                contentIndex = ReadScalarAsString(root, "content_index"),
                textField = deltaField ?? "",
                deltaCharacterCount
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string TryReadEventType(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            return ReadString(document.RootElement, "type");
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static object ReadAudioConstraintEvidence(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return new { echoCancellation = "unknown", noiseSuppression = "unknown", autoGainControl = "unknown" };
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            return new
            {
                echoCancellation = ReadConstraint(root, "echoCancellation"),
                noiseSuppression = ReadConstraint(root, "noiseSuppression"),
                autoGainControl = ReadConstraint(root, "autoGainControl")
            };
        }
        catch (JsonException)
        {
            return new { echoCancellation = "unknown", noiseSuppression = "unknown", autoGainControl = "unknown" };
        }
    }

    private static string ReadConstraint(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
            return "missing";
        return value.ValueKind switch
        {
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.String => value.GetString() ?? "unknown",
            _ => value.ToString()
        };
    }

    private static bool IsTranscriptEvent(string eventType) => eventType is
        "conversation.item.input_audio_transcription.delta" or
        "conversation.item.input_audio_transcription.completed" or
        "conversation.item.input_audio_transcription.failed" or
        "response.audio_transcript.delta" or
        "response.audio_transcript.done" or
        "response.output_audio_transcript.delta" or
        "response.output_audio_transcript.done" or
        "response.text.delta" or
        "response.text.done" or
        "response.output_text.delta" or
        "response.output_text.done" or
        "response.done";

    private static string? FirstPresentStringName(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return name;
        }

        return null;
    }

    private static string ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string? ReadWebViewPayload(JsonElement payload) =>
        payload.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => payload.GetString(),
            _ => payload.GetRawText()
        };

    private static string ReadScalarAsString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
            return string.Empty;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.ToString(),
            _ => string.Empty
        };
    }

    private void PublishTurnTakingEvidence(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventType = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() ?? string.Empty : string.Empty;
        foreach (var evidenceItem in turnTaking.Observe(eventType))
        {
            audit.Write(sessionId, Guid.NewGuid(), ToEvidenceName(evidenceItem.Kind), new
            {
                kind = evidenceItem.Kind.ToString(),
                evidenceItem.Reason
            });
        }
    }

    private static string ToEvidenceName(RealtimeTurnEvidenceKind kind) => kind switch
    {
        RealtimeTurnEvidenceKind.AssistantInterrupted => "BARGE_IN_DETECTED",
        RealtimeTurnEvidenceKind.AssistantCancellationRequested => "ACTIVE_RESPONSE_CANCEL_REQUESTED",
        RealtimeTurnEvidenceKind.AssistantAudioFlushRequested => "REMOTE_AUDIO_FLUSHED",
        RealtimeTurnEvidenceKind.NewUserUtteranceAccepted => "INTERRUPTING_UTTERANCE_RECEIVED",
        RealtimeTurnEvidenceKind.AssistantResponseStarted => "POST_INTERRUPT_RESPONSE_STARTED",
        RealtimeTurnEvidenceKind.AssistantResponseCompleted => "BARGE_IN_COMPLETED",
        RealtimeTurnEvidenceKind.UserSpeechStarted => "USER_SPEECH_STARTED",
        RealtimeTurnEvidenceKind.UserSpeechStopped => "USER_SPEECH_STOPPED",
        _ => "realtime_turn_taking"
    };

    private static bool ShouldPublishActivity(string type) => type is
        "webview.ready" or
        "mic.stream.started" or
        "remote.audio.track" or
        "peer.connection.established" or
        "remote.audio.playing" or
        "data.channel.open" or
        "interrupt.sent" or
        "remote.audio.flushed" or
        "assistant.item.truncate.not_required" or
        "closed" or
        "fault";

    private string BuildRealtimeSessionUpdateJson() =>
        JsonSerializer.Serialize(RealtimeSessionContract.BuildSessionUpdatePayload(options, toolBridge.Tools));

    private static string BuildResponseCreateJson() =>
        JsonSerializer.Serialize(new
        {
            type = "response.create"
        });

    private Task SendRealtimeEventAsync(string eventJson)
    {
        var escaped = JsonSerializer.Serialize(eventJson);
        return webView.ExecuteScriptAsync($"window.JarvisRealtimeTransport.sendRealtimeEvent(JSON.parse({escaped}));").AsTask();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(true);
    }
}
