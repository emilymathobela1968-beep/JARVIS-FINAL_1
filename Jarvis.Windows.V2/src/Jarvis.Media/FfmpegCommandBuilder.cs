using System.Globalization;

namespace Jarvis.Media;

public sealed class FfmpegCommandBuilder
{
    private const int Width = 1920;
    private const int Height = 1080;
    private const int FrameRate = 30;

    public RendererCommand Build(RendererTools tools, MediaStudioRenderRequest request, MediaProductionPlan plan)
    {
        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(request.OutputPath))!;
        Directory.CreateDirectory(outputDirectory);
        var args = new List<string> { "-y" };

        foreach (var shot in plan.Timeline.OrderBy(shot => shot.Start))
        {
            var panel = request.Panels.Single(item => item.Index == shot.PanelIndex);
            args.AddRange(["-loop", "1", "-t", Seconds(shot.Duration), "-i", Path.GetFullPath(panel.Path)]);
        }

        var inputCount = plan.Timeline.Count;
        var narrationIndex = -1;
        var musicIndex = -1;
        if (!string.IsNullOrWhiteSpace(request.NarrationAudioPath))
        {
            narrationIndex = inputCount++;
            args.AddRange(["-i", Path.GetFullPath(request.NarrationAudioPath)]);
        }

        if (!string.IsNullOrWhiteSpace(request.MusicAudioPath))
        {
            musicIndex = inputCount++;
            args.AddRange(["-stream_loop", "-1", "-i", Path.GetFullPath(request.MusicAudioPath)]);
        }

        args.AddRange(["-filter_complex", BuildFilter(request, plan, narrationIndex, musicIndex)]);
        args.AddRange(["-map", "[vout]"]);
        if (narrationIndex >= 0 || musicIndex >= 0)
        {
            args.AddRange(["-map", "[aout]"]);
            args.AddRange(["-c:a", "aac", "-b:a", "192k"]);
        }

        args.AddRange(["-c:v", "libx264", "-pix_fmt", "yuv420p", "-r", FrameRate.ToString(CultureInfo.InvariantCulture), "-movflags", "+faststart", "-t", Seconds(plan.TotalDuration), Path.GetFullPath(request.OutputPath)]);
        return new(tools.FfmpegPath, args, outputDirectory, tools.FfmpegPath + " " + string.Join(" ", args.Select(Quote)));
    }

    private static string BuildFilter(MediaStudioRenderRequest request, MediaProductionPlan plan, int narrationIndex, int musicIndex)
    {
        var parts = new List<string>();
        var ordered = plan.Timeline.OrderBy(shot => shot.Start).ToArray();
        for (var i = 0; i < ordered.Length; i++)
        {
            var shot = ordered[i];
            var zoom = shot.Motion.Contains("push", StringComparison.OrdinalIgnoreCase) ? "min(zoom+0.0006,1.08)" : "1.035";
            parts.Add($"[{i}:v]scale={Width}:{Height}:force_original_aspect_ratio=increase,crop={Width}:{Height},setsar=1,zoompan=z='{zoom}':d={Math.Max(1, (int)Math.Round(shot.Duration.TotalSeconds * FrameRate))}:s={Width}x{Height}:fps={FrameRate},drawtext=text='{EscapeText(shot.Title)}':x=80:y=910:fontsize=42:fontcolor=white:box=1:boxcolor=black@0.42[v{i}]");
        }

        var current = "v0";
        var transition = 0.75;
        for (var i = 1; i < ordered.Length; i++)
        {
            var previousDuration = ordered.Take(i).Sum(shot => shot.Duration.TotalSeconds);
            var offset = Math.Max(0.1, previousDuration - transition);
            var next = i == ordered.Length - 1 ? "vout" : $"vx{i}";
            parts.Add($"[{current}][v{i}]xfade=transition=fade:duration={transition.ToString(CultureInfo.InvariantCulture)}:offset={offset.ToString("0.###", CultureInfo.InvariantCulture)}[{next}]");
            current = next;
        }

        if (narrationIndex >= 0 && musicIndex >= 0)
        {
            parts.Add($"[{musicIndex}:a]volume=0.18[music];[{narrationIndex}:a]volume=1.0[narration];[music][narration]amix=inputs=2:duration=first:dropout_transition=2[aout]");
        }
        else if (narrationIndex >= 0)
        {
            parts.Add($"[{narrationIndex}:a]volume=1.0[aout]");
        }
        else if (musicIndex >= 0)
        {
            parts.Add($"[{musicIndex}:a]volume=0.25[aout]");
        }

        return string.Join(";", parts);
    }

    private static string Seconds(TimeSpan value) => value.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);

    private static string EscapeText(string text) => text
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace(":", "\\:", StringComparison.Ordinal)
        .Replace("'", "\\'", StringComparison.Ordinal);

    private static string Quote(string arg) => arg.Contains(' ') ? $"\"{arg}\"" : arg;
}
