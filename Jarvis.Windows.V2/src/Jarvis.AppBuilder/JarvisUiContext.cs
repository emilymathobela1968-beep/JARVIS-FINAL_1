using Jarvis.Governance;

namespace Jarvis.AppBuilder;

public enum JarvisWorkspace { HOME, COMPUTER, DEVELOPER, MEDIA, BUILDER, BAREHANDS, SYSTEM }

public sealed record JarvisUiContext(
    JarvisWorkspace ActiveWorkspace,
    string? ActiveProjectId,
    string? ActiveProjectName,
    string? ActiveScreenId,
    string? ActiveScreenName,
    string? ActiveRevisionId,
    string? ActiveArtifact,
    string? ActiveCandidateState,
    IReadOnlyList<string> ActiveSemanticComponentIds,
    string? ActiveSelection,
    DateTimeOffset UpdatedAt);

public sealed class JarvisUiContextProvider
{
    private readonly object gate = new();
    private JarvisUiContext current = new(JarvisWorkspace.HOME, null, null, null, null, null, null, null, [], null, DateTimeOffset.UtcNow);

    public JarvisUiContext Current { get { lock (gate) return current; } }
    public void Set(JarvisUiContext context) { lock (gate) current = context with { UpdatedAt = DateTimeOffset.UtcNow }; }
}

public sealed class GetJarvisUiContextTool(Func<JarvisUiContext> current) : IGovernedTool
{
    public const string Name = "get_jarvis_ui_context";
    public GovernedToolDefinition Definition { get; } = new(Name, "Read the current Jarvis V2 workspace and active Builder screen state.", []);

    public Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var context = current();
        return Task.FromResult(new GovernedToolResult(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Succeeded,
            $"Current Jarvis workspace is {context.ActiveWorkspace}. Screen: {context.ActiveScreenName ?? "none"}. Revision: {context.ActiveRevisionId ?? "none"}.", context));
    }
}

public sealed class NavigateJarvisWorkspaceTool(Func<JarvisWorkspace, Task> navigate) : IGovernedTool
{
    public const string Name = "navigate_jarvis_workspace";
    public GovernedToolDefinition Definition { get; } = new(Name, "Navigate Jarvis V2 top-level workspaces only: HOME, COMPUTER, DEVELOPER, MEDIA, BUILDER, BAREHANDS, SYSTEM.", ["workspace"]);

    public async Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var value = request.Arguments.GetProperty("workspace").GetString() ?? string.Empty;
        if (!TryResolve(value, out var workspace))
            return new(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Rejected, "Unknown Jarvis workspace. Choose HOME, COMPUTER, DEVELOPER, MEDIA, BUILDER, BAREHANDS, or SYSTEM.", null, "workspace_not_allowed");
        await navigate(workspace).ConfigureAwait(false);
        return new(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Succeeded, $"Jarvis workspace changed to {workspace}.", new { workspace = workspace.ToString() });
    }

    public static bool TryResolve(string value, out JarvisWorkspace workspace)
    {
        var normalized = value.Trim().ToLowerInvariant();
        normalized = normalized.StartsWith("go to ", StringComparison.Ordinal) ? normalized[6..] : normalized;
        normalized = normalized.StartsWith("go ", StringComparison.Ordinal) ? normalized[3..] : normalized;
        normalized = normalized.StartsWith("switch to ", StringComparison.Ordinal) ? normalized[10..] : normalized;
        normalized = normalized.StartsWith("open ", StringComparison.Ordinal) ? normalized[5..] : normalized;
        workspace = normalized switch
        {
            "home" => JarvisWorkspace.HOME,
            "computer" => JarvisWorkspace.COMPUTER,
            "developer" or "dev" or "development" or "code" or "coding" => JarvisWorkspace.DEVELOPER,
            "media" => JarvisWorkspace.MEDIA,
            "builder" or "build" or "app builder" or "design" or "design mode" => JarvisWorkspace.BUILDER,
            "barehands" or "bare hands" => JarvisWorkspace.BAREHANDS,
            "system" => JarvisWorkspace.SYSTEM,
            "bold mode" or "bolder" => JarvisWorkspace.BUILDER,
            _ => default
        };
        return normalized is "home" or "computer" or "developer" or "dev" or "development" or "code" or "coding" or "media" or "builder" or "build" or "app builder" or "design" or "design mode" or "barehands" or "bare hands" or "system" or "bold mode" or "bolder";
    }
}
