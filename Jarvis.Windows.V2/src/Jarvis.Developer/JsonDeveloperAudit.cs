using System.Text.Json;

namespace Jarvis.Developer;

public sealed class JsonDeveloperAudit(string path, DeveloperCommandPolicy policy)
{
    private readonly object sync = new();

    public void Write(Guid taskId, string stage, object? data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var json = JsonSerializer.Serialize(new { timestampUtc = DateTimeOffset.UtcNow, taskId, stage, data });
        json = policy.RedactSecrets(json);
        lock (sync) File.AppendAllText(path, json + Environment.NewLine);
    }
}
