using System.Text.Json;

namespace Jarvis.Media;

public sealed class JsonMediaAudit(string path)
{
    private readonly object sync = new();

    public void Write(Guid missionId, string stage, object? data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var line = JsonSerializer.Serialize(new { timestampUtc = DateTimeOffset.UtcNow, missionId, stage, data });
        lock (sync) File.AppendAllText(path, line + Environment.NewLine);
    }
}
