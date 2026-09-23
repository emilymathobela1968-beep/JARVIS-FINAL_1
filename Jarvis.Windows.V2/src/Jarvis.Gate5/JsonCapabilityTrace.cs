using System.Text.Json;
using System.Text.Json.Serialization;
namespace Jarvis.Gate5;
public sealed class JsonCapabilityTrace(string path)
{
    private readonly object sync = new();
    public void Write(string stage, object? data)
    {
        lock (sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.AppendAllText(path, JsonSerializer.Serialize(new { timestampUtc = DateTimeOffset.UtcNow, stage, data }, new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } }) + Environment.NewLine);
        }
    }
}
