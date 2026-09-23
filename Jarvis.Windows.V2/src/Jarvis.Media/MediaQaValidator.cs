namespace Jarvis.Media;

public sealed class MediaQaValidator
{
    public MediaQaResult Validate(MediaStudioRenderRequest request, MediaProductionPlan plan, RendererExecutionResult execution, VideoProbeResult probe, IReadOnlyList<string> frameEvidence)
    {
        var passed = new List<string>();
        var failed = new List<string>();

        Check(execution.ExitCode == 0, "renderer_exit_code", passed, failed);
        Check(execution.OutputFileSize > 0, "output_file_nonempty", passed, failed);
        Check(probe.Width == 1920 && probe.Height == 1080, "resolution_1920x1080", passed, failed);
        Check(Math.Abs((probe.Width / (double)probe.Height) - (16 / 9.0)) < 0.01, "aspect_ratio_16x9", passed, failed);
        Check(Math.Abs((probe.Duration - request.TargetDuration).TotalSeconds) <= 1.0, "duration_within_tolerance", passed, failed);
        Check(!request.NarrationAudioPath.HasValueOrWhiteSpace() && !request.MusicAudioPath.HasValueOrWhiteSpace() || probe.HasAudio, "expected_audio_present", passed, failed);
        Check(plan.Timeline.Count == 16 && plan.Timeline.Select(shot => shot.PanelIndex).SequenceEqual(Enumerable.Range(1, 16)), "all_16_scenes_ordered", passed, failed);
        Check(frameEvidence.Count >= Math.Min(16, plan.Timeline.Count), "representative_frames_preserved", passed, failed);

        return new(failed.Count == 0, passed, failed, frameEvidence);
    }

    private static void Check(bool condition, string name, ICollection<string> passed, ICollection<string> failed)
    {
        if (condition) passed.Add(name);
        else failed.Add(name);
    }
}

file static class StringExtensions
{
    public static bool HasValueOrWhiteSpace(this string? value) => !string.IsNullOrWhiteSpace(value);
}
