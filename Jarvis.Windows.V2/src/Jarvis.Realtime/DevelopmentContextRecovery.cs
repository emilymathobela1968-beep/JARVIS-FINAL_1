using Jarvis.Developer;
using Jarvis.Governance;

namespace Jarvis.Realtime;

public sealed record DevelopmentWorkspaceCandidate(string Path, string Source);

public sealed record DevelopmentContextSnapshot(
    string? ActiveProjectId,
    string? ActiveProjectName,
    string? ActiveObjective,
    string? ActiveWorkspace,
    string WorkspaceState,
    string? ActiveScreen,
    string? ActiveRevision,
    string? CurrentDevelopmentTask,
    DeveloperBlockerState? PreviousBlocker,
    IReadOnlyList<DevelopmentWorkspaceCandidate> CandidateWorkspaces,
    IReadOnlyList<string> RecoveryActions);

public sealed record DeveloperBlockerState(
    string FailureCategory,
    string RequestedOperation,
    string MissingPrerequisite,
    string? ActiveProject,
    string WorkspaceState,
    IReadOnlyList<string> AuthorizedRecoveryActions,
    bool UserInputRequired,
    DateTimeOffset Timestamp);

public sealed record DevelopmentWorkspaceResolution(
    bool CanRun,
    string? Workspace,
    string Reason,
    string Message,
    DeveloperBlockerState? Blocker,
    DevelopmentContextSnapshot Context);

public interface IDevelopmentContextProvider
{
    DevelopmentContextSnapshot Current();
    void RememberBlocker(DeveloperBlockerState blocker);
}

public sealed class StaticDevelopmentContextProvider(Func<DevelopmentContextSnapshot> current) : IDevelopmentContextProvider
{
    private DeveloperBlockerState? blocker;

    public DevelopmentContextSnapshot Current()
    {
        var snapshot = current();
        return snapshot with { PreviousBlocker = blocker ?? snapshot.PreviousBlocker };
    }

    public void RememberBlocker(DeveloperBlockerState blocker) => this.blocker = blocker;
}

public sealed class DevelopmentContextResolver(WorkspaceAuthorization authorization)
{
    public DevelopmentWorkspaceResolution Resolve(DevelopmentContextSnapshot context, string requestedOperation, string objective)
    {
        var authorized = new List<DevelopmentWorkspaceCandidate>();
        foreach (var candidate in context.CandidateWorkspaces)
        {
            var result = authorization.Authorize(candidate.Path);
            if (result.Allowed && !string.IsNullOrWhiteSpace(result.CanonicalWorkspace))
                authorized.Add(candidate with { Path = result.CanonicalWorkspace });
        }

        if (!string.IsNullOrWhiteSpace(context.ActiveWorkspace))
        {
            var active = authorization.Authorize(context.ActiveWorkspace);
            if (active.Allowed && !string.IsNullOrWhiteSpace(active.CanonicalWorkspace) &&
                authorized.All(c => !string.Equals(c.Path, active.CanonicalWorkspace, StringComparison.OrdinalIgnoreCase)))
            {
                authorized.Add(new(active.CanonicalWorkspace, "active_context"));
            }
        }

        if (authorized.Count == 1)
            return new(true, authorized[0].Path, "workspace_resolved", $"Using approved workspace from {authorized[0].Source}.", null, context with { ActiveWorkspace = authorized[0].Path, WorkspaceState = "authorized" });

        if (authorized.Count > 1)
        {
            var choices = authorized.Select(c => $"{c.Path} ({c.Source})").ToArray();
            var blocker = Blocker(
                "workspace_disambiguation_required",
                requestedOperation,
                "single approved workspace choice",
                context,
                "multiple_authorized_candidates",
                true);
            return new(false, null, blocker.FailureCategory, $"Choose the approved workspace for this Developer task: {string.Join(" | ", choices)}.", blocker, context with { WorkspaceState = "multiple_authorized_candidates" });
        }

        var missing = Blocker(
            "approved_workspace_required",
            requestedOperation,
            "approved Git workspace",
            context,
            "none_authorized",
            true);
        return new(false, null, missing.FailureCategory, MissingWorkspaceMessage(context, objective), missing, context with { WorkspaceState = "none_authorized" });
    }

    private static DeveloperBlockerState Blocker(string category, string operation, string prerequisite, DevelopmentContextSnapshot context, string workspaceState, bool userInputRequired) =>
        new(
            category,
            operation,
            prerequisite,
            context.ActiveProjectName ?? context.ActiveProjectId,
            workspaceState,
            ["provide_approved_workspace", "open_developer_workspace", "inspect_development_context"],
            userInputRequired,
            DateTimeOffset.UtcNow);

    private static string MissingWorkspaceMessage(DevelopmentContextSnapshot context, string objective)
    {
        var project = context.ActiveProjectName ?? context.ActiveProjectId ?? "the active project";
        return $"Developer task cannot run yet: {project} has no approved Git workspace associated with it. Missing prerequisite: approved_workspace. Open Developer and provide the approved workspace for: {objective}";
    }
}

public sealed class DevelopmentContextGovernedTool(IDevelopmentContextProvider provider) : IGovernedTool
{
    public const string Name = "get_development_context";
    public GovernedToolDefinition Definition { get; } = new(
        Name,
        "Read current development context, active project, workspace state, previous Developer blocker, and governed recovery actions.",
        []);

    public Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var context = provider.Current();
        return Task.FromResult(new GovernedToolResult(
            request.ConversationId,
            request.CorrelationId,
            request.Name,
            GovernedToolStatus.Succeeded,
            $"Development context returned. Workspace state: {context.WorkspaceState}.",
            context));
    }
}
