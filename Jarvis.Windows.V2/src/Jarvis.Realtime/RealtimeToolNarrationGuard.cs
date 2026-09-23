namespace Jarvis.Realtime;

public static class RealtimeToolNarrationGuard
{
    private static readonly string[] ActionVerbs =
    [
        "open ",
        "launch ",
        "start ",
        "check ",
        "checking ",
        "verify ",
        "status"
    ];

    private static readonly string[] ComputerTargets =
    [
        "word",
        "microsoft word",
        "excel",
        "calculator",
        "notepad",
        "file explorer",
        "settings",
        "computer",
        "system"
    ];

    private static readonly string[] NarrationPhrases =
    [
        "i'm going to",
        "i am going to",
        "i'll",
        "i will",
        "still launching",
        "launch is still in progress",
        "checking the status",
        "let me check",
        "should open",
        "may be open"
    ];

    public static bool LooksLikeNarratedToolUse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = text.ToLowerInvariant();
        return NarrationPhrases.Any(normalized.Contains) &&
               ActionVerbs.Any(normalized.Contains) &&
               ComputerTargets.Any(normalized.Contains);
    }
}
