using System.Text.Json;
using Jarvis.Developer;

namespace Jarvis.Realtime;

public sealed class DeveloperTaskRunnerAdapter(DeveloperTaskOrchestrator orchestrator) : IDeveloperTaskRunner
{
    public Task<DeveloperTaskEvidence> RunAsync(DeveloperTaskRequest request, CancellationToken cancellationToken = default) =>
        orchestrator.RunAsync(request, cancellationToken);
}

public sealed class DeveloperRealtimeToolBridge(
    IDeveloperTaskRunner developer,
    IRealtimeAuditSink audit,
    IDevelopmentContextProvider? developmentContext = null) : IRealtimeToolBridge
{
    public const string RunDeveloperTaskTool = "run_developer_task";
    private readonly DevelopmentContextResolver contextResolver = new(new WorkspaceAuthorization());

    public IReadOnlyList<RealtimeToolDefinition> Tools { get; } =
    [
        new(RunDeveloperTaskTool, "Run a bounded Developer Jarvis task. If approved_workspace is omitted, recover it only from authoritative development context.", ["objective"])
    ];

    public async Task<RealtimeToolResult> InvokeAsync(RealtimeToolCall call, CancellationToken cancellationToken = default)
    {
        audit.Write(call.SessionId, call.CorrelationId, "tool_requested", new { call.Name });
        if (!string.Equals(call.Name, RunDeveloperTaskTool, StringComparison.Ordinal))
            return Reject(call, "unknown_tool", $"Unknown realtime tool: {call.Name}");

        if (!TryReadString(call.Arguments, "objective", out var objective) || string.IsNullOrWhiteSpace(objective))
            return Reject(call, "objective_required", "Developer task rejected: objective is required.");
        if (ContainsBlockedExpansion(call.Arguments))
            return Reject(call, "tool_arguments_request_authorization_expansion", "Developer task rejected: realtime tool arguments requested authority expansion.");
        TryReadString(call.Arguments, "approved_workspace", out var workspace);
        if (string.IsNullOrWhiteSpace(workspace))
        {
            var resolution = ResolveWorkspace(call, objective);
            if (!resolution.CanRun)
                return RejectWithBlocker(call, resolution);
            workspace = resolution.Workspace;
        }

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

        try
        {
            var evidence = await developer.RunAsync(request, cancellationToken).ConfigureAwait(false);
            var status = evidence.Status switch
            {
                DeveloperTaskStatus.Succeeded => RealtimeToolStatus.Succeeded,
                DeveloperTaskStatus.TimedOut => RealtimeToolStatus.TimedOut,
                DeveloperTaskStatus.PolicyRejected => RealtimeToolStatus.Rejected,
                _ => RealtimeToolStatus.Failed
            };
            var message = ToConversationMessage(evidence);
            object? resultEvidence = evidence;
            if (evidence.Status == DeveloperTaskStatus.PolicyRejected && IsWorkspaceFailure(evidence.FailureReason))
            {
                var blocker = BuildBlocker(call.Name, evidence.Objective, evidence.FailureReason ?? "workspace_not_authorized", developmentContext?.Current());
                developmentContext?.RememberBlocker(blocker);
                resultEvidence = new { developerEvidence = evidence, blocker };
                message = ToWorkspaceRecoveryMessage(blocker);
            }
            var result = new RealtimeToolResult(call.SessionId, call.CorrelationId, call.Name, status, message, resultEvidence, evidence.FailureReason);
            audit.Write(call.SessionId, call.CorrelationId, "tool_result", new { result.Status, result.FailureReason });
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var result = new RealtimeToolResult(call.SessionId, call.CorrelationId, call.Name, RealtimeToolStatus.Cancelled, "Developer task cancelled.", null, "cancelled");
            audit.Write(call.SessionId, call.CorrelationId, "tool_cancelled");
            return result;
        }
    }

    private static bool TryReadString(JsonElement args, string name, out string? value)
    {
        value = null;
        if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.String)
            return false;
        value = element.GetString();
        return true;
    }

    private static bool ContainsBlockedExpansion(JsonElement args)
    {
        var text = args.GetRawText();
        string[] blocked = ["credential", "api key", "api_key", "token", "secret", "password", "fish_audio_api_key", "fish_audio_reference_id", "administrator", "elevated", "deploy", "publish", "outside the workspace", "setx"];
        return blocked.Any(x => text.Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    private DevelopmentWorkspaceResolution ResolveWorkspace(RealtimeToolCall call, string objective)
    {
        var context = developmentContext?.Current() ?? new DevelopmentContextSnapshot(null, null, null, null, "none_configured", null, null, null, null, [], ["provide_approved_workspace"]);
        return contextResolver.Resolve(context, call.Name, objective);
    }

    private RealtimeToolResult Reject(RealtimeToolCall call, string reason, string message)
    {
        var result = new RealtimeToolResult(call.SessionId, call.CorrelationId, call.Name, RealtimeToolStatus.Rejected, message, null, reason);
        audit.Write(call.SessionId, call.CorrelationId, "tool_rejected", new { reason });
        return result;
    }

    private RealtimeToolResult RejectWithBlocker(RealtimeToolCall call, DevelopmentWorkspaceResolution resolution)
    {
        if (resolution.Blocker is not null)
            developmentContext?.RememberBlocker(resolution.Blocker);
        var evidence = new
        {
            failureCategory = resolution.Reason,
            requestedOperation = call.Name,
            missingPrerequisite = resolution.Blocker?.MissingPrerequisite,
            activeProject = resolution.Context.ActiveProjectName ?? resolution.Context.ActiveProjectId,
            workspaceState = resolution.Context.WorkspaceState,
            authorizedRecoveryActions = resolution.Blocker?.AuthorizedRecoveryActions ?? resolution.Context.RecoveryActions,
            userInputRequired = resolution.Blocker?.UserInputRequired ?? true,
            context = resolution.Context,
            blocker = resolution.Blocker
        };
        var result = new RealtimeToolResult(call.SessionId, call.CorrelationId, call.Name, RealtimeToolStatus.Rejected, resolution.Message, evidence, resolution.Reason);
        audit.Write(call.SessionId, call.CorrelationId, "tool_rejected", evidence);
        return result;
    }

    private static bool IsWorkspaceFailure(string? reason) => reason is "workspace_required" or "approved_workspace_required" or "workspace_missing" or "workspace_not_git_repository" or "workspace_path_invalid";

    private static DeveloperBlockerState BuildBlocker(string operation, string objective, string category, DevelopmentContextSnapshot? context) =>
        new(
            category,
            operation,
            "approved Git workspace",
            context?.ActiveProjectName ?? context?.ActiveProjectId,
            context?.WorkspaceState ?? "provided_workspace_rejected",
            ["provide_approved_workspace", "open_developer_workspace", "inspect_development_context"],
            true,
            DateTimeOffset.UtcNow);

    private static string ToWorkspaceRecoveryMessage(DeveloperBlockerState blocker) =>
        $"Developer task cannot run yet: {blocker.MissingPrerequisite} is not authorized for {blocker.ActiveProject ?? "the active project"}. Workspace state: {blocker.WorkspaceState}.";

    private static string ToConversationMessage(DeveloperTaskEvidence evidence) => evidence.Status switch
    {
        DeveloperTaskStatus.Succeeded => $"Developer task completed. {evidence.FinalReport}",
        DeveloperTaskStatus.PolicyRejected => $"Developer task stopped: {evidence.FailureReason}",
        DeveloperTaskStatus.TimedOut => "Developer task timed out safely.",
        _ => $"Developer task ended: {evidence.Status}. {evidence.FailureReason}"
    };
}
