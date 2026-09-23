namespace Jarvis.Media;

public sealed class MediaProductionPlanner
{
    public MediaProductionPlan CreatePlan(MediaProductionRequest request)
    {
        request.Authority.Validate();
        if (request.Panels.Count == 0) throw new InvalidOperationException("At least one source panel is required.");
        if (request.Panels.Select(panel => panel.Index).Distinct().Count() != request.Panels.Count)
            throw new InvalidOperationException("Panel indexes must be unique.");
        if (request.TargetDuration <= TimeSpan.Zero) throw new InvalidOperationException("Target duration must be positive.");

        var ordered = request.Panels.OrderBy(panel => panel.Index).ToArray();
        var perPanelTicks = request.TargetDuration.Ticks / ordered.Length;
        var timeline = new List<MediaTimelineShot>();
        var cursor = TimeSpan.Zero;
        for (var i = 0; i < ordered.Length; i++)
        {
            var duration = i == ordered.Length - 1
                ? request.TargetDuration - cursor
                : TimeSpan.FromTicks(perPanelTicks);
            var panel = ordered[i];
            timeline.Add(new(
                panel.Index,
                cursor,
                duration,
                i % 2 == 0 ? "restrained push-in" : "slow lateral drift",
                panel.Title,
                $"Narrate the ALEXIS value shown in {panel.Title} with restrained, professional language."));
            cursor += duration;
        }

        return new(
            request.ProductionId,
            timeline,
            [
                "all_source_panels_present",
                "sixteen_by_nine_master",
                "duration_matches_target",
                "text_not_cropped",
                "brand_consistency",
                "audio_track_present",
                "sampled_frames_reviewed"
            ],
            cursor,
            request.OutputFormat);
    }
}
