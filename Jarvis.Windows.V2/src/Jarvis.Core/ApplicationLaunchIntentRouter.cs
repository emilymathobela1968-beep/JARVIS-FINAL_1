using System.Text.RegularExpressions;
using Jarvis.ApplicationCapabilities;
namespace Jarvis.Core;

/// <summary>Only a complete, single imperative/request can create a typed launch request.</summary>
public sealed class ApplicationLaunchIntentRouter
{
    private static readonly Regex Syntax = new(@"^(?:jarvis\s*,?\s+)?(?:(?:can|could|would)\s+you\s+)?(?:please\s+)?(?:open(?:\s+up)?|launch|start)\s+(?<application>.+?)(?:\s*,?\s+please)?[.!?]?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    public bool TryRoute(string? text, out LaunchApplicationRequest? request) => TryRoute(text, out request, out _);
    public bool TryRoute(string? text, out LaunchApplicationRequest? request, out string reason)
    {
        request = null;
        if (string.IsNullOrWhiteSpace(text)) { reason = "empty_transcript"; return false; }
        if (text.Length > 256) { reason = "transcript_too_long"; return false; }
        var match = Syntax.Match(text.Trim());
        if (!match.Success) { reason = "unsupported_command_syntax"; return false; }
        var alias = Regex.Replace(match.Groups["application"].Value.Trim(), @"\s+", " ");
        // Product-name variation only; never search for a command inside unrelated speech.
        if (alias.Equals("office microsoft word", StringComparison.OrdinalIgnoreCase)) alias = "microsoft word";
        var app = ApplicationRegistry.Entries.FirstOrDefault(x => x.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase));
        if (app is null) { reason = "unregistered_or_compound_application"; return false; }
        request = new(app.Id); reason = "recognized"; return true;
    }
}
