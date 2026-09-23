using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Jarvis.Media;

public interface IVideoProbe
{
    Task<VideoProbeResult> ProbeAsync(string path, RendererTools tools, CancellationToken cancellationToken);
}

public sealed class FfprobeVideoProbe : IVideoProbe
{
    public async Task<VideoProbeResult> ProbeAsync(string path, RendererTools tools, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo(tools.FfprobePath)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var arg in new[] { "-v", "error", "-show_streams", "-show_format", "-of", "json", path })
            start.ArgumentList.Add(arg);

        using var process = Process.Start(start) ?? throw new InvalidOperationException("ffprobe could not be started.");
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (process.ExitCode != 0) throw new InvalidOperationException("ffprobe failed.");

        using var document = JsonDocument.Parse(stdout);
        var streams = document.RootElement.GetProperty("streams").EnumerateArray().ToArray();
        var video = streams.First(stream => stream.GetProperty("codec_type").GetString() == "video");
        var hasAudio = streams.Any(stream => stream.GetProperty("codec_type").GetString() == "audio");
        var durationText = document.RootElement.GetProperty("format").GetProperty("duration").GetString() ?? "0";
        var frameRate = ParseRate(video.TryGetProperty("avg_frame_rate", out var rate) ? rate.GetString() : null);
        return new(
            path,
            TimeSpan.FromSeconds(double.Parse(durationText, CultureInfo.InvariantCulture)),
            video.GetProperty("width").GetInt32(),
            video.GetProperty("height").GetInt32(),
            frameRate,
            hasAudio);
    }

    private static double ParseRate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var parts = value.Split('/');
        if (parts.Length == 2 && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var numerator) && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var denominator) && denominator != 0)
            return numerator / denominator;
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
    }
}
