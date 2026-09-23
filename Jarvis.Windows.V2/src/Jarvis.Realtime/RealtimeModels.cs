using System.Text.Json;
using Jarvis.Developer;

namespace Jarvis.Realtime;

public enum RealtimeSessionState { Stopped, Starting, Connected, ConversationActive, ToolRunning, Interrupted, Faulted }
public enum RealtimeEventKind { Lifecycle, UserText, AssistantText, AssistantAudio, ToolRequested, ToolResult, Audit }
public enum RealtimeToolStatus
{
    Requested,
    Authorized,
    ConfirmationRequired,
    Denied,
    Executing,
    Succeeded,
    Failed,
    Unavailable,
    VerificationFailed,
    Rejected,
    Cancelled,
    TimedOut
}

public sealed record RealtimeSessionOptions(
    Guid SessionId,
    string Provider,
    string Model,
    IReadOnlyList<RealtimeToolDefinition> Tools);

public sealed record RealtimeToolDefinition(
    string Name,
    string Description,
    IReadOnlyList<string> RequiredArguments);

public sealed record RealtimeConversationEvent(
    Guid SessionId,
    Guid CorrelationId,
    RealtimeEventKind Kind,
    string Name,
    string? Text = null,
    object? Data = null);

public sealed record RealtimeToolCall(
    Guid SessionId,
    Guid CorrelationId,
    string Name,
    JsonElement Arguments);

public sealed record RealtimeToolResult(
    Guid SessionId,
    Guid CorrelationId,
    string Name,
    RealtimeToolStatus Status,
    string Message,
    object? Evidence = null,
    string? FailureReason = null);

public sealed record RealtimeProviderStatus(
    bool Configured,
    bool PhysicalAudioReady,
    string Provider,
    string Reason,
    IReadOnlyList<string> RequiredConfiguration);

public interface IRealtimeAuditSink
{
    void Write(Guid sessionId, Guid correlationId, string name, object? data = null);
}

public interface IRealtimeJarvisSession : IAsyncDisposable
{
    event EventHandler<RealtimeConversationEvent>? EventReceived;
    RealtimeSessionState State { get; }
    Task StartAsync(RealtimeSessionOptions options, CancellationToken cancellationToken = default);
    Task StopAsync(string reason, CancellationToken cancellationToken = default);
    Task InterruptAsync(string reason, CancellationToken cancellationToken = default);
    Task SendUserTextAsync(string text, CancellationToken cancellationToken = default);
    Task SubmitToolResultAsync(RealtimeToolResult result, CancellationToken cancellationToken = default);
}

public interface IRealtimeToolBridge
{
    IReadOnlyList<RealtimeToolDefinition> Tools { get; }
    Task<RealtimeToolResult> InvokeAsync(RealtimeToolCall call, CancellationToken cancellationToken = default);
}

public interface IDeveloperTaskRunner
{
    Task<DeveloperTaskEvidence> RunAsync(DeveloperTaskRequest request, CancellationToken cancellationToken = default);
}
