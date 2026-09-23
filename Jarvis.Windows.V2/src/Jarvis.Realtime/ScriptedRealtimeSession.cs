using System.Text.Json;

namespace Jarvis.Realtime;

public sealed class ScriptedRealtimeSession(IRealtimeToolBridge tools, IRealtimeAuditSink audit) : IRealtimeJarvisSession
{
    public event EventHandler<RealtimeConversationEvent>? EventReceived;
    public RealtimeSessionState State { get; private set; } = RealtimeSessionState.Stopped;
    private RealtimeSessionOptions? options;

    public Task StartAsync(RealtimeSessionOptions options, CancellationToken cancellationToken = default)
    {
        this.options = options;
        State = RealtimeSessionState.Connected;
        Emit(RealtimeEventKind.Lifecycle, "realtime_started", options.SessionId, Guid.NewGuid(), "Realtime session started.");
        return Task.CompletedTask;
    }

    public Task StopAsync(string reason, CancellationToken cancellationToken = default)
    {
        var sessionId = options?.SessionId ?? Guid.Empty;
        State = RealtimeSessionState.Stopped;
        Emit(RealtimeEventKind.Lifecycle, "realtime_stopped", sessionId, Guid.NewGuid(), reason);
        return Task.CompletedTask;
    }

    public Task InterruptAsync(string reason, CancellationToken cancellationToken = default)
    {
        var sessionId = options?.SessionId ?? Guid.Empty;
        State = RealtimeSessionState.Interrupted;
        Emit(RealtimeEventKind.Lifecycle, "realtime_interrupted", sessionId, Guid.NewGuid(), reason);
        return Task.CompletedTask;
    }

    public async Task SendUserTextAsync(string text, CancellationToken cancellationToken = default)
    {
        if (options is null) throw new InvalidOperationException("Realtime session has not started.");
        var correlationId = Guid.NewGuid();
        State = RealtimeSessionState.ConversationActive;
        Emit(RealtimeEventKind.UserText, "user_text", options.SessionId, correlationId, text);

        if (!TryBuildDeveloperCall(text, options.SessionId, correlationId, out var call))
        {
            Emit(RealtimeEventKind.AssistantText, "assistant_text", options.SessionId, correlationId, "I can discuss that, but I do not have a governed tool request to run.");
            return;
        }

        State = RealtimeSessionState.ToolRunning;
        Emit(RealtimeEventKind.ToolRequested, "tool_requested", options.SessionId, correlationId, call.Name, new { call.Name });
        var result = await tools.InvokeAsync(call, cancellationToken).ConfigureAwait(false);
        await SubmitToolResultAsync(result, cancellationToken).ConfigureAwait(false);
    }

    public Task SubmitToolResultAsync(RealtimeToolResult result, CancellationToken cancellationToken = default)
    {
        Emit(RealtimeEventKind.ToolResult, "tool_result", result.SessionId, result.CorrelationId, result.Message, result);
        State = RealtimeSessionState.Connected;
        Emit(RealtimeEventKind.AssistantText, "assistant_text", result.SessionId, result.CorrelationId, result.Message);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static bool TryBuildDeveloperCall(string text, Guid sessionId, Guid correlationId, out RealtimeToolCall call)
    {
        call = default!;
        const string workspaceMarker = "workspace:";
        const string objectiveMarker = "objective:";
        var workspaceIndex = text.IndexOf(workspaceMarker, StringComparison.OrdinalIgnoreCase);
        var objectiveIndex = text.IndexOf(objectiveMarker, StringComparison.OrdinalIgnoreCase);
        if (workspaceIndex < 0 || objectiveIndex < 0 || objectiveIndex <= workspaceIndex)
            return false;

        var workspace = text[(workspaceIndex + workspaceMarker.Length)..objectiveIndex].Trim();
        var objective = text[(objectiveIndex + objectiveMarker.Length)..].Trim();
        var args = JsonSerializer.SerializeToElement(new { approved_workspace = workspace, objective });
        call = new RealtimeToolCall(sessionId, correlationId, DeveloperRealtimeToolBridge.RunDeveloperTaskTool, args);
        return true;
    }

    private void Emit(RealtimeEventKind kind, string name, Guid sessionId, Guid correlationId, string? text = null, object? data = null)
    {
        audit.Write(sessionId, correlationId, name, data ?? new { text });
        EventReceived?.Invoke(this, new(sessionId, correlationId, kind, name, text, data));
    }
}
