namespace Jarvis.Developer;

public sealed class WorkspaceAuthorization
{
    public WorkspaceAuthorizationResult Authorize(string workspace, bool requireGitRepository = true)
    {
        if (string.IsNullOrWhiteSpace(workspace))
            return new(false, null, "workspace_required");

        string canonical;
        try { canonical = Path.GetFullPath(workspace); }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        { return new(false, null, "workspace_path_invalid"); }

        if (!Directory.Exists(canonical))
            return new(false, canonical, "workspace_missing");

        if (requireGitRepository && !Directory.Exists(Path.Combine(canonical, ".git")))
            return new(false, canonical, "workspace_not_git_repository");

        return new(true, canonical, "workspace_authorized");
    }

    public bool IsInsideWorkspace(string approvedWorkspace, string candidate)
    {
        var root = EnsureTrailingSeparator(Path.GetFullPath(approvedWorkspace));
        var path = Path.GetFullPath(candidate);
        return path.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            || string.Equals(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
}
