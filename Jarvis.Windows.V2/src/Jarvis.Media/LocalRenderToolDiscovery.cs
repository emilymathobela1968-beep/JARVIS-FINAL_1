namespace Jarvis.Media;

public sealed class LocalRenderToolDiscovery
{
    public RendererTools? Find()
    {
        var ffmpeg = FindOnPath("ffmpeg.exe") ?? FindOnPath("ffmpeg");
        var ffprobe = FindOnPath("ffprobe.exe") ?? FindOnPath("ffprobe");
        return ffmpeg is null || ffprobe is null ? null : new(ffmpeg, ffprobe);
    }

    private static string? FindOnPath(string executable)
    {
        var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var path in paths)
        {
            var candidate = Path.Combine(path, executable);
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }
}
