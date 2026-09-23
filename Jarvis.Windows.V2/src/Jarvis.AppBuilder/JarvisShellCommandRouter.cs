namespace Jarvis.AppBuilder;

public enum JarvisShellCommandIntent
{
    Conversation,
    Builder,
    Developer,
    Media,
    Computer,
    Barehands,
    System
}

public sealed record JarvisShellCommandRoute(
    JarvisShellCommandIntent Intent,
    JarvisWorkspace Workspace,
    string Reason);

public static class JarvisShellCommandRouter
{
    public static JarvisShellCommandRoute Route(JarvisUiContext context, string prompt)
    {
        var normalized = (prompt ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return new(JarvisShellCommandIntent.Conversation, context.ActiveWorkspace, "empty");

        if (context.ActiveWorkspace == JarvisWorkspace.BUILDER && LooksLikeBuilderInstruction(normalized))
            return new(JarvisShellCommandIntent.Builder, JarvisWorkspace.BUILDER, "active_builder_workspace");

        if (ContainsAny(normalized, "build ", "build me", "builder", "application brief", "two-page application", "workshop app", "screen", "panel", "canvas", "preview", "implement this"))
            return new(JarvisShellCommandIntent.Builder, JarvisWorkspace.BUILDER, "builder_objective");

        if (ContainsAny(normalized, "open the code", "developer", "codex", "source", "run tests", "build the project", "fix the code"))
            return new(JarvisShellCommandIntent.Developer, JarvisWorkspace.DEVELOPER, "developer_objective");

        if (ContainsAny(normalized, "generate an image", "image", "media", "render a picture", "promo scene", "visual asset"))
            return new(JarvisShellCommandIntent.Media, JarvisWorkspace.MEDIA, "media_objective");

        if (ContainsAny(normalized, "open calculator", "open notepad", "open file explorer", "computer", "window", "desktop", "laptop"))
            return new(JarvisShellCommandIntent.Computer, JarvisWorkspace.COMPUTER, "computer_operation");

        if (ContainsAny(normalized, "barehands", "bare hands", "camera board", "3d board"))
            return new(JarvisShellCommandIntent.Barehands, JarvisWorkspace.BAREHANDS, "barehands_workspace");

        if (ContainsAny(normalized, "diagnostics", "system status", "realtime status", "latest transcript", "turn gate"))
            return new(JarvisShellCommandIntent.System, JarvisWorkspace.SYSTEM, "diagnostics_request");

        return new(JarvisShellCommandIntent.Conversation, context.ActiveWorkspace, "conversation");
    }

    private static bool LooksLikeBuilderInstruction(string normalized) =>
        ContainsAny(normalized, "make this", "make the", "narrower", "wider", "revise", "render", "show preview", "implement this", "candidate", "selection", "component");

    private static bool ContainsAny(string value, params string[] needles) =>
        needles.Any(needle => value.Contains(needle, StringComparison.Ordinal));
}
