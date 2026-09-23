using System.Text.Json;
using Jarvis.Governance;

namespace Jarvis.Computer;

public sealed class ComputerGovernedTool(Func<ComputerOperation, CancellationToken, Task<ComputerOperationResult>> dispatcher) : IGovernedTool
{
    public GovernedToolDefinition Definition { get; } = new(
        "computer_operation",
        "Perform a governed laptop-control operation with risk classification, confirmation and verification. For visible application-window state and control, use kind Application with action running, focus, minimize, maximize, restore, or close plus a human-readable name such as File Explorer or Downloads File Explorer window; do not provide or guess a process ID or window handle. Closing targets one resolved visible window, is graceful only, and may return ambiguity or user_action_required.",
        ["kind", "action"]);

    public async Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var args = request.Arguments;
        var dict = args.EnumerateObject()
            .Where(p => p.Value.ValueKind == JsonValueKind.String || p.Value.ValueKind == JsonValueKind.Number || p.Value.ValueKind == JsonValueKind.True || p.Value.ValueKind == JsonValueKind.False)
            .ToDictionary(p => p.Name, p => p.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        if (!Enum.TryParse<ComputerOperationKind>(dict["kind"], ignoreCase: true, out var kind))
            return new(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Rejected, "Unknown computer operation kind.", null, "unknown_computer_kind");

        var confirmed = args.TryGetProperty("confirmed", out var confirmedElement) && confirmedElement.ValueKind == JsonValueKind.True;
        var operation = new ComputerOperation(request.CorrelationId, kind, dict["action"], dict, confirmed);
        var result = await dispatcher(operation, cancellationToken).ConfigureAwait(false);
        return new(request.ConversationId, request.CorrelationId, request.Name, Map(result.Status), result.Message, result, result.FailureReason);
    }

    private static GovernedToolStatus Map(ComputerOperationStatus status) => status switch
    {
        ComputerOperationStatus.Succeeded => GovernedToolStatus.Succeeded,
        ComputerOperationStatus.ConfirmationRequired => GovernedToolStatus.AuthorizationRequired,
        ComputerOperationStatus.AuthorizationRequired => GovernedToolStatus.AuthorizationRequired,
        ComputerOperationStatus.TimedOut => GovernedToolStatus.TimedOut,
        ComputerOperationStatus.Cancelled => GovernedToolStatus.Cancelled,
        ComputerOperationStatus.VerificationFailed => GovernedToolStatus.VerificationFailed,
        ComputerOperationStatus.ProviderUnavailable or ComputerOperationStatus.Unavailable or ComputerOperationStatus.Unsupported => GovernedToolStatus.ProviderUnavailable,
        _ => GovernedToolStatus.Rejected
    };
}

public sealed class ComputerLaunchApplicationGovernedTool(Func<ComputerOperation, CancellationToken, Task<ComputerOperationResult>> dispatcher) : IGovernedTool
{
    public const string Name = "computer_launch_application";
    public GovernedToolDefinition Definition { get; } = new(
        Name,
        "Launch an installed Windows application through governed Computer control. Use this for requests like open Calculator, launch Word, start Excel, open Edge, File Explorer, Notepad, or Settings. Call this tool instead of narrating launch intent. Never claim success until the returned status is SUCCEEDED.",
        ["name"]);

    public async Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Arguments.GetProperty("name").GetString() ?? string.Empty;
        var confirmed = request.Arguments.TryGetProperty("confirmed", out var confirmedElement) && confirmedElement.ValueKind == JsonValueKind.True;
        var operation = new ComputerOperation(
            request.CorrelationId,
            ComputerOperationKind.Application,
            "launch",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["name"] = name },
            confirmed);
        var result = await dispatcher(operation, cancellationToken).ConfigureAwait(false);
        return new(request.ConversationId, request.CorrelationId, request.Name, Map(result.Status), result.Message, result, result.FailureReason);
    }

    private static GovernedToolStatus Map(ComputerOperationStatus status) => status switch
    {
        ComputerOperationStatus.Succeeded => GovernedToolStatus.Succeeded,
        ComputerOperationStatus.ConfirmationRequired => GovernedToolStatus.AuthorizationRequired,
        ComputerOperationStatus.AuthorizationRequired => GovernedToolStatus.AuthorizationRequired,
        ComputerOperationStatus.VerificationFailed => GovernedToolStatus.VerificationFailed,
        ComputerOperationStatus.TimedOut => GovernedToolStatus.TimedOut,
        ComputerOperationStatus.Cancelled => GovernedToolStatus.Cancelled,
        ComputerOperationStatus.ProviderUnavailable or ComputerOperationStatus.Unavailable or ComputerOperationStatus.Unsupported => GovernedToolStatus.ProviderUnavailable,
        _ => GovernedToolStatus.Rejected
    };
}

public sealed class ComputerStatusGovernedTool(Func<ComputerOperation, CancellationToken, Task<ComputerOperationResult>> dispatcher) : IGovernedTool
{
    public const string Name = "computer_get_status";
    public GovernedToolDefinition Definition { get; } = new(
        Name,
        "Return a governed system status snapshot with sanitized evidence.",
        []);

    public async Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var operation = new ComputerOperation(
            request.CorrelationId,
            ComputerOperationKind.System,
            "status",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            false);
        var result = await dispatcher(operation, cancellationToken).ConfigureAwait(false);
        var status = result.Status == ComputerOperationStatus.Succeeded ? GovernedToolStatus.Succeeded : GovernedToolStatus.Failed;
        return new(request.ConversationId, request.CorrelationId, request.Name, status, result.Message, result, result.FailureReason);
    }
}
