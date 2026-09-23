using System.Text.Json;
namespace Jarvis.ApplicationCapabilities;
public sealed class JsonLaunchAudit(string path) : ILaunchAudit
{
    private readonly object sync = new();
    public void Write(string stage, Guid operationId, ApplicationId application, LaunchResult? result = null)
    {
        lock (sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.AppendAllText(path, JsonSerializer.Serialize(new { timestampUtc = DateTimeOffset.UtcNow, stage, operationId, application = application.ToString(), result }, new JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } }) + Environment.NewLine);
        }
    }
}
