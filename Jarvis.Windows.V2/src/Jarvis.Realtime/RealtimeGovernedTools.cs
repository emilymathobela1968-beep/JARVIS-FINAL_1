using System.Text.Json;
using Jarvis.Developer;
using Jarvis.Governance;

namespace Jarvis.Realtime;

public sealed class DeveloperGovernedTool(
    IDeveloperTaskRunner developer,
    IDevelopmentContextProvider? developmentContext = null) : IGovernedTool
{
    public const string Name = "run_developer_task";
    private readonly DevelopmentContextResolver contextResolver = new(new WorkspaceAuthorization());
    public GovernedToolDefinition Definition { get; } = new(Name, "Run a bounded Developer Jarvis task. If approved_workspace is omitted, recover it only from authoritative development context.", ["objective"]);

    public async Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var workspace = request.Arguments.TryGetProperty("approved_workspace", out var workspaceElement) && workspaceElement.ValueKind == JsonValueKind.String
            ? workspaceElement.GetString() ?? string.Empty
            : string.Empty;
        var objective = request.Arguments.GetProperty("objective").GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(workspace))
        {
            var context = developmentContext?.Current() ?? new DevelopmentContextSnapshot(null, null, null, null, "none_configured", null, null, null, null, [], ["provide_approved_workspace"]);
            var resolution = contextResolver.Resolve(context, request.Name, objective);
            if (!resolution.CanRun)
            {
                if (resolution.Blocker is not null)
                    developmentContext?.RememberBlocker(resolution.Blocker);
                return new(
                    request.ConversationId,
                    request.CorrelationId,
                    request.Name,
                    GovernedToolStatus.Rejected,
                    resolution.Message,
                    new
                    {
                        failureCategory = resolution.Reason,
                        requestedOperation = request.Name,
                        missingPrerequisite = resolution.Blocker?.MissingPrerequisite,
                        activeProject = resolution.Context.ActiveProjectName ?? resolution.Context.ActiveProjectId,
                        workspaceState = resolution.Context.WorkspaceState,
                        authorizedRecoveryActions = resolution.Blocker?.AuthorizedRecoveryActions ?? resolution.Context.RecoveryActions,
                        userInputRequired = resolution.Blocker?.UserInputRequired ?? true,
                        context = resolution.Context,
                        blocker = resolution.Blocker
                    },
                    resolution.Reason);
            }

            workspace = resolution.Workspace!;
        }
        var developerRequest = new DeveloperTaskRequest(
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

        var evidence = await developer.RunAsync(developerRequest, cancellationToken).ConfigureAwait(false);
        var status = evidence.Status switch
        {
            DeveloperTaskStatus.Succeeded => GovernedToolStatus.Succeeded,
            DeveloperTaskStatus.TimedOut => GovernedToolStatus.TimedOut,
            DeveloperTaskStatus.PolicyRejected => GovernedToolStatus.Rejected,
            _ => GovernedToolStatus.Failed
        };
        var message = ToMessage(evidence);
        object? outputEvidence = evidence;
        if (evidence.Status == DeveloperTaskStatus.PolicyRejected && IsWorkspaceFailure(evidence.FailureReason))
        {
            var context = developmentContext?.Current();
            var blocker = new DeveloperBlockerState(
                evidence.FailureReason ?? "workspace_not_authorized",
                request.Name,
                "approved Git workspace",
                context?.ActiveProjectName ?? context?.ActiveProjectId,
                context?.WorkspaceState ?? "provided_workspace_rejected",
                ["provide_approved_workspace", "open_developer_workspace", "inspect_development_context"],
                true,
                DateTimeOffset.UtcNow);
            developmentContext?.RememberBlocker(blocker);
            outputEvidence = new { developerEvidence = evidence, blocker };
            message = $"Developer task cannot run yet: {blocker.MissingPrerequisite} is not authorized for {blocker.ActiveProject ?? "the active project"}. Workspace state: {blocker.WorkspaceState}.";
        }
        return new(request.ConversationId, request.CorrelationId, request.Name, status, message, outputEvidence, evidence.FailureReason);
    }

    private static bool IsWorkspaceFailure(string? reason) => reason is "workspace_required" or "approved_workspace_required" or "workspace_missing" or "workspace_not_git_repository" or "workspace_path_invalid";

    private static string ToMessage(DeveloperTaskEvidence evidence) => evidence.Status switch
    {
        DeveloperTaskStatus.Succeeded => $"Developer task completed. {evidence.FinalReport}",
        DeveloperTaskStatus.PolicyRejected => $"Developer task stopped: {evidence.FailureReason}",
        DeveloperTaskStatus.TimedOut => "Developer task timed out safely.",
        _ => $"Developer task ended: {evidence.Status}. {evidence.FailureReason}"
    };
}

public sealed class RealtimeGovernedToolBridge(GovernedCapabilityRegistry registry) : IRealtimeToolBridge
{
    public IReadOnlyList<RealtimeToolDefinition> Tools { get; } = registry.Definitions
        .Select(tool => new RealtimeToolDefinition(tool.Name, tool.Description, tool.RequiredArguments))
        .ToArray();

    public async Task<RealtimeToolResult> InvokeAsync(RealtimeToolCall call, CancellationToken cancellationToken = default)
    {
        var result = await registry.DispatchAsync(new GovernedToolRequest(call.SessionId, call.CorrelationId, call.Name, call.Arguments), cancellationToken).ConfigureAwait(false);
        return new(call.SessionId, call.CorrelationId, call.Name, Map(result.Status), result.Message, result.Evidence, result.FailureReason);
    }

    private static RealtimeToolStatus Map(GovernedToolStatus status) => status switch
    {
        GovernedToolStatus.Succeeded => RealtimeToolStatus.Succeeded,
        GovernedToolStatus.AuthorizationRequired => RealtimeToolStatus.ConfirmationRequired,
        GovernedToolStatus.ProviderUnavailable or GovernedToolStatus.ReferenceRequired => RealtimeToolStatus.Unavailable,
        GovernedToolStatus.VerificationFailed => RealtimeToolStatus.VerificationFailed,
        GovernedToolStatus.TimedOut => RealtimeToolStatus.TimedOut,
        GovernedToolStatus.Cancelled => RealtimeToolStatus.Cancelled,
        GovernedToolStatus.Failed => RealtimeToolStatus.Failed,
        _ => RealtimeToolStatus.Rejected
    };
}

public sealed class CapabilityInventoryGovernedTool(Func<IReadOnlyList<GovernedToolDefinition>> definitions) : IGovernedTool
{
    public const string Name = "capability_inventory";
    public GovernedToolDefinition Definition { get; } = new(
        Name,
        "Return the machine-readable governed capability inventory, including tool names and required arguments.",
        []);

    public Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var inventory = definitions().Select(tool => new
        {
            tool.Name,
            tool.Description,
            requiredArguments = tool.RequiredArguments
        }).ToArray();
        return Task.FromResult(new GovernedToolResult(
            request.ConversationId,
            request.CorrelationId,
            request.Name,
            GovernedToolStatus.Succeeded,
            "Capability inventory returned.",
            new { capabilities = inventory }));
    }
}

public sealed class EmergencyAssistanceGovernedTool : IGovernedTool
{
    public const string Name = "emergency_assistance";
    public GovernedToolDefinition Definition { get; } = new(
        Name,
        "Report emergency assistance provider availability without bypassing governance or claiming external contact.",
        ["requested_help"]);

    public Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new GovernedToolResult(
            request.ConversationId,
            request.CorrelationId,
            request.Name,
            GovernedToolStatus.ProviderUnavailable,
            "Emergency provider is not configured. No external emergency contact was made.",
            new { provider = "not_configured", outboundContact = "not_attempted" },
            "EMERGENCY_PROVIDER_UNAVAILABLE"));
}

public sealed class RealtimeGovernanceAuditAdapter(IRealtimeAuditSink realtimeAudit) : IGovernedToolAudit
{
    public void Write(Guid conversationId, Guid correlationId, string name, object? data = null) =>
        realtimeAudit.Write(conversationId, correlationId, name, data);
}
