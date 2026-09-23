using System.Text.Json;
using System.Text.RegularExpressions;

namespace Jarvis.Governance;

public enum GovernedToolStatus { Succeeded, Rejected, Failed, TimedOut, Cancelled, ProviderUnavailable, AuthorizationRequired, ReferenceRequired, VerificationFailed }

public sealed record GovernedToolDefinition(string Name, string Description, IReadOnlyList<string> RequiredArguments);

public sealed record GovernedToolRequest(
    Guid ConversationId,
    Guid CorrelationId,
    string Name,
    JsonElement Arguments);

public sealed record GovernedToolResult(
    Guid ConversationId,
    Guid CorrelationId,
    string Name,
    GovernedToolStatus Status,
    string Message,
    object? Evidence = null,
    string? FailureReason = null);

public interface IGovernedTool
{
    GovernedToolDefinition Definition { get; }
    Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default);
}

public interface IGovernedToolAudit
{
    void Write(Guid conversationId, Guid correlationId, string name, object? data = null);
}

public sealed class GovernedCapabilityRegistry(IEnumerable<IGovernedTool> tools, IGovernedToolAudit audit)
{
    private readonly Dictionary<string, IGovernedTool> tools = tools.ToDictionary(x => x.Definition.Name, StringComparer.Ordinal);

    public IReadOnlyList<GovernedToolDefinition> Definitions => tools.Values.Select(x => x.Definition).OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();

    public async Task<GovernedToolResult> DispatchAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        audit.Write(request.ConversationId, request.CorrelationId, "tool_request_received", new { request.Name });
        if (!tools.TryGetValue(request.Name, out var tool))
        {
            var rejected = new GovernedToolResult(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Rejected, $"Unknown tool: {request.Name}", null, "unknown_tool");
            audit.Write(request.ConversationId, request.CorrelationId, "tool_rejected", new { rejected.FailureReason });
            return rejected;
        }

        var missing = tool.Definition.RequiredArguments
            .Where(arg => request.Arguments.ValueKind != JsonValueKind.Object || !request.Arguments.TryGetProperty(arg, out _))
            .ToArray();
        if (missing.Length > 0)
        {
            var rejected = new GovernedToolResult(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Rejected, $"Malformed tool arguments: missing {string.Join(", ", missing)}.", null, "malformed_arguments");
            audit.Write(request.ConversationId, request.CorrelationId, "tool_rejected", new { rejected.FailureReason, missing });
            return rejected;
        }

        if (GovernanceText.ContainsAuthorizationExpansion(request.Arguments.GetRawText()))
        {
            var rejected = new GovernedToolResult(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Rejected, "Tool request rejected: authorization expansion is not allowed.", null, "authorization_expansion_requested");
            audit.Write(request.ConversationId, request.CorrelationId, "tool_rejected", new { rejected.FailureReason });
            return rejected;
        }

        try
        {
            var result = await tool.InvokeAsync(request, cancellationToken).ConfigureAwait(false);
            audit.Write(request.ConversationId, request.CorrelationId, "tool_result", new { result.Status, result.FailureReason });
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var cancelled = new GovernedToolResult(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Cancelled, "Tool request cancelled.", null, "cancelled");
            audit.Write(request.ConversationId, request.CorrelationId, "tool_cancelled");
            return cancelled;
        }
    }
}

public sealed class JsonGovernedToolAudit(string path) : IGovernedToolAudit
{
    private readonly object sync = new();

    public void Write(Guid conversationId, Guid correlationId, string name, object? data = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var line = GovernanceText.RedactSecrets(JsonSerializer.Serialize(new
        {
            timestamp = DateTimeOffset.UtcNow,
            conversationId,
            correlationId,
            name,
            data
        }));
        lock (sync)
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }
}

public static class GovernanceText
{
    private static readonly Regex SecretPattern = new("(?i)(api[_-]?key|token|secret|password|fish_audio_api_key|fish_audio_reference_id|azure_openai_api_key|openai_api_key)\\s*[=:]\\s*[^\\s;\"}]+", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    private static readonly string[] ExpansionMarkers = ["credential", "api key", "api_key", "token", "secret", "password", "administrator", "elevated", "deploy", "publish", "outside the workspace", "setx", "bypass"];

    public static string RedactSecrets(string text) =>
        string.IsNullOrEmpty(text) ? string.Empty : SecretPattern.Replace(text, m => m.Value.Split('=')[0].Split(':')[0] + "=<REDACTED>");

    public static bool ContainsAuthorizationExpansion(string text) =>
        ExpansionMarkers.Any(marker => text.Contains(marker, StringComparison.OrdinalIgnoreCase));
}
