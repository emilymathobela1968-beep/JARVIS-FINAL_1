using Jarvis.Media;
using Jarvis.MissionIntelligence;
using Jarvis.VisualEngineering;

var tests = new MissionIntelligenceTests();
tests.RunAll();

internal sealed class MissionIntelligenceTests
{
    private int passed;
    private int failed;

    public void RunAll()
    {
        Run("ALEXIS authority validates", AlexisAuthorityValidates);
        Run("Visual Engineering requires rendered evidence stages", VisualPlanRequiresRenderedEvidence);
        Run("Visual Engineering produces bounded Developer task", VisualPlanProducesBoundedDeveloperTask);
        Run("Media planner creates sixteen-panel five-minute timeline", MediaPlannerCreatesSixteenPanelTimeline);
        Run("Media planner requires post-render QA checks", MediaPlannerRequiresPostRenderQa);
        Run("Media planner rejects duplicate panel indexes", MediaPlannerRejectsDuplicatePanelIndexes);
        Run("Media input validates sixteen images", MediaInputValidatesSixteenImages);
        Run("Media input rejects missing images", MediaInputRejectsMissingImages);
        Run("Media input rejects output outside approved root", MediaInputRejectsOutputOutsideApprovedRoot);
        Run("Media command construction includes renderer controls", MediaCommandConstructionIncludesRendererControls);
        Run("Media render failure evidence is structured", MediaRenderFailureEvidenceIsStructured);
        Run("Media probe QA accepts valid video evidence", MediaProbeQaAcceptsValidEvidence);
        Run("Media QA rejects bad duration", MediaQaRejectsBadDuration);
        Run("Media audit writes evidence", MediaAuditWritesEvidence);

        Console.WriteLine($"MISSION_INTELLIGENCE_TESTS passed={passed} failed={failed}");
        if (failed > 0) Environment.ExitCode = 1;
    }

    private static void AlexisAuthorityValidates()
    {
        var authority = AlexisDesignAuthority.CreateBaseline();
        authority.Validate();
        Assert(authority.Rules.Any(rule => rule.Id == "visual.system-scan"), "System Scan rule is required.");
        Assert(authority.Rules.Any(rule => rule.Id == "functionality.locked"), "Functionality preservation rule is required.");
    }

    private static void VisualPlanRequiresRenderedEvidence()
    {
        var request = VisualRequest();
        var plan = new VisualEngineeringPlanner().CreatePlan(request);
        Assert(plan.Stages.Contains(VisualEngineeringStage.InspectRenderedUi), "plan must inspect rendered UI.");
        Assert(plan.Stages.Contains(VisualEngineeringStage.CaptureRenderedResult), "plan must capture rendered result.");
        Assert(plan.Stages.Contains(VisualEngineeringStage.ReviewRenderedResult), "plan must review rendered result.");
        Assert(plan.RequiredEvidence.Contains("preview_render_capture"), "plan must require preview capture evidence.");
    }

    private static void VisualPlanProducesBoundedDeveloperTask()
    {
        var planner = new VisualEngineeringPlanner();
        var request = VisualRequest();
        var developerTask = planner.ToDeveloperTask(request, planner.CreatePlan(request));
        Assert(developerTask.ApprovedWorkspace == request.ApprovedWorkspace, "approved workspace should carry through.");
        Assert(developerTask.IterationLimit == 2, "visual task should cap Developer iterations at two.");
        Assert(developerTask.ProhibitedOperations.Any(item => item.Contains("functional workflow", StringComparison.OrdinalIgnoreCase)), "functional preservation must be prohibited.");
        Assert(developerTask.Objective.Contains("Preserve functionality", StringComparison.Ordinal), "Developer objective must include functionality boundary.");
    }

    private static void MediaPlannerCreatesSixteenPanelTimeline()
    {
        var request = MediaRequest();
        var plan = new MediaProductionPlanner().CreatePlan(request);
        Assert(plan.Timeline.Count == 16, "timeline should contain sixteen shots.");
        Assert(plan.TotalDuration == TimeSpan.FromMinutes(5), $"duration was {plan.TotalDuration}.");
        Assert(plan.Timeline[0].Start == TimeSpan.Zero, "first shot should start at zero.");
        Assert(plan.Timeline[^1].Start + plan.Timeline[^1].Duration == TimeSpan.FromMinutes(5), "last shot should finish at target duration.");
    }

    private static void MediaPlannerRequiresPostRenderQa()
    {
        var plan = new MediaProductionPlanner().CreatePlan(MediaRequest());
        Assert(plan.RequiredQaChecks.Contains("sampled_frames_reviewed"), "sampled frame QA required.");
        Assert(plan.RequiredQaChecks.Contains("text_not_cropped"), "cropping QA required.");
        Assert(plan.RequiredQaChecks.Contains("audio_track_present"), "audio QA required.");
    }

    private static void MediaPlannerRejectsDuplicatePanelIndexes()
    {
        var authority = AlexisDesignAuthority.CreateBaseline();
        AssertThrows<InvalidOperationException>(() => new MediaProductionPlanner().CreatePlan(new(
            Guid.NewGuid(),
            authority,
            [new(1, "panel-01.png", "Panel 01"), new(1, "panel-duplicate.png", "Panel 01 duplicate")],
            TimeSpan.FromMinutes(5),
            MediaOutputFormat.Youtube16x9Master)));
    }

    private static void MediaInputValidatesSixteenImages()
    {
        var fixture = MediaFixture();
        new MediaInputValidator().Validate(fixture.Request);
    }

    private static void MediaInputRejectsMissingImages()
    {
        var fixture = MediaFixture();
        File.Delete(fixture.Request.Panels[5].Path);
        AssertThrows<FileNotFoundException>(() => new MediaInputValidator().Validate(fixture.Request));
    }

    private static void MediaInputRejectsOutputOutsideApprovedRoot()
    {
        var fixture = MediaFixture();
        var request = fixture.Request with { OutputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "bad.mp4") };
        AssertThrows<UnauthorizedAccessException>(() => new MediaInputValidator().Validate(request));
    }

    private static void MediaCommandConstructionIncludesRendererControls()
    {
        var fixture = MediaFixture(includeAudio: true);
        var plan = new MediaProductionPlanner().CreatePlan(new(fixture.Request.MissionId, fixture.Request.Authority, fixture.Request.Panels, fixture.Request.TargetDuration, MediaOutputFormat.Youtube16x9Master));
        var command = new FfmpegCommandBuilder().Build(new("ffmpeg.exe", "ffprobe.exe"), fixture.Request, plan);
        Assert(command.Arguments.Contains("-filter_complex"), "command should include filter graph.");
        Assert(command.DisplayCommand.Contains("xfade", StringComparison.Ordinal), "command should include dissolve transitions.");
        Assert(command.DisplayCommand.Contains("zoompan", StringComparison.Ordinal), "command should include restrained pan/zoom.");
        Assert(command.DisplayCommand.Contains("drawtext", StringComparison.Ordinal), "command should include title overlays.");
        Assert(command.DisplayCommand.Contains("amix", StringComparison.Ordinal), "command should mix narration and music.");
        Assert(command.Arguments.Contains("-t"), "command should constrain final duration.");
        Assert(command.Arguments.Last().EndsWith(".mp4", StringComparison.OrdinalIgnoreCase), "command should end with mp4 output.");
    }

    private static void MediaRenderFailureEvidenceIsStructured()
    {
        var evidence = new MediaRenderEvidence(
            Guid.NewGuid(),
            MediaRenderStatus.Failed,
            @"C:\out\alexis.mp4",
            new("ffmpeg.exe", ["-bad"], @"C:\out", "ffmpeg.exe -bad"),
            new(1, false, "renderer error", @"C:\out\alexis.mp4", 0),
            null,
            null,
            "renderer_failed");
        Assert(evidence.Status == MediaRenderStatus.Failed, "failed status should be explicit.");
        Assert(evidence.Execution?.ExitCode == 1, "exit code should be preserved.");
        Assert(evidence.Execution?.StandardError == "renderer error", "stderr should be preserved.");
    }

    private static void MediaProbeQaAcceptsValidEvidence()
    {
        var fixture = MediaFixture(includeAudio: true);
        var plan = new MediaProductionPlanner().CreatePlan(new(fixture.Request.MissionId, fixture.Request.Authority, fixture.Request.Panels, fixture.Request.TargetDuration, MediaOutputFormat.Youtube16x9Master));
        var qa = new MediaQaValidator().Validate(
            fixture.Request,
            plan,
            new(0, false, "", fixture.Request.OutputPath, 4096),
            new(fixture.Request.OutputPath, fixture.Request.TargetDuration, 1920, 1080, 30, true),
            Enumerable.Range(1, 16).Select(index => $"scene-{index:00}.png").ToArray());
        Assert(qa.Accepted, "valid video evidence should pass QA.");
        Assert(qa.PassedChecks.Contains("all_16_scenes_ordered"), "scene ordering check should pass.");
    }

    private static void MediaQaRejectsBadDuration()
    {
        var fixture = MediaFixture();
        var plan = new MediaProductionPlanner().CreatePlan(new(fixture.Request.MissionId, fixture.Request.Authority, fixture.Request.Panels, fixture.Request.TargetDuration, MediaOutputFormat.Youtube16x9Master));
        var qa = new MediaQaValidator().Validate(
            fixture.Request,
            plan,
            new(0, false, "", fixture.Request.OutputPath, 4096),
            new(fixture.Request.OutputPath, TimeSpan.FromMinutes(4), 1920, 1080, 30, false),
            Enumerable.Range(1, 16).Select(index => $"scene-{index:00}.png").ToArray());
        Assert(!qa.Accepted, "bad duration should reject QA.");
        Assert(qa.FailedChecks.Contains("duration_within_tolerance"), "duration failure should be explicit.");
    }

    private static void MediaAuditWritesEvidence()
    {
        var root = Path.Combine(Path.GetTempPath(), "jarvis-media-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "media-audit.jsonl");
        var missionId = Guid.NewGuid();
        var audit = new JsonMediaAudit(path);
        audit.Write(missionId, "qa_finished", new { accepted = true });
        Assert(File.Exists(path), "audit file should exist.");
        var text = File.ReadAllText(path);
        Assert(text.Contains("qa_finished", StringComparison.Ordinal), "audit stage should be written.");
        Assert(text.Contains(missionId.ToString(), StringComparison.OrdinalIgnoreCase), "mission ID should be written.");
    }

    private static VisualEngineeringRequest VisualRequest() => new(
        Guid.NewGuid(),
        @"C:\Approved\Alexis",
        "System Health",
        "Upgrade this page to Platinum Authority without changing functionality.",
        AlexisDesignAuthority.CreateBaseline(),
        VisualInspectionMode.RenderedApplication,
        3);

    private static MediaProductionRequest MediaRequest() => new(
        Guid.NewGuid(),
        AlexisDesignAuthority.CreateBaseline(),
        Enumerable.Range(1, 16).Select(index => new MediaSourcePanel(index, $"panel-{index:00}.png", $"Panel {index:00}")).ToArray(),
        TimeSpan.FromMinutes(5),
        MediaOutputFormat.Youtube16x9Master);

    private static MediaFixtureData MediaFixture(bool includeAudio = false)
    {
        var root = Path.Combine(Path.GetTempPath(), "jarvis-media-tests", Guid.NewGuid().ToString("N"));
        var input = Path.Combine(root, "input");
        var output = Path.Combine(root, "output");
        Directory.CreateDirectory(input);
        Directory.CreateDirectory(output);
        var panels = Enumerable.Range(1, 16).Select(index =>
        {
            var path = Path.Combine(input, $"alexis-panel-{index:00}.png");
            File.WriteAllBytes(path, [137, 80, 78, 71]);
            return new MediaSourcePanel(index, path, $"ALEXIS Panel {index:00}");
        }).ToArray();

        string? narration = null;
        string? music = null;
        if (includeAudio)
        {
            narration = Path.Combine(input, "narration.wav");
            music = Path.Combine(input, "music.mp3");
            File.WriteAllBytes(narration, [1, 2, 3]);
            File.WriteAllBytes(music, [4, 5, 6]);
        }

        return new(new(
            Guid.NewGuid(),
            AlexisDesignAuthority.CreateBaseline(),
            panels,
            TimeSpan.FromMinutes(5),
            Path.Combine(output, "alexis-promo.mp4"),
            new("ALEXIS", "Workshop diagnostic intelligence", "Jarvis Media Studio"),
            narration,
            music,
            input,
            output));
    }

    private void Run(string name, Action test)
    {
        try
        {
            test();
            passed++;
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            failed++;
            Console.WriteLine($"FAIL {name}: {ex.Message}");
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertThrows<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    private sealed record MediaFixtureData(MediaStudioRenderRequest Request);
}
