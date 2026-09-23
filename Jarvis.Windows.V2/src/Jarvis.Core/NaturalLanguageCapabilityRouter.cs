using System.Text.RegularExpressions;
using Jarvis.Contracts;

namespace Jarvis.Core;

/// <summary>
/// Converts a deliberately small set of user phrases into explicit capability requests.
/// Requests that do not match are left for the existing voice/model path.
/// </summary>
public sealed class NaturalLanguageCapabilityRouter
{
    private const string JarvisRoot = @"C:\JARVIS_CODEX";

    public bool TryRoute(string? text, out CapabilityRequest? request)
    {
        request = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim().TrimEnd('.', '!', '?').Trim();
        var phrase = normalized.ToLowerInvariant();

        if (phrase is "jarvis, open file explorer" or "open file explorer" or "open explorer")
        {
            request = new CapabilityRequest(CapabilityNames.OpenFileExplorer);
            return true;
        }
        if (phrase is "open calculator" or "jarvis, open calculator")
        {
            request = new CapabilityRequest(CapabilityNames.OpenCalculator);
            return true;
        }
        if (phrase is "open notepad" or "jarvis, open notepad")
        {
            request = new CapabilityRequest(CapabilityNames.OpenNotepad);
            return true;
        }
        if (phrase is "open the jarvis folder" or "open jarvis folder")
        {
            request = WithPath(CapabilityNames.OpenFolder, JarvisRoot);
            return true;
        }
        if (phrase is "what folder are you working in" or "what folder are you working in?" or "current directory")
        {
            request = new CapabilityRequest(CapabilityNames.GetCurrentDirectory);
            return true;
        }
        if (phrase is "what processes are running" or "list running processes" or "show running processes")
        {
            request = new CapabilityRequest(CapabilityNames.ListRunningProcesses);
            return true;
        }

        var url = Regex.Match(normalized, @"^open\s+(?<url>https?://\S+)$", RegexOptions.IgnoreCase);
        if (url.Success)
        {
            request = new CapabilityRequest(CapabilityNames.OpenUrl,
                new Dictionary<string, string> { ["url"] = url.Groups["url"].Value });
            return true;
        }

        var openFolder = Regex.Match(normalized, @"^open\s+(?:folder\s+)?(?<path>[A-Za-z]:\\.+)$", RegexOptions.IgnoreCase);
        if (openFolder.Success)
        {
            request = WithPath(CapabilityNames.OpenFolder, openFolder.Groups["path"].Value);
            return true;
        }

        var listDirectory = Regex.Match(normalized, @"^(?:show me\s+)?(?:the\s+)?files\s+in\s+(?<path>[A-Za-z]:\\.+)$", RegexOptions.IgnoreCase);
        if (listDirectory.Success)
        {
            request = WithPath(CapabilityNames.ListDirectory, listDirectory.Groups["path"].Value);
            return true;
        }

        return false;
    }

    private static CapabilityRequest WithPath(string capability, string path) =>
        new(capability, new Dictionary<string, string> { ["path"] = path });
}
