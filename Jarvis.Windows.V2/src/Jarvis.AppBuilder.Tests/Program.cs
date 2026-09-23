using Jarvis.AppBuilder;
using System.Text.Json;
using Jarvis.Governance;
using Jarvis.Developer;

var tests = new AppBuilderTests();
await tests.RunAll();

internal sealed class AppBuilderTests
{
    private int passed;

    public async Task RunAll()
    {
        await Run("project persistence", ProjectPersistence);
        await Run("stable requirement IDs", StableRequirementIds);
        await Run("requirement update without unrelated loss", RequirementUpdateWithoutUnrelatedLoss);
        await Run("design token persistence", DesignTokenPersistence);
        await Run("System Scan creates ScreenDesignSpec", SystemScanCreatesScreenDesignSpec);
        await Run("ScreenDesignSpec remains renderer independent", ScreenDesignSpecRemainsRendererIndependent);
        await Run("semantic component IDs are stable", SemanticComponentIdsAreStable);
        await Run("state dimensions remain independent", StateDimensionsRemainIndependent);
        await Run("normative data semantics remain distinct", NormativeDataSemanticsRemainDistinct);
        await Run("revision inheritance", RevisionInheritance);
        await Run("DesignRevision persists structured snapshot", DesignRevisionPersistsStructuredSnapshot);
        await Run("candidate accepted rejected states", CandidateAcceptedRejectedStates);
        await Run("human-only acceptance", HumanOnlyAcceptance);
        await Run("artifact registration", ArtifactRegistration);
        await Run("governed System Scan mock tool", GovernedSystemScanMockTool);
        await Run("truthful inventory and blocked media provider", TruthfulInventoryAndBlockedMediaProvider);
        await Run("System Scan artifact is produced from ScreenDesignSpec", SystemScanArtifactIsProducedFromScreenDesignSpec);
        await Run("restart reload", RestartReload);
        await Run("restart reload restores project screen revision context", RestartReloadRestoresProjectScreenRevisionContext);
        await Run("ALEXIS profile loading", AlexisProfileLoading);
        await Run("prohibited requirement preservation", ProhibitedRequirementPreservation);
        await Run("System Scan conversational revision", SystemScanConversationalRevision);
        await Run("ambiguous revision requires clarification", AmbiguousRevisionRequiresClarification);
        await Run("governed conversational revision tool", GovernedConversationalRevisionTool);
        await Run("continue resolves active objective", ContinueResolvesActiveObjective);
        await Run("delegated reversible action chooses next step", DelegatedReversibleActionChoosesNextStep);
        await Run("selection resolves this and local revision preserves unrelated", SelectionResolvesThisAndRevisionPreservesUnrelated);
        await Run("bounded Developer handoff", BoundedDeveloperHandoff);
        await Run("distinct Builder acceptance states", DistinctBuilderAcceptanceStates);
        await Run("AppBuilder stages intact", AppBuilderStagesIntact);
        await Run("live UI context reports Builder System Scan", LiveUiContextReportsBuilderSystemScan);
        await Run("workspace aliases are bounded", WorkspaceAliasesAreBounded);
        await Run("governed workspace navigation", GovernedWorkspaceNavigation);
        await Run("universal composer routes long Builder prompt", UniversalComposerRoutesLongBuilderPrompt);
        await Run("active Builder prompt stays in Builder", ActiveBuilderPromptStaysInBuilder);
        await Run("universal composer reaches manual workspaces", UniversalComposerReachesManualWorkspaces);
        Console.WriteLine($"APP_BUILDER_TESTS_TOTAL passed={passed} failed=0");
    }

    private async Task Run(string name, Func<Task> test)
    {
        await test();
        passed++;
        Console.WriteLine($"PASS appbuilder {name}");
    }

    private static AppBuilderService Service(out string root)
    {
        root = Path.Combine(Path.GetTempPath(), "jarvis-appbuilder-tests", Guid.NewGuid().ToString("N"));
        return new AppBuilderService(new AppBuilderStore(root));
    }

    private static async Task ProjectPersistence()
    {
        var service = Service(out var root);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        var reloaded = await new AppBuilderStore(root).LoadProjectAsync(project.Id);
        Assert(reloaded.Name == "ALEXIS Design Project", "project should persist");
    }

    private static async Task StableRequirementIds()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        var ids = project.Requirements.Select(r => r.Id).ToArray();
        await service.UpdateRequirementAsync(project.Id, ids[0], "Use page background #050915 with no drift.");
        var updated = await service.CreateOrLoadAlexisProjectAsync();
        Assert(updated.Requirements.Select(r => r.Id).SequenceEqual(ids), "IDs should remain stable after update");
    }

    private static async Task RequirementUpdateWithoutUnrelatedLoss()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        var before = project.Requirements.Count;
        await service.UpdateRequirementAsync(project.Id, "REQ-ALEXIS-001", "Make the background much darker while preserving ALEXIS baseline intent.");
        var updated = await service.CreateOrLoadAlexisProjectAsync();
        Assert(updated.Requirements.Count == before, "unrelated requirements should not be dropped");
        Assert(updated.Requirements.Any(r => r.Id == "REQ-ALEXIS-SYSTEMSCAN-001"), "screen requirement should remain");
    }

    private static async Task DesignTokenPersistence()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        var updated = await service.CreateOrLoadAlexisProjectAsync();
        Assert(updated.DesignAuthority.ColorTokens["accentBlue"] == "#2F7CFF", "accent token should persist");
    }

    private static async Task SystemScanCreatesScreenDesignSpec()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        var result = await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        var spec = result.Project.CurrentScreenDesignSpec ?? throw new InvalidOperationException("missing ScreenDesignSpec");
        Assert(spec.Metadata.ScreenId == "alexis.systemScan", "screen id should be stable");
        Assert(spec.ContextModel.OperatingContext == OperatingContext.WorkshopStationary, "fixture context should be workshop stationary");
    }

    private static async Task ScreenDesignSpecRemainsRendererIndependent()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        var result = await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        var json = JsonSerializer.Serialize(result.Project.CurrentScreenDesignSpec, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        foreach (var forbidden in new[] { "html", "css", "xaml", "qml", "react", "<div", "grid-template", "WebView" })
            Assert(!json.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"spec should not contain renderer-specific token {forbidden}");
    }

    private static async Task SemanticComponentIdsAreStable()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        var result = await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        var ids = result.Project.CurrentScreenDesignSpec!.ComponentTree.Select(c => c.SemanticId).ToHashSet(StringComparer.Ordinal);
        foreach (var id in new[]
        {
            "alexis.systemScan.vehicleContext",
            "alexis.systemScan.scanProgress",
            "alexis.systemScan.moduleNavigator",
            "alexis.systemScan.networkTopology",
            "alexis.systemScan.findingsSummary",
            "alexis.systemScan.evidenceInspector",
            "alexis.systemScan.primaryActions"
        })
            Assert(ids.Contains(id), $"missing semantic id {id}");
    }

    private static async Task StateDimensionsRemainIndependent()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        var result = await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        var context = result.Project.CurrentScreenDesignSpec!.ContextModel;
        Assert(context.MotionState == MotionState.Stationary, "motion state should stand alone");
        Assert(context.IgnitionState == IgnitionState.On, "ignition state should stand alone");
        Assert(context.ConnectionState == ConnectionState.Degraded, "connection state should stand alone");
        Assert(context.ScanState == ScanState.Complete, "scan state should stand alone");
        Assert(context.DiagnosticStates.Contains(DiagnosticState.ActiveFaults), "diagnostic active faults should stand alone");
        Assert(context.DiagnosticStates.Contains(DiagnosticState.CommunicationFaults), "diagnostic communication faults should stand alone");
        Assert(context.DataState == DataState.Current, "data state should stand alone");
        Assert(context.OperationState == OperationState.Ready, "operation state should stand alone");
        Assert(context.SafetyState == SafetyState.Caution, "safety state should stand alone");
    }

    private static Task NormativeDataSemanticsRemainDistinct()
    {
        var required = new[]
        {
            NormativeDataSemantic.Zero,
            NormativeDataSemantic.Missing,
            NormativeDataSemantic.Unavailable,
            NormativeDataSemantic.Unsupported,
            NormativeDataSemantic.Stale,
            NormativeDataSemantic.Invalid,
            NormativeDataSemantic.Failed,
            NormativeDataSemantic.Unknown
        };
        Assert(required.Select(x => x.ToString()).Distinct(StringComparer.Ordinal).Count() == required.Length, "normative data semantics should remain distinct");
        Assert(required.Select(x => (int)x).Distinct().Count() == required.Length, "normative data semantics should have distinct enum values");
        return Task.CompletedTask;
    }

    private static async Task RevisionInheritance()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        await service.GenerateSystemScanMockAsync(project.Id, "Revision 1");
        await service.ApplyConversationalRevisionAsync(project.Id, "Make the topology larger, make the left panel narrower and remove the bright borders.");
        await service.GenerateSystemScanMockAsync(project.Id, "Revision 2");
        var updated = await service.CreateOrLoadAlexisProjectAsync();
        Assert(updated.RevisionHistory.Count == 2, "two revisions should be preserved");
        Assert(updated.Requirements.Any(r => r.Id == "REQ-ALEXIS-006"), "unchanged domain boundary should be inherited");
    }

    private static async Task DesignRevisionPersistsStructuredSnapshot()
    {
        var service = Service(out var root);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        var reloaded = await new AppBuilderStore(root).LoadProjectAsync(project.Id);
        var revision = reloaded.RevisionHistory.Single();
        Assert(revision.RevisionId == "system-scan-r001", "revision id should persist");
        Assert(revision.DesignSpecSnapshot is not null, "design spec snapshot should persist inside revision");
        Assert(revision.RenderArtifact.EndsWith(".html", StringComparison.OrdinalIgnoreCase), "render artifact should persist");
        Assert(revision.SemanticTreeArtifact.EndsWith(".semantic-tree.json", StringComparison.OrdinalIgnoreCase), "semantic tree artifact should persist");
        Assert(revision.AcceptanceState == DesignRevisionAcceptanceState.Candidate, "revision acceptance state should persist");
    }

    private static async Task CandidateAcceptedRejectedStates()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        var (_, artifact, _) = await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        var accepted = await service.MarkArtifactAsync(project.Id, artifact.Id, ArtifactAcceptanceState.Accepted, true);
        Assert(accepted.GeneratedArtifacts.Single().AcceptanceState == ArtifactAcceptanceState.Accepted, "acceptance should persist");
        var rejected = await service.MarkArtifactAsync(project.Id, artifact.Id, ArtifactAcceptanceState.Rejected, true);
        Assert(rejected.GeneratedArtifacts.Single().AcceptanceState == ArtifactAcceptanceState.Rejected, "rejection should persist");
    }

    private static async Task HumanOnlyAcceptance()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        var (_, artifact, _) = await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        try
        {
            await service.MarkArtifactAsync(project.Id, artifact.Id, ArtifactAcceptanceState.Accepted, false);
            throw new InvalidOperationException("acceptance without human should fail");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Human confirmation", StringComparison.OrdinalIgnoreCase)) { }
    }

    private static async Task ArtifactRegistration()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        var (_, artifact, evaluation) = await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        Assert(File.Exists(artifact.ArtifactPath), "artifact file should exist");
        Assert(File.Exists(artifact.EvaluationPath), "evaluation file should exist");
        Assert(evaluation.RequiredComponentsPresent.Contains("network-topology"), "topology evidence should be present");
    }

    private static async Task GovernedSystemScanMockTool()
    {
        var service = Service(out _);
        AppBuilderMockEvidence? callbackEvidence = null;
        var tool = new AppBuilderDesignSystemScanMockTool(service, evidence => callbackEvidence = evidence);
        Assert(tool.Definition.Name == AppBuilderDesignSystemScanMockTool.Name, "tool name should be stable");
        Assert(tool.Definition.RequiredArguments.SequenceEqual(["objective"]), "tool schema should require objective only");
        var result = await tool.InvokeAsync(new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AppBuilderDesignSystemScanMockTool.Name,
            JsonSerializer.SerializeToElement(new { objective = "Design me an ALEXIS System Scan mock screen." })));
        Assert(result.Status == GovernedToolStatus.Succeeded, "tool should succeed");
        var evidence = result.Evidence as AppBuilderMockEvidence ?? throw new InvalidOperationException("expected AppBuilder evidence");
        Assert(callbackEvidence?.ArtifactId == evidence.ArtifactId, "render callback should receive evidence");
        Assert(File.Exists(evidence.ArtifactPath), "artifact path should exist");
        Assert(File.Exists(evidence.EvaluationPath), "evaluation path should exist");
        Assert(evidence.AcceptanceState == ArtifactAcceptanceState.Candidate.ToString(), "artifact should remain candidate");
        Assert(evidence.Findings.Count > 0, "truthful findings should be returned");
    }

    private static async Task SystemScanArtifactIsProducedFromScreenDesignSpec()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        var (_, artifact, _) = await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        var html = await File.ReadAllTextAsync(artifact.ArtifactPath);
        Assert(html.Contains("data-semantic-id=\"alexis.systemScan.networkTopology\"", StringComparison.Ordinal), "artifact should expose semantic id from spec");
        Assert(html.Contains("data-semantic-id=\"alexis.systemScan.primaryActions\"", StringComparison.Ordinal), "artifact should expose action semantic id from spec");
        Assert(html.Contains("builder.selection", StringComparison.Ordinal), "artifact should expose semantic click selection bridge");
    }

    private static Task TruthfulInventoryAndBlockedMediaProvider()
    {
        var service = Service(out _);
        var inventory = service.GetCapabilityInventory();
        Assert(inventory.Capabilities.Any(c => c.Id == "screen_design_spec" && c.State == BuilderCapabilityState.Available), "ScreenDesignSpec should be available");
        Assert(inventory.Capabilities.Any(c => c.Id == "media_image_generation" && c.State is BuilderCapabilityState.Blocked or BuilderCapabilityState.Partial), "media provider should be truthful blocked or partial");
        Assert(inventory.Capabilities.Any(c => c.Id == "release" && c.State == BuilderCapabilityState.NotImplemented), "release should not be faked");
        Assert(inventory.Capabilities.Count >= 24, "inventory should cover requested capability surface");
        return Task.CompletedTask;
    }

    private static async Task RestartReload()
    {
        var service = Service(out var root);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        var afterRestart = new AppBuilderService(new AppBuilderStore(root));
        var loaded = await afterRestart.CreateOrLoadAlexisProjectAsync();
        Assert(loaded.GeneratedArtifacts.Count == 1, "restart reload should preserve artifacts");
    }

    private static async Task RestartReloadRestoresProjectScreenRevisionContext()
    {
        var service = Service(out var root);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        var rendered = await service.GenerateSystemScanMockAsync(project.Id, "System Scan objective");
        await service.SelectComponentAsync(project.Id, "alexis.systemScan.networkTopology");
        var restarted = new AppBuilderService(new AppBuilderStore(root));
        var restored = await restarted.LoadLatestActiveProjectAsync();
        var context = await restarted.LoadLatestContextAsync();
        Assert(restored?.Id == project.Id, "latest active project should recover after restart");
        Assert(context?.RevisionId == rendered.Project.CurrentScreenDesignSpec!.Metadata.RevisionId, "revision should recover in durable context");
        Assert(context?.Selection?.SemanticId == "alexis.systemScan.networkTopology", "selection should recover in durable context");
        Assert(File.Exists(Path.Combine(root, "context", "latest-active.builder-context.json")), "context persistence path should exist");
    }

    private static async Task AlexisProfileLoading()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        Assert(project.DesignAuthority.VisualDirection.Contains("premium", StringComparison.OrdinalIgnoreCase), "profile should load ALEXIS direction");
        Assert(project.DesignAuthority.ReferenceArtifacts.Count > 0, "profile should include references");
    }

    private static async Task ProhibitedRequirementPreservation()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        await service.ApplyConversationalRevisionAsync(project.Id, "Make the topology larger.");
        var updated = await service.CreateOrLoadAlexisProjectAsync();
        Assert(updated.Requirements.Any(r => r.Classification == RequirementClassification.Prohibited), "prohibited requirements should remain");
    }

    private static async Task SystemScanConversationalRevision()
    {
        var service = Service(out var root);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        await service.GenerateSystemScanMockAsync(project.Id, "Freeze Frame objective should not replace System Scan");
        var result = await service.ReviseSystemScanAsync(project.Id, "Make the left module panel 20% narrower.");
        var module = result.Project.CurrentScreenDesignSpec!.ComponentTree.Single(c => c.SemanticId == "alexis.systemScan.moduleNavigator");
        Assert(result.Request.TargetComponentId == "alexis.systemScan.moduleNavigator", "left module reference should resolve semantically");
        Assert(module.Properties["widthPx"] == "216", "module width should be persisted in revised spec");
        Assert(result.CollateralChanges.Any(c => c.ComponentId == "alexis.systemScan.networkTopology"), "topology compensation should be collateral");
        Assert(result.Project.CurrentScreenDesignSpec.TaskModel.PrimaryTask == PrimaryTask.Scan, "active objective should remain System Scan");
        var reloaded = await new AppBuilderService(new AppBuilderStore(root)).CreateOrLoadAlexisProjectAsync();
        Assert(reloaded.RevisionHistory.Last().DesignSpecSnapshot!.ComponentTree.Single(c => c.SemanticId == "alexis.systemScan.moduleNavigator").Properties["widthPx"] == "216", "revision should survive reload");
        var html = await File.ReadAllTextAsync(result.Artifact.ArtifactPath);
        Assert(html.Contains("grid-template-columns:1.3fr 0.8fr", StringComparison.Ordinal), "renderer should consume revised spec allocation");
    }

    private static async Task AmbiguousRevisionRequiresClarification()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        try
        {
            await service.ReviseSystemScanAsync(project.Id, "Make it smaller.");
            throw new InvalidOperationException("ambiguous revision should not mutate");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Clarification required", StringComparison.OrdinalIgnoreCase)) { }
    }

    private static async Task GovernedConversationalRevisionTool()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        var tool = new AppBuilderSystemScanRevisionTool(service);
        var result = await tool.InvokeAsync(new(Guid.NewGuid(), Guid.NewGuid(), AppBuilderSystemScanRevisionTool.Name, JsonSerializer.SerializeToElement(new { objective = "Make the topology wider." })));
        Assert(result.Status == GovernedToolStatus.Succeeded, "revision tool should succeed");
        Assert(result.Evidence is SystemScanRevisionResult revision && revision.Artifact.Revision == 2, "revision tool should return new artifact evidence");
    }

    private static async Task ContinueResolvesActiveObjective()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        await service.GenerateSystemScanMockAsync(project.Id, "System Scan objective");
        project = await service.CreateOrLoadAlexisProjectAsync();
        var context = await service.LoadLatestContextAsync();
        var decision = service.ResolveInitiative(project, context, "I don't know where to start");
        Assert(decision.Action == BuilderInitiativeActionKind.ContinueExistingObjective, "continue should resolve existing objective");
        Assert(decision.GovernedToolName == AppBuilderSystemScanRevisionTool.Name, "next step should be governed revision");
    }

    private static async Task DelegatedReversibleActionChoosesNextStep()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        var context = await service.LoadLatestContextAsync();
        var decision = service.ResolveInitiative(project, context, "Improve the Builder screen");
        Assert(decision.Action == BuilderInitiativeActionKind.RenderCandidate, "first reversible Builder step should render when no candidate exists");
        Assert(decision.GovernedToolName == AppBuilderDesignSystemScanMockTool.Name, "render should use governed Builder tool");
        Assert(decision.ClarifyingQuestion is null, "reversible delegated work should not ask where to start");
    }

    private static async Task SelectionResolvesThisAndRevisionPreservesUnrelated()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        var selection = await service.SelectComponentAsync(project.Id, "alexis.systemScan.networkTopology");
        var context = await service.LoadLatestContextAsync();
        var decision = service.ResolveInitiative(await service.CreateOrLoadAlexisProjectAsync(), context, "Make this 10% wider.");
        Assert(decision.Arguments["objective"].Contains(selection.SemanticId, StringComparison.Ordinal), "this should resolve to active selection");
        var revised = await service.ReviseSystemScanAsync(project.Id, decision.Arguments["objective"]);
        Assert(revised.Request.TargetComponentId == "alexis.systemScan.networkTopology", "selection should drive revision target");
        Assert(revised.PreservedDecisions.Count > 0, "local revision should preserve unrelated design decisions");
    }

    private static async Task BoundedDeveloperHandoff()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        var rendered = await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        await service.MarkArtifactAsync(project.Id, rendered.Artifact.Id, ArtifactAcceptanceState.Accepted, true);
        var runner = new FakeBuilderDeveloperRunner();
        var evidence = await service.SubmitImplementationTaskAsync(project.Id, "C:\\ApprovedWorkspace", "Implement preview shell only", runner);
        Assert(runner.Requests.Count == 1, "Developer handoff should submit exactly one bounded task");
        Assert(runner.Requests[0].IterationLimit == 2, "Developer handoff should remain bounded");
        Assert(runner.Requests[0].ProhibitedOperations.Contains("deployment"), "Developer handoff should prohibit deployment");
        Assert(evidence.State == BuilderDeliveryState.Tested, "successful handoff should mark tested evidence state");
    }

    private static async Task DistinctBuilderAcceptanceStates()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        var rendered = await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        var accepted = await service.MarkArtifactAsync(project.Id, rendered.Artifact.Id, ArtifactAcceptanceState.Accepted, true);
        var context = await service.LoadLatestContextAsync();
        Assert(accepted.AcceptanceStatus == ArtifactAcceptanceState.Candidate, "project acceptance status should not imply physical acceptance");
        Assert(context!.DeliveryStates.Contains(BuilderDeliveryState.Rendered), "rendered state should be distinct");
        Assert(!context.DeliveryStates.Contains(BuilderDeliveryState.PhysicalAccepted), "physical acceptance must not be inferred");
    }

    private static async Task AppBuilderStagesIntact()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        var rendered = await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        var spec = rendered.Project.CurrentScreenDesignSpec!;
        Assert(spec.ContextModel is not null, "context model should exist before rendering");
        Assert(spec.UserModel.Role == UserRole.Technician, "role model should exist before rendering");
        Assert(spec.TaskModel.PrimaryTask == PrimaryTask.Scan, "task model should exist before rendering");
        Assert(spec.DomainModel.RequiredCapabilities.Count > 0, "domain model should exist before rendering");
        Assert(spec.InformationArchitecture.PrimaryInformation.Count > 0, "IA should exist before rendering");
        Assert(spec.AcceptanceCriteria.Count > 0, "acceptance criteria should exist before rendering");
        Assert(spec.DesignRationale.Count > 0, "rationale should exist before rendering");
        Assert(spec.Typography?.Count > 0, "structured typography should exist");
        Assert(spec.LayoutAttributes?.Count > 0, "structured layout attributes should exist");
    }

    private static async Task LiveUiContextReportsBuilderSystemScan()
    {
        var service = Service(out _);
        var project = await service.CreateOrLoadAlexisProjectAsync();
        var rendered = await service.GenerateSystemScanMockAsync(project.Id, "System Scan");
        var spec = rendered.Project.CurrentScreenDesignSpec!;
        var context = new JarvisUiContext(JarvisWorkspace.BUILDER, project.Id, project.Name, spec.Metadata.ScreenId, spec.Metadata.ScreenName, spec.Metadata.RevisionId, rendered.Artifact.ArtifactPath, rendered.Artifact.AcceptanceState.ToString(), spec.ComponentTree.Select(c => c.SemanticId).ToArray(), null, DateTimeOffset.UtcNow);
        Assert(context.ActiveWorkspace == JarvisWorkspace.BUILDER && context.ActiveScreenName == "ALEXIS System Scan", "context should report active Builder System Scan");
        Assert(context.ActiveSemanticComponentIds.Contains("alexis.systemScan.networkTopology"), "context should expose semantic IDs");
    }

    private static Task WorkspaceAliasesAreBounded()
    {
        Assert(NavigateJarvisWorkspaceTool.TryResolve("go to developer", out var developer) && developer == JarvisWorkspace.DEVELOPER, "developer alias should resolve");
        Assert(NavigateJarvisWorkspaceTool.TryResolve("go home", out var home) && home == JarvisWorkspace.HOME, "home alias should resolve");
        Assert(NavigateJarvisWorkspaceTool.TryResolve("go to bold mode", out var builder) && builder == JarvisWorkspace.BUILDER, "clear Builder transcription alias should resolve");
        Assert(!NavigateJarvisWorkspaceTool.TryResolve("bold text", out _), "ordinary bold must remain ambiguous");
        return Task.CompletedTask;
    }

    private static async Task GovernedWorkspaceNavigation()
    {
        var selected = JarvisWorkspace.HOME;
        var tool = new NavigateJarvisWorkspaceTool(workspace => { selected = workspace; return Task.CompletedTask; });
        var result = await tool.InvokeAsync(new(Guid.NewGuid(), Guid.NewGuid(), NavigateJarvisWorkspaceTool.Name, JsonSerializer.SerializeToElement(new { workspace = "developer" })));
        Assert(result.Status == GovernedToolStatus.Succeeded && selected == JarvisWorkspace.DEVELOPER, "governed navigation should switch workspace");
        var rejected = await tool.InvokeAsync(new(Guid.NewGuid(), Guid.NewGuid(), NavigateJarvisWorkspaceTool.Name, JsonSerializer.SerializeToElement(new { workspace = "not-an-app" })));
        Assert(rejected.Status == GovernedToolStatus.Rejected, "unknown workspace should be rejected");
    }

    private static Task UniversalComposerRoutesLongBuilderPrompt()
    {
        var prompt = "Jarvis, create a new two-page application called VANTAGE Workshop Console. " + new string('x', 1400);
        var route = JarvisShellCommandRouter.Route(EmptyContext(JarvisWorkspace.HOME), prompt);
        Assert(route.Intent == JarvisShellCommandIntent.Builder && route.Workspace == JarvisWorkspace.BUILDER, "long application brief should route to Builder");
        return Task.CompletedTask;
    }

    private static Task ActiveBuilderPromptStaysInBuilder()
    {
        var route = JarvisShellCommandRouter.Route(EmptyContext(JarvisWorkspace.BUILDER), "Make this panel twenty percent narrower.");
        Assert(route.Intent == JarvisShellCommandIntent.Builder && route.Workspace == JarvisWorkspace.BUILDER, "active Builder revisions should stay in Builder");
        return Task.CompletedTask;
    }

    private static Task UniversalComposerReachesManualWorkspaces()
    {
        Assert(JarvisShellCommandRouter.Route(EmptyContext(JarvisWorkspace.HOME), "Open the code").Workspace == JarvisWorkspace.DEVELOPER, "developer intent should route");
        Assert(JarvisShellCommandRouter.Route(EmptyContext(JarvisWorkspace.HOME), "Generate an image").Workspace == JarvisWorkspace.MEDIA, "media intent should route");
        Assert(JarvisShellCommandRouter.Route(EmptyContext(JarvisWorkspace.HOME), "Open Calculator").Workspace == JarvisWorkspace.COMPUTER, "computer intent should route");
        Assert(NavigateJarvisWorkspaceTool.TryResolve("system", out var system) && system == JarvisWorkspace.SYSTEM, "manual launcher target should remain available");
        return Task.CompletedTask;
    }

    private static JarvisUiContext EmptyContext(JarvisWorkspace workspace) =>
        new(workspace, null, null, null, null, null, null, null, [], null, DateTimeOffset.UtcNow);

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakeBuilderDeveloperRunner : IBuilderDeveloperTaskRunner
    {
        public List<DeveloperTaskRequest> Requests { get; } = [];

        public Task<DeveloperTaskEvidence> RunAsync(DeveloperTaskRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(new DeveloperTaskEvidence(request.TaskId, request.ApprovedWorkspace, request.Objective, DeveloperTaskStatus.Succeeded, 1, 0, false, ["src/App.xaml"], ["dotnet build"], "implemented and tested", null));
        }
    }
}
