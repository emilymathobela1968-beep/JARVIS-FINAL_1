using System.Text.Json;

namespace Jarvis.Voice;

public sealed class JsonLineVoiceDiagnosticSink : IVoiceDiagnosticSink
{
    private readonly string path;
    private readonly object writeLock = new();

    public JsonLineVoiceDiagnosticSink(string? path = null) =>
        this.path = path ?? Path.Combine(AppContext.BaseDirectory, "logs", "voice-gate1.jsonl");

    public void Write(string eventName, IReadOnlyDictionary<string, object?> fields)
    {
        var record = new Dictionary<string, object?>(fields) { ["timestampUtc"] = DateTimeOffset.UtcNow, ["event"] = eventName };
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        lock (writeLock) File.AppendAllText(path, JsonSerializer.Serialize(record) + Environment.NewLine);
    }
}
