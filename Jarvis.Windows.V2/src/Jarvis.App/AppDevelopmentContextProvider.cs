using Jarvis.AppBuilder;
using Jarvis.Realtime;

namespace Jarvis.App;

internal sealed class AppDevelopmentContextProvider(
    Func<JarvisUiContext> uiContext,
    Func<BuilderProjectContext?> builderContext,
    IReadOnlyList<DevelopmentWorkspaceCandidate> candidateWorkspaces) : IDevelopmentContextProvider
{
    private DeveloperBlockerState? blocker;

    public DevelopmentContextSnapshot Current()
    {
        var ui = uiContext();
        var builder = builderContext();
        return new(
            ui.ActiveProjectId ?? builder?.ActiveProjectId,
            ui.ActiveProjectName ?? builder?.ActiveProjectName,
            builder?.Objective,
            candidateWorkspaces.Count == 1 ? candidateWorkspaces[0].Path : null,
            candidateWorkspaces.Count == 0 ? "none_configured" : candidateWorkspaces.Count == 1 ? "single_candidate" : "multiple_candidates",
            ui.ActiveScreenName ?? builder?.ScreenName,
            ui.ActiveRevisionId ?? builder?.RevisionId,
            builder?.CurrentTask,
            blocker,
            candidateWorkspaces,
            ["inspect_development_context", "ask_for_approved_workspace", "open_developer_workspace"]);
    }

    public void RememberBlocker(DeveloperBlockerState blocker) => this.blocker = blocker;
}
