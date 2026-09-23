using System.Text.RegularExpressions;

namespace Jarvis.Developer;

public sealed class DeveloperCommandPolicy
{
    private static readonly Regex SecretPattern = new("(?i)(api[_-]?key|token|secret|password|fish_audio_api_key|fish_audio_reference_id)\\s*[=:]\\s*[^\\s;]+", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    private static readonly string[] ExpansionMarkers = ["outside the workspace", "bypass", "administrator", "elevated", "credential", "token", "api key", "setx", "deploy", "publish"];

    public bool ValidateEnvelope(DeveloperTaskRequest request, out string reason)
    {
        if (request.IterationLimit < 1 || request.IterationLimit > 2) { reason = "iteration_limit_out_of_policy"; return false; }
        if (request.Timeout <= TimeSpan.Zero || request.Timeout > TimeSpan.FromMinutes(20)) { reason = "timeout_out_of_policy"; return false; }
        if (!request.PermittedOperations.Contains(DeveloperOperationClass.Inspect)) { reason = "inspect_permission_required"; return false; }
        if (request.ProhibitedOperations.Any(x => x.Contains("credential", StringComparison.OrdinalIgnoreCase) || x.Contains("security", StringComparison.OrdinalIgnoreCase))) { reason = "recognized_governance_prohibitions"; return true; }
        reason = "missing_required_prohibitions"; return false;
    }

    public bool IsAgentAuthorizationExpansion(string agentText, out string reason)
    {
        if (ExpansionMarkers.Any(x => agentText.Contains(x, StringComparison.OrdinalIgnoreCase)))
        {
            reason = "agent_requested_or_implied_authorization_expansion";
            return true;
        }

        reason = "no_expansion_detected";
        return false;
    }

    public string RedactSecrets(string text) =>
        string.IsNullOrEmpty(text) ? string.Empty : SecretPattern.Replace(text, m => m.Value.Split('=')[0].Split(':')[0] + "=<REDACTED>");
}
