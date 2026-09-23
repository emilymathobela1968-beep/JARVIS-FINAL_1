namespace Jarvis.Media;

public sealed class MediaStudioOrchestrator(
    LocalRenderToolDiscovery discovery,
    MediaInputValidator validator,
    MediaProductionPlanner planner,
    FfmpegCommandBuilder commandBuilder,
    IRendererProcess renderer,
    IVideoProbe probe,
    MediaQaValidator qa,
    JsonMediaAudit audit)
{
    public async Task<MediaRenderEvidence> RenderAsync(MediaStudioRenderRequest request, CancellationToken cancellationToken = default)
    {
        audit.Write(request.MissionId, "render_requested", new { request.OutputPath, request.TargetDuration, panelCount = request.Panels.Count });
        try
        {
            validator.Validate(request);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            audit.Write(request.MissionId, "input_rejected", new { reason = ex.Message });
            return new(request.MissionId, MediaRenderStatus.Rejected, request.OutputPath, null, null, null, null, ex.Message);
        }

        var tools = discovery.Find();
        if (tools is null)
        {
            const string reason = "ffmpeg_or_ffprobe_missing";
            audit.Write(request.MissionId, "renderer_missing", new { reason });
            return new(request.MissionId, MediaRenderStatus.MissingRenderer, request.OutputPath, null, null, null, null, reason);
        }

        var plan = planner.CreatePlan(new(request.MissionId, request.Authority, request.Panels, request.TargetDuration, MediaOutputFormat.Youtube16x9Master));
        var command = commandBuilder.Build(tools, request, plan);
        audit.Write(request.MissionId, "renderer_command_created", new { command.FileName, command.Arguments, command.WorkingDirectory });
        var execution = await renderer.RunAsync(command, TimeSpan.FromMinutes(30), cancellationToken).ConfigureAwait(false);
        audit.Write(request.MissionId, "renderer_finished", execution);
        if (execution.ExitCode != 0 || execution.TimedOut)
        {
            return new(request.MissionId, MediaRenderStatus.Failed, request.OutputPath, command, execution, null, null, execution.TimedOut ? "renderer_timeout" : "renderer_failed");
        }

        var probed = await probe.ProbeAsync(request.OutputPath, tools, cancellationToken).ConfigureAwait(false);
        var frames = await ExtractFramesAsync(request, plan).ConfigureAwait(false);
        var qaResult = qa.Validate(request, plan, execution, probed, frames);
        audit.Write(request.MissionId, "qa_finished", qaResult);
        return new(request.MissionId, qaResult.Accepted ? MediaRenderStatus.Succeeded : MediaRenderStatus.Rejected, request.OutputPath, command, execution, probed, qaResult, qaResult.Accepted ? null : "qa_rejected");
    }

    private static Task<IReadOnlyList<string>> ExtractFramesAsync(MediaStudioRenderRequest request, MediaProductionPlan plan)
    {
        var evidenceRoot = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(request.OutputPath))!, ".media-qa", request.MissionId.ToString("N"));
        Directory.CreateDirectory(evidenceRoot);
        IReadOnlyList<string> frames = plan.Timeline
            .Select(shot => Path.Combine(evidenceRoot, $"scene-{shot.PanelIndex:00}-sample.png"))
            .ToArray();
        return Task.FromResult(frames);
    }
}
