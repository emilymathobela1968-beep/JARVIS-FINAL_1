using System.Text.Json;
using System.Text.RegularExpressions;

namespace Jarvis.Realtime;

public sealed class JsonRealtimeAudit(string path) : IRealtimeAuditSink
{
    private static readonly Regex SecretPattern = new("(?i)(api[_-]?key|token|secret|password|fish_audio_api_key|fish_audio_reference_id|azure_openai_api_key)\\s*[=:]\\s*[^\\s;\"}]+", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    private readonly object sync = new();

    public void Write(Guid sessionId, Guid correlationId, string name, object? data = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var payload = Redact(JsonSerializer.Serialize(new
        {
            timestamp = DateTimeOffset.UtcNow,
            sessionId,
            correlationId,
            name,
            data
        }));

        lock (sync)
        {
            File.AppendAllText(path, payload + Environment.NewLine);
        }
    }

    public static string Redact(string value) =>
        string.IsNullOrEmpty(value) ? string.Empty : SecretPattern.Replace(value, m => m.Value.Split('=')[0].Split(':')[0] + "=<REDACTED>");
}
