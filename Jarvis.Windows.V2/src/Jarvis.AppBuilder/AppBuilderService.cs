using System.Text;
using System.Text.Json;
using Jarvis.Developer;

namespace Jarvis.AppBuilder;

public interface IBuilderDeveloperTaskRunner
{
    Task<DeveloperTaskEvidence> RunAsync(DeveloperTaskRequest request, CancellationToken cancellationToken = default);
}

public sealed class AppBuilderService
{
    private readonly AppBuilderStore store;

    public AppBuilderService(AppBuilderStore store) => this.store = store;

    public string PersistenceRoot => store.Root;

    public BuilderCapabilityInventory GetCapabilityInventory()
    {
        var configuredMedia = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"));
        return new("builder-executive-intelligence-v2", DateTimeOffset.UtcNow,
        [
            Cap("realtime_conversation", "Realtime conversation", BuilderCapabilityState.Available, "Session contract and WebRTC host exist; physical audio acceptance remains separate."),
            Cap("internal_navigation", "Internal navigation", BuilderCapabilityState.Available, "Governed top-level workspace navigation is implemented."),
            Cap("ui_context", "UI context", BuilderCapabilityState.Available, "Current workspace, project, screen, revision, artifact and selection are exposed read-only."),
            Cap("screen_design_spec", "ScreenDesignSpec", BuilderCapabilityState.Available, "Structured screen model is persisted and renderer-independent."),
            Cap("semantic_component_model", "Semantic component model", BuilderCapabilityState.Available, "Components carry semanticId/type/parent/properties."),
            Cap("bounded_revisions", "Bounded revisions", BuilderCapabilityState.Available, "System Scan semantic layout revisions create DesignRevision records."),
            Cap("html_mock_renderer", "HTML/mock renderer", BuilderCapabilityState.Available, "Static HTML artifacts are rendered from ScreenDesignSpec."),
            Cap("persistent_projects_revisions", "Persistent projects/revisions", BuilderCapabilityState.Available, "Projects, specs, artifacts, semantic trees and context persist under app-builder storage."),
            Cap("developer_codex_orchestration", "Developer/Codex orchestration", BuilderCapabilityState.Partial, "Governed handoff can submit bounded tasks; Codex runtime availability is external.", "Approved workspace and Codex command host."),
            Cap("windows_ops", "Windows operations", BuilderCapabilityState.Available, "Governed Computer tools cover status, launch and bounded operations."),
            Cap("media_image_generation", "Media/image generation", configuredMedia ? BuilderCapabilityState.Partial : BuilderCapabilityState.Blocked, configuredMedia ? "Provider appears configured; generation remains governed and acceptance separated." : "Image provider credential is not configured or not visible to this process.", "Provider authorization."),
            Cap("barehands_3d", "Barehands/3D", BuilderCapabilityState.Partial, "Barehands workspace exists; Builder-specific 3D editing is not implemented.", "Future visual engineering seam."),
            Cap("screenshot_inspection", "Screenshot inspection", BuilderCapabilityState.NotImplemented, "No governed screenshot critique pipeline is implemented."),
            Cap("component_selection", "Component selection", BuilderCapabilityState.Available, "Semantic selection can be recorded and used for safe revisions."),
            Cap("property_inspector", "Property inspector", BuilderCapabilityState.Partial, "Safe editable properties are exposed; arbitrary live property editing is not implemented."),
            Cap("live_app_modification", "Live app modification", BuilderCapabilityState.Blocked, "Live app changes must go through bounded Developer handoff, not direct Builder mutation.", "Accepted spec and approved workspace."),
            Cap("accessibility", "Accessibility", BuilderCapabilityState.Partial, "Requirements are modeled; automated accessibility audit is not implemented."),
            Cap("responsive", "Responsive layout", BuilderCapabilityState.Partial, "ScreenDesignSpec stores responsive rules; renderer target is currently fixed 16:9 workstation preview."),
            Cap("typography", "Typography", BuilderCapabilityState.Available, "Structured TypographySpec is stored in ScreenDesignSpec."),
            Cap("visualization", "Visualization", BuilderCapabilityState.Partial, "Visualization intent is modeled; advanced data visualization engines are future seams."),
            Cap("assets_import", "Assets/import", BuilderCapabilityState.NotImplemented, "No governed asset import workflow is implemented."),
            Cap("build_test", "Build/test", BuilderCapabilityState.Partial, "Developer handoff can request build/test; results are distinct evidence states."),
            Cap("release", "Release", BuilderCapabilityState.NotImplemented, "Deployment/release is intentionally not implemented in Builder."),
            Cap("durable_continuity", "Durable continuity", BuilderCapabilityState.Available, "Latest active Builder context is persisted across restarts.")
        ]);
    }

    public async Task<AppBuilderProject?> LoadLatestActiveProjectAsync(CancellationToken cancellationToken = default)
    {
        var context = await store.LoadLatestContextAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(context?.ActiveProjectId) || !File.Exists(store.ProjectPath(context.ActiveProjectId)))
            return null;
        return await store.LoadProjectAsync(context.ActiveProjectId, cancellationToken).ConfigureAwait(false);
    }

    public Task<BuilderProjectContext?> LoadLatestContextAsync(CancellationToken cancellationToken = default) =>
        store.LoadLatestContextAsync(cancellationToken);

    public async Task<BuilderProjectContext> SaveContextAsync(AppBuilderProject project, string currentTask, string? lastAction = null, string? pendingAction = null, ActiveBuilderSelection? selection = null, CancellationToken cancellationToken = default)
    {
        var context = BuildContext(project, currentTask, lastAction, pendingAction, selection);
        await store.SaveContextAsync(context, cancellationToken).ConfigureAwait(false);
        return context;
    }

    public BuilderRealtimeContextBridge BuildRealtimeBridge(AppBuilderProject? project, BuilderProjectContext? context = null)
    {
        var spec = project?.CurrentScreenDesignSpec;
        var candidate = project?.GeneratedArtifacts.LastOrDefault(a => a.AcceptanceState == ArtifactAcceptanceState.Candidate) ?? project?.GeneratedArtifacts.LastOrDefault();
        var blockers = GetCapabilityInventory().Capabilities
            .Where(c => c.State is BuilderCapabilityState.Blocked or BuilderCapabilityState.NotImplemented)
            .Select(c => $"{c.Id}:{c.State}")
            .Take(8)
            .ToArray();
        return new(
            "BUILDER",
            project?.Name ?? context?.ActiveProjectName,
            context?.Objective ?? spec?.TaskModel.UserGoal ?? "No active Builder objective.",
            spec?.Metadata.ScreenName ?? context?.ScreenName,
            spec?.Metadata.RevisionId ?? context?.RevisionId,
            candidate?.AcceptanceState.ToString() ?? context?.AcceptanceState,
            context?.PendingGovernedAction,
            blockers,
            spec?.ComponentTree.Select(c => c.SemanticId).ToArray() ?? [],
            context?.Selection?.SemanticId);
    }

    public BuilderInitiativeDecision ResolveInitiative(AppBuilderProject? project, BuilderProjectContext? context, string utterance)
    {
        var text = utterance.Trim();
        var lower = text.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(text))
            return Decision(BuilderConversationIntent.Clarification, BuilderInitiativeActionKind.AskClarification, "No Builder instruction was supplied.", null, new Dictionary<string, string>(), "What Builder objective should I continue?");

        if (lower is "continue" or "go ahead" or "do it" or "you decide" or "what next" || lower.Contains("don't know where to start", StringComparison.Ordinal))
        {
            if (project?.CurrentScreenDesignSpec is null)
                return Decision(BuilderConversationIntent.DelegatedObjective, BuilderInitiativeActionKind.RenderCandidate, "No rendered candidate exists; first valid reversible step is to render from the authoritative ScreenDesignSpec pipeline.", AppBuilderDesignSystemScanMockTool.Name, new Dictionary<string, string> { ["objective"] = context?.Objective ?? "Render the active Builder candidate." }, null);
            return Decision(BuilderConversationIntent.DelegatedObjective, BuilderInitiativeActionKind.ContinueExistingObjective, "A candidate exists; next reversible step is bounded semantic revision or acceptance review, not an open question.", AppBuilderSystemScanRevisionTool.Name, new Dictionary<string, string> { ["objective"] = context?.CurrentTask ?? "Improve the active candidate using the current Builder objective." }, null);
        }

        if (lower.Contains("accept", StringComparison.Ordinal))
            return Decision(BuilderConversationIntent.Approval, BuilderInitiativeActionKind.Answer, "Acceptance requires explicit human action and remains distinct from implementation.", null, new Dictionary<string, string>(), null);
        if (lower.Contains("deploy", StringComparison.Ordinal) || lower.Contains("release", StringComparison.Ordinal))
            return Decision(BuilderConversationIntent.DelegatedObjective, BuilderInitiativeActionKind.StopUnsupported, "Release/deployment is not implemented in Builder and cannot be bypassed.", null, new Dictionary<string, string>(), null);
        if (lower.Contains("this", StringComparison.Ordinal) && context?.Selection is not null)
            return Decision(BuilderConversationIntent.DelegatedObjective, BuilderInitiativeActionKind.ReviseSelection, "Resolved pronoun to active semantic selection.", AppBuilderSystemScanRevisionTool.Name, new Dictionary<string, string> { ["objective"] = $"{text} target:{context.Selection.SemanticId}" }, null);
        if (lower.Contains("what can", StringComparison.Ordinal) || lower.Contains("capabilities", StringComparison.Ordinal))
            return Decision(BuilderConversationIntent.Information, BuilderInitiativeActionKind.Answer, "Information request should answer from truthful inventory.", GetBuilderCapabilityInventoryTool.Name, new Dictionary<string, string>(), null);

        var hasSpec = project?.CurrentScreenDesignSpec is not null;
        return Decision(BuilderConversationIntent.DelegatedObjective, hasSpec ? BuilderInitiativeActionKind.ContinueExistingObjective : BuilderInitiativeActionKind.RenderCandidate, "Delegated reversible Builder work should proceed at the next governed boundary.", hasSpec ? AppBuilderSystemScanRevisionTool.Name : AppBuilderDesignSystemScanMockTool.Name, new Dictionary<string, string> { ["objective"] = text }, null);
    }

    public async Task<ActiveBuilderSelection> SelectComponentAsync(string projectId, string semanticId, CancellationToken cancellationToken = default)
    {
        var project = await store.LoadProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
        var component = project.CurrentScreenDesignSpec?.ComponentTree.FirstOrDefault(c => c.SemanticId.Equals(semanticId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Component {semanticId} was not found in the active screen.");
        var safe = component.Properties
            .Where(kvp => kvp.Key is "widthPx" or "allocation" or "density" or "role" or "fitMode")
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);
        if (safe.Count == 0) safe["selection"] = "inspect-only";
        var selection = new ActiveBuilderSelection(component.SemanticId, component.ComponentType, component.Parent, safe, DateTimeOffset.UtcNow);
        await SaveContextAsync(project, $"Inspect {component.SemanticId}", "select_builder_component", null, selection, cancellationToken).ConfigureAwait(false);
        return selection;
    }

    public async Task<BuilderImplementationTaskEvidence> SubmitImplementationTaskAsync(string projectId, string approvedWorkspace, string boundedGoal, IBuilderDeveloperTaskRunner developer, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(approvedWorkspace))
            throw new InvalidOperationException("Approved workspace is required for Builder implementation handoff.");
        var project = await store.LoadProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
        var accepted = project.GeneratedArtifacts.LastOrDefault(a => a.AcceptanceState == ArtifactAcceptanceState.Accepted)
            ?? throw new InvalidOperationException("Accepted Builder artifact is required before implementation handoff.");
        var spec = project.CurrentScreenDesignSpec ?? throw new InvalidOperationException("Current ScreenDesignSpec is required before implementation handoff.");
        var objective = $"Implement accepted Builder spec {spec.Metadata.RevisionId} for project {project.Name}. Goal: {boundedGoal}. Preserve governed runtime boundaries. Use artifact {accepted.ArtifactPath}. Do not deploy.";
        var request = new DeveloperTaskRequest(
            Guid.NewGuid(),
            approvedWorkspace,
            objective,
            new HashSet<DeveloperOperationClass> { DeveloperOperationClass.Inspect, DeveloperOperationClass.Search, DeveloperOperationClass.EditSource, DeveloperOperationClass.Build, DeveloperOperationClass.Test },
            ["deployment", "release", "credential exposure", "destructive filesystem cleanup"],
            2,
            TimeSpan.FromMinutes(10));
        var result = await developer.RunAsync(request, cancellationToken).ConfigureAwait(false);
        var state = result.Status == DeveloperTaskStatus.Succeeded ? BuilderDeliveryState.Tested : BuilderDeliveryState.Implemented;
        await SaveContextAsync(project, boundedGoal, "submit_builder_implementation_task", null, null, cancellationToken).ConfigureAwait(false);
        return new(project.Id, approvedWorkspace, spec.Metadata.RevisionId, state, result.Status.ToString(), result.FinalReport, result.FailureReason);
    }

    private BuilderProjectContext BuildContext(AppBuilderProject project, string currentTask, string? lastAction, string? pendingAction, ActiveBuilderSelection? selection)
    {
        var spec = project.CurrentScreenDesignSpec;
        var artifact = project.GeneratedArtifacts.LastOrDefault(a => a.AcceptanceState == ArtifactAcceptanceState.Candidate) ?? project.GeneratedArtifacts.LastOrDefault();
        var blockers = GetCapabilityInventory().Capabilities
            .Where(c => c.State is BuilderCapabilityState.Blocked or BuilderCapabilityState.NotImplemented)
            .Select(c => $"{c.Id}: {c.Reason}")
            .ToArray();
        var accepted = project.AcceptedDecisions.Select(d => $"{d.Id}: {d.Value}").ToArray();
        var explicitLeon = project.Requirements
            .Where(r => r.Classification is RequirementClassification.Must or RequirementClassification.Prohibited)
            .Select(r => $"{r.Id}: {r.Statement}")
            .Take(12)
            .ToArray();
        var states = new List<BuilderDeliveryState> { BuilderDeliveryState.Planned };
        if (spec is not null) states.Add(BuilderDeliveryState.Designed);
        if (artifact is not null) states.Add(BuilderDeliveryState.Rendered);
        return new(
            project.Id,
            project.Name,
            project.Product.Purpose,
            spec?.TaskModel.UserGoal ?? currentTask,
            spec?.Metadata.ScreenId,
            spec?.Metadata.ScreenName,
            spec?.Metadata.RevisionId,
            artifact?.AcceptanceState.ToString(),
            artifact?.ArtifactPath,
            accepted,
            project.UnresolvedQuestions,
            explicitLeon,
            currentTask,
            lastAction,
            pendingAction,
            blockers,
            selection,
            states,
            DateTimeOffset.UtcNow);
    }

    private static BuilderCapability Cap(string id, string name, BuilderCapabilityState state, string reason = "", string dependency = "") =>
        new(id, name, state, reason, dependency);

    private static BuilderInitiativeDecision Decision(BuilderConversationIntent intent, BuilderInitiativeActionKind action, string rationale, string? tool, IReadOnlyDictionary<string, string> args, string? question) =>
        new(intent, action, rationale, tool, args, question);

    public async Task<AppBuilderProject> CreateOrLoadAlexisProjectAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(store.ProjectPath(AlexisDesignProfile.ProjectId)))
        {
            var existing = await store.LoadProjectAsync(AlexisDesignProfile.ProjectId, cancellationToken);
            await SaveContextAsync(existing, "Loaded active Builder project.", "load_builder_project", null, null, cancellationToken).ConfigureAwait(false);
            return existing;
        }

        var project = AlexisDesignProfile.CreateProject();
        await store.SaveProjectAsync(project, cancellationToken);
        await SaveContextAsync(project, "Created active Builder project.", "create_builder_project", null, null, cancellationToken).ConfigureAwait(false);
        return project;
    }

    public async Task<AppBuilderProject> UpdateRequirementAsync(string projectId, string requirementId, string statement, CancellationToken cancellationToken = default)
    {
        var project = await store.LoadProjectAsync(projectId, cancellationToken);
        var index = project.Requirements.FindIndex(r => r.Id.Equals(requirementId, StringComparison.OrdinalIgnoreCase));
        if (index < 0) throw new InvalidOperationException($"Requirement {requirementId} was not found.");
        var previous = project.Requirements[index];
        project.Requirements[index] = previous with { Statement = statement, UpdatedAt = DateTimeOffset.UtcNow };
        await store.SaveProjectAsync(project, cancellationToken);
        return project;
    }

    public async Task<AppBuilderProject> ApplyConversationalRevisionAsync(string projectId, string instruction, CancellationToken cancellationToken = default)
    {
        var project = await store.LoadProjectAsync(projectId, cancellationToken);
        UpdateOrAdd(project, "REQ-ALEXIS-SYSTEMSCAN-TOPOLOGY-SCALE", "screen", "Network topology should be larger than Revision 1.", instruction.Contains("topology", StringComparison.OrdinalIgnoreCase));
        UpdateOrAdd(project, "REQ-ALEXIS-SYSTEMSCAN-LEFT-PANEL", "screen", "Left-side panel should be narrower than Revision 1.", instruction.Contains("left", StringComparison.OrdinalIgnoreCase) && instruction.Contains("narrow", StringComparison.OrdinalIgnoreCase));
        UpdateOrAdd(project, "REQ-ALEXIS-SYSTEMSCAN-BORDERS", "visual", "Bright borders should be removed or muted.", instruction.Contains("border", StringComparison.OrdinalIgnoreCase));
        UpdateOrAdd(project, "REQ-ALEXIS-SYSTEMSCAN-DARKER", "visual", "Overall screen should be darker than Revision 1.", instruction.Contains("dark", StringComparison.OrdinalIgnoreCase));
        await store.SaveProjectAsync(project, cancellationToken);
        return project;
    }

    public async Task<SystemScanRevisionResult> ReviseSystemScanAsync(string projectId, string instruction, CancellationToken cancellationToken = default)
    {
        var project = await store.LoadProjectAsync(projectId, cancellationToken);
        var current = project.CurrentScreenDesignSpec;
        if (current is null || !current.Metadata.ScreenId.Equals("alexis.systemScan", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A current System Scan candidate is required before conversational revision.");

        var text = instruction.Trim();
        var lower = text.ToLowerInvariant();
        var explicitTarget = System.Text.RegularExpressions.Regex.Match(text, @"target:(?<id>[A-Za-z0-9_.-]+)").Groups["id"].Value;
        var target = !string.IsNullOrWhiteSpace(explicitTarget)
            ? explicitTarget
            : lower.Contains("left") || lower.Contains("module") || lower.Contains("workflow")
            ? "alexis.systemScan.moduleNavigator"
            : lower.Contains("topology") || lower.Contains("network")
                ? "alexis.systemScan.networkTopology"
                : lower.Contains("right") || lower.Contains("evidence") || lower.Contains("module panel")
                    ? "alexis.systemScan.evidenceInspector"
                    : string.Empty;
        var confidence = string.IsNullOrEmpty(target) ? "LOW" : "HIGH";
        var property = target == "alexis.systemScan.networkTopology" && (lower.Contains("allocation") || lower.Contains("space"))
            ? "relative horizontal allocation"
            : "width";
        if (string.IsNullOrEmpty(target) || (!lower.Contains("narrow") && !lower.Contains("wide") && !lower.Contains("space") && !lower.Contains("bigger") && !lower.Contains("larger") && !lower.Contains("smaller")))
            throw new InvalidOperationException("Clarification required: specify left module panel, topology, or right evidence panel and a bounded width change.");

        var percent = System.Text.RegularExpressions.Regex.Match(lower, @"(?<value>\d+)\s*%").Groups["value"].Value;
        var amount = int.TryParse(percent, out var parsed) ? Math.Clamp(parsed, 5, 30) : 10;
        var narrowing = lower.Contains("narrow") || lower.Contains("less space") || lower.Contains("smaller");
        var requested = $"{(narrowing ? "decrease" : "increase")} by {amount}%";
        var request = new SystemScanRevisionRequest(target, property, requested, confidence, "bounded layout-only change; unrelated state preserved", text);
        var components = current.ComponentTree.Select(c => c with { Properties = new Dictionary<string, string>(c.Properties) }).ToArray();
        var direct = new List<DesignChange>();
        var collateral = new List<DesignChange>();
        var targetIndex = Array.FindIndex(components, c => c.SemanticId == target);
        if (targetIndex < 0)
            throw new InvalidOperationException($"Clarification required: {target} is not an editable component in the active screen.");
        var targetComponent = components[targetIndex];
        var targetProperties = new Dictionary<string, string>(targetComponent.Properties);
        var before = targetProperties.TryGetValue("allocation", out var oldAllocation) ? oldAllocation : targetProperties.GetValueOrDefault("widthPx", "270");
        var baseValue = double.TryParse(before, out var numeric) ? numeric : 1.0;
        var next = Math.Round(baseValue * (narrowing ? 1 - amount / 100d : 1 + amount / 100d), 2);
        next = target == "alexis.systemScan.moduleNavigator" ? Math.Clamp(next, 180, 320) : Math.Clamp(next, .55, 1.6);
        var key = target == "alexis.systemScan.moduleNavigator" ? "widthPx" : "allocation";
        targetProperties[key] = key == "widthPx" ? ((int)Math.Round(next)).ToString() : next.ToString("0.##");
        components[targetIndex] = targetComponent with { Properties = targetProperties };
        direct.Add(new DesignChange(target, property, before, targetProperties[key], "Bounded conversational System Scan layout revision."));
        if (target == "alexis.systemScan.moduleNavigator")
        {
            var topologyIndex = Array.FindIndex(components, c => c.SemanticId == "alexis.systemScan.networkTopology");
            var topology = components[topologyIndex];
            var topologyProperties = new Dictionary<string, string>(topology.Properties);
            var topologyBefore = topologyProperties.GetValueOrDefault("allocation", "1.2");
            var topologyNext = (double.Parse(topologyBefore) * (narrowing ? 1.08 : .96)).ToString("0.##");
            topologyProperties["allocation"] = topologyNext;
            components[topologyIndex] = topology with { Properties = topologyProperties };
            collateral.Add(new DesignChange(topology.SemanticId, "relative horizontal allocation", topologyBefore, topologyNext, "Constraint-driven compensation for workflow rail width."));
        }

        var revision = project.RevisionHistory.Count + 1;
        var revisionId = $"system-scan-r{revision:000}";
        var spec = current with
        {
            Metadata = current.Metadata with { RevisionId = revisionId, ParentRevisionId = current.Metadata.RevisionId, CreatedAt = DateTimeOffset.UtcNow },
            ComponentTree = components,
            DesignRationale = current.DesignRationale.Concat(["Conversational revision applied with bounded semantic target resolution."]).ToArray()
        };
        var artifactId = $"mock-system-scan-r{revision:000}";
        var artifactPath = Path.Combine(store.ArtifactsRoot, $"{artifactId}.html");
        var evaluationPath = Path.Combine(store.ArtifactsRoot, $"{artifactId}.evaluation.json");
        var specPath = Path.Combine(store.SpecsRoot, $"{revisionId}.screen-spec.json");
        var semanticTreePath = Path.Combine(store.SpecsRoot, $"{revisionId}.semantic-tree.json");
        var html = RenderSystemScanHtml(project, spec);
        await File.WriteAllTextAsync(artifactPath, html, Encoding.UTF8, cancellationToken);
        var evaluation = Evaluate(artifactId, revision, html, project);
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
        await File.WriteAllTextAsync(specPath, JsonSerializer.Serialize(spec, json), cancellationToken);
        await File.WriteAllTextAsync(semanticTreePath, JsonSerializer.Serialize(spec.ComponentTree, json), cancellationToken);
        await File.WriteAllTextAsync(evaluationPath, JsonSerializer.Serialize(evaluation, json), cancellationToken);
        foreach (var existing in project.GeneratedArtifacts.Where(a => a.AcceptanceState == ArtifactAcceptanceState.Candidate).ToArray())
        {
            var i = project.GeneratedArtifacts.FindIndex(a => a.Id == existing.Id);
            project.GeneratedArtifacts[i] = existing with { AcceptanceState = ArtifactAcceptanceState.Superseded };
        }
        var artifact = new MockArtifact(artifactId, revision, current.Metadata.ScreenName, artifactPath, evaluationPath, ArtifactAcceptanceState.Candidate, DateTimeOffset.UtcNow);
        project.GeneratedArtifacts.Add(artifact);
        var preserved = project.DesignAuthority.SurfaceHierarchy.Concat(project.DesignAuthority.Typography).Concat(project.DesignAuthority.StateSemantics).Select(d => d.Id).ToArray();
        project.RevisionHistory.Add(new DesignRevision { Revision = revision, Summary = text, RequirementIds = project.Requirements.Select(r => r.Id).ToArray(), ArtifactIds = [artifactId], CreatedAt = DateTimeOffset.UtcNow, RevisionId = revisionId, ParentRevisionId = current.Metadata.RevisionId, OriginatingInstruction = text, Origin = DesignRevisionOrigin.Human, AffectedComponents = direct.Concat(collateral).Select(c => c.ComponentId).Distinct().ToArray(), DirectChanges = direct, CollateralChanges = collateral, PreservedDecisions = preserved, CriticFindingsBefore = [], CriticFindingsAfter = evaluation.Findings, DesignSpecSnapshot = spec, RenderArtifact = artifactPath, ScreenshotArtifact = string.Empty, SemanticTreeArtifact = semanticTreePath, AcceptanceState = DesignRevisionAcceptanceState.Candidate, Timestamp = DateTimeOffset.UtcNow });
        project = project with { CurrentScreenDesignSpec = spec };
        await store.SaveProjectAsync(project, cancellationToken);
        await SaveContextAsync(project, text, "revise_system_scan_design", null, null, cancellationToken).ConfigureAwait(false);
        return new SystemScanRevisionResult(project, artifact, evaluation, request, direct, collateral, preserved);
    }

    public async Task<(AppBuilderProject Project, MockArtifact Artifact, VisualEvaluation Evaluation)> GenerateSystemScanMockAsync(string projectId, string screenObjective, CancellationToken cancellationToken = default)
    {
        var project = await store.LoadProjectAsync(projectId, cancellationToken);
        var revision = project.RevisionHistory.Count + 1;
        var parentRevisionId = project.RevisionHistory.LastOrDefault()?.RevisionId;
        var revisionId = $"system-scan-r{revision:000}";
        var artifactId = $"mock-system-scan-r{revision:000}";
        var artifactPath = Path.Combine(store.ArtifactsRoot, $"{artifactId}.html");
        var evaluationPath = Path.Combine(store.ArtifactsRoot, $"{artifactId}.evaluation.json");
        var specPath = Path.Combine(store.SpecsRoot, $"{revisionId}.screen-spec.json");
        var semanticTreePath = Path.Combine(store.SpecsRoot, $"{revisionId}.semantic-tree.json");
        var spec = BuildSystemScanSpec(project, revisionId, parentRevisionId);
        var html = RenderSystemScanHtml(project, spec);
        await File.WriteAllTextAsync(artifactPath, html, Encoding.UTF8, cancellationToken);
        var evaluation = Evaluate(artifactId, revision, html, project);
        await File.WriteAllTextAsync(specPath, JsonSerializer.Serialize(spec, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }), cancellationToken);
        await File.WriteAllTextAsync(semanticTreePath, JsonSerializer.Serialize(spec.ComponentTree, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }), cancellationToken);
        await File.WriteAllTextAsync(evaluationPath, JsonSerializer.Serialize(evaluation, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }), cancellationToken);

        foreach (var existing in project.GeneratedArtifacts.Where(a => a.AcceptanceState == ArtifactAcceptanceState.Candidate).ToArray())
        {
            var i = project.GeneratedArtifacts.FindIndex(a => a.Id == existing.Id);
            project.GeneratedArtifacts[i] = existing with { AcceptanceState = ArtifactAcceptanceState.Superseded };
        }

        var artifact = new MockArtifact(artifactId, revision, screenObjective, artifactPath, evaluationPath, ArtifactAcceptanceState.Candidate, DateTimeOffset.UtcNow);
        project.GeneratedArtifacts.Add(artifact);
        project.RevisionHistory.Add(new DesignRevision
        {
            Revision = revision,
            Summary = screenObjective,
            RequirementIds = project.Requirements.Select(r => r.Id).ToArray(),
            ArtifactIds = [artifactId],
            CreatedAt = DateTimeOffset.UtcNow,
            RevisionId = revisionId,
            ParentRevisionId = parentRevisionId,
            OriginatingInstruction = screenObjective,
            Origin = DesignRevisionOrigin.Human,
            AffectedComponents = spec.ComponentTree.Select(c => c.SemanticId).ToArray(),
            DirectChanges = revision == 1
                ? spec.ComponentTree.Select(c => new DesignChange(c.SemanticId, "created", "absent", "present", "Initial System Scan candidate from ScreenDesignSpec.")).ToArray()
                : [],
            CollateralChanges = [],
            PreservedDecisions = project.DesignAuthority.SurfaceHierarchy
                .Concat(project.DesignAuthority.Typography)
                .Concat(project.DesignAuthority.Spacing)
                .Concat(project.DesignAuthority.Geometry)
                .Select(d => d.Id)
                .ToArray(),
            CriticFindingsBefore = [],
            CriticFindingsAfter = evaluation.Findings,
            DesignSpecSnapshot = spec,
            RenderArtifact = artifactPath,
            ScreenshotArtifact = string.Empty,
            SemanticTreeArtifact = semanticTreePath,
            AcceptanceState = DesignRevisionAcceptanceState.Candidate,
            Timestamp = DateTimeOffset.UtcNow
        });
        project = project with { CurrentScreenDesignSpec = spec };
        await store.SaveProjectAsync(project, cancellationToken);
        await SaveContextAsync(project, screenObjective, "design_system_scan_mock", null, null, cancellationToken).ConfigureAwait(false);
        return (project, artifact, evaluation);
    }

    public async Task<AppBuilderProject> MarkArtifactAsync(string projectId, string artifactId, ArtifactAcceptanceState state, bool humanConfirmed, CancellationToken cancellationToken = default)
    {
        if (state == ArtifactAcceptanceState.Accepted && !humanConfirmed)
        {
            throw new InvalidOperationException("Human confirmation is required before accepting a design artifact.");
        }

        var project = await store.LoadProjectAsync(projectId, cancellationToken);
        var index = project.GeneratedArtifacts.FindIndex(a => a.Id == artifactId);
        if (index < 0) throw new InvalidOperationException($"Artifact {artifactId} was not found.");
        project.GeneratedArtifacts[index] = project.GeneratedArtifacts[index] with { AcceptanceState = state };
        await store.SaveProjectAsync(project, cancellationToken);
        await SaveContextAsync(project, $"Artifact {artifactId} marked {state}.", "mark_builder_artifact", null, null, cancellationToken).ConfigureAwait(false);
        return project;
    }

    private static void UpdateOrAdd(AppBuilderProject project, string id, string area, string statement, bool applies)
    {
        if (!applies) return;
        var index = project.Requirements.FindIndex(r => r.Id == id);
        var req = new Requirement(id, RequirementClassification.Must, area, statement, "Conversational design revision delta.", DateTimeOffset.UtcNow);
        if (index >= 0) project.Requirements[index] = req;
        else project.Requirements.Add(req);
    }

    private static VisualEvaluation Evaluate(string artifactId, int revision, string html, AppBuilderProject project)
    {
        var tokens = project.DesignAuthority.ColorTokens.Where(kvp => html.Contains(kvp.Value, StringComparison.OrdinalIgnoreCase)).Select(kvp => kvp.Key).ToArray();
        var components = new[] { "workflow-nav", "diagnostic-strategy", "network-topology", "scan-controls", "module-evidence", "acceptance-controls" }
            .Where(html.Contains).ToArray();
        var findings = new List<string>
        {
            tokens.Length >= 6 ? "ALEXIS color tokens are present." : "Some ALEXIS color tokens are missing.",
            components.Length == 6 ? "Required System Scan components are present." : "One or more required components are missing.",
            "Static HTML artifact rendered for 1440x900 viewport contract."
        };
        return new VisualEvaluation(artifactId, revision, 1440, 900, false, false, components, tokens, findings, ["Semantic multimodal visual inspection is not implemented in Phase 1."]);
    }

    private static ScreenDesignSpec BuildSystemScanSpec(AppBuilderProject project, string revisionId, string? parentRevisionId)
    {
        var metadata = new ScreenDesignMetadata(
            project.Id,
            "alexis.systemScan",
            "ALEXIS System Scan",
            revisionId,
            "ALEXIS Builder Brain V1",
            "builder-brain-v1.foundation",
            DateTimeOffset.UtcNow,
            "Jarvis.AppBuilder",
            parentRevisionId);

        var context = new ContextModel(
            OperatingContext.WorkshopStationary,
            "workshop",
            "simulated vehicle context",
            "desktop voice plus visual preview",
            "normal diagnostic workflow",
            "Windows workstation",
            SafetyState.Safe,
            MotionState.Stationary,
            IgnitionState.On,
            ConnectionState.Degraded,
            ScanState.Complete,
            [DiagnosticState.ActiveFaults, DiagnosticState.CommunicationFaults, DiagnosticState.EvidenceAvailable],
            DataState.Current,
            OperationState.Ready,
            SafetyState.Caution);

        var components = new[]
        {
            Component("alexis.systemScan.vehicleContext", "contextPanel", "Preserve vehicle/session context and simulated-data boundary.", null, ["alexis.systemScan.scanProgress"]),
            Component("alexis.systemScan.scanProgress", "statusRegion", "Expose scan completion separately from vehicle health.", "alexis.systemScan.vehicleContext", []),
            Component("alexis.systemScan.moduleNavigator", "workflowNavigation", "Keep System Scan inside the diagnostic workflow sequence.", null, [], new Dictionary<string, string> { ["widthPx"] = "270" }),
            Component("alexis.systemScan.networkTopology", "topologyVisualization", "Primary topology-first scan surface for module communication state.", null, [], new Dictionary<string, string> { ["allocation"] = "1.2" }),
            Component("alexis.systemScan.findingsSummary", "diagnosticStrategy", "Summarize scan scope, priority and evidence mode.", null, []),
            Component("alexis.systemScan.evidenceInspector", "moduleEvidence", "Show module evidence without inventing verified diagnostic data.", null, [], new Dictionary<string, string> { ["allocation"] = "0.8" }),
            Component("alexis.systemScan.primaryActions", "actionBar", "Expose scan and acceptance actions with candidate-only semantics.", null, [])
        };

        return new ScreenDesignSpec(
            metadata,
            context,
            new UserModel(UserRole.Technician, "advanced diagnostic technician", ["ECU", "DTC", "gateway", "topology", "communication fault"], ["dense workstation", "evidence-first"], []),
            new TaskModel(PrimaryTask.Scan, "Inspect System Scan status and module communication evidence.", "Identify failed communication boundaries before drilling into faults.", ["retry failed modules", "open report", "inspect topology"], "Candidate screen communicates scan result without implying vehicle health.", ["module response state", "active DTC count", "communication failure count", "scan state"], ["voice interruption", "manual stop"], ["retry failed modules", "return to workflow navigation"]),
            new DomainModel("ALEXIS_SYSTEM_SCAN_001", new Dictionary<string, string>
            {
                ["supportedModules"] = "34",
                ["respondingModules"] = "32",
                ["activeDTCs"] = "4",
                ["communicationFailures"] = "2",
                ["scanState"] = "complete",
                ["connectionCoverage"] = "partial"
            }, ["vehicleHealthy"], ["moduleFiltering", "topologyNavigation", "faultDrilldown", "retryFailedModules", "report", "comparison"], Enum.GetValues<NormativeDataSemantic>()),
            new InformationArchitecture(
                ["network topology", "scan state", "communication boundaries"],
                ["diagnostic strategy", "scan controls"],
                ["module evidence", "simulated fixture labels"],
                ["workflow navigation", "acceptance controls"],
                ["progressive drilldown for module details"],
                ["vehicle context", "workflow position"],
                ["DTC Results", "Freeze Frame", "Live Data"]),
            new VisualHierarchy(
                "alexis.systemScan.networkTopology",
                ["alexis.systemScan.findingsSummary", "alexis.systemScan.primaryActions"],
                ["alexis.systemScan.moduleNavigator", "alexis.systemScan.evidenceInspector"],
                ["communication failure distinct from active fault"],
                ["moduleNavigator", "vehicleContext", "networkTopology", "findingsSummary", "primaryActions", "evidenceInspector"],
                "System Scan is topology-first; completion must not imply health."),
            new LayoutModel(
                "1440x900 diagnostic workstation preview",
                "left workflow rail plus main topology-and-strategy workspace",
                [
                    new("workflow", "persistent diagnostic sequence", ["alexis.systemScan.moduleNavigator"]),
                    new("main", "topology-first scan analysis", ["alexis.systemScan.vehicleContext", "alexis.systemScan.networkTopology", "alexis.systemScan.findingsSummary", "alexis.systemScan.primaryActions", "alexis.systemScan.evidenceInspector"])
                ],
                ["no critical overflow", "stable topology region", "candidate state visible"],
                ["main topology grows before secondary panels"],
                "1180x720 desktop shell preview",
                "expertDense",
                "structured left-to-right diagnostic scan",
                "topology-dominant",
                "clip-free fixed mock preview"),
            components,
            new StateModel(new Dictionary<string, string>
            {
                ["alexis.systemScan.scanProgress"] = ScanState.Complete.ToString(),
                ["alexis.systemScan.networkTopology"] = ConnectionState.Degraded.ToString(),
                ["alexis.systemScan.evidenceInspector"] = DataState.Current.ToString(),
                ["alexis.systemScan.primaryActions"] = OperationState.Ready.ToString()
            }, Enum.GetValues<NormativeDataSemantic>()),
            [
                new("alexis.systemScan.startScan", "Start scan", "lowRisk", ["vehicle stationary"], ["diagnostic session"], "available", "availableWithWarning", "blocked when vehicle state unsafe", "unsupported when vehicle does not support scan", [], "running scan", "abort available", "scan complete", "scan failed", ["retry", "inspect failures"], ["scan report evidence"]),
                new("alexis.systemScan.abortScan", "Abort scan", "controlled", ["scan running"], ["operator intent"], "available during scan", "warn about partial evidence", "blocked when idle", "unsupported when no active scan", ["confirm if evidence loss possible"], "cancelling", "safe cancellation", "scan cancelled", "abort failed", ["resume or restart"], ["partial scan evidence"])
            ],
            new InteractionModel(["select topology node", "select module evidence"], ["tab through actions", "enter activates focused safe action"], ["candidate must be accepted or rejected explicitly"]),
            new VisualizationModel("ALEXIS_SYSTEM_SCAN_001 fixture", Enum.GetValues<NormativeDataSemantic>(), "Find communication boundaries and scan evidence.", "Topology communicates relationships; status text carries truth.", "compare responding vs supported modules", "select module or fault group", "scan completion snapshot", ["count", "state"], "current fixture snapshot", ["communication failure", "active DTC"], ["do not encode critical state by color alone"]),
            project.DesignAuthority.ColorTokens.Keys.ToArray(),
            ["critical state not color-only", "compact readable labels", "keyboard reachable actions"],
            ["do not imply vehicle healthy", "candidate cannot be auto-accepted", "unsafe context would block risky actions"],
            ["scan state", "module response evidence", "communication failure count", "simulated data label"],
            ["all required semantic components present", "scan completion distinct from health", "communication failure distinct from active fault", "candidate state visible"],
            ["Builder Brain V1 requires design reasoning before render.", "ALEXIS System Scan fixture is topology-first.", "Existing visual mock is preserved while moving design intent into ScreenDesignSpec."],
            [
                new("alexis.systemScan.screenTitle", "screen title", "Segoe UI Variable Display", 31, "650", 38, "textPrimary", "Dense executive diagnostic title, not hero scale."),
                new("alexis.systemScan.moduleNavigator", "navigation label", "Segoe UI", 13, "500", 18, "mutedText", "Compact left rail labels."),
                new("alexis.systemScan.networkTopology", "node label", "Segoe UI", 15, "650", 20, "textPrimary", "Topology nodes must remain readable at 16:9 fit."),
                new("alexis.systemScan.evidenceInspector", "evidence text", "Segoe UI", 13, "400", 18, "textPrimary", "Evidence density supports repeated technical scanning.")
            ],
            [
                new("alexis.systemScan.moduleNavigator", "left navigator", "270px", "fill", "fixed-fit", "collapsible rail keeps canvas dominant.", "Preserve workflow without stealing canvas."),
                new("alexis.systemScan.networkTopology", "center canvas", "1.2fr", "520px", "fit-16:9", "dominant responsive preview region.", "Topology is primary decision surface."),
                new("alexis.systemScan.evidenceInspector", "right inspector", ".8fr", "auto", "collapsible-inspector", "Secondary detail stays available without clipping.", "Inspector supports drilldown and selection evidence.")
            ]);
    }

    private static ComponentSpec Component(string semanticId, string componentType, string purpose, string? parent, IReadOnlyList<string> children, IReadOnlyDictionary<string, string>? properties = null) =>
        new(semanticId, componentType, purpose, parent, children, properties ?? new Dictionary<string, string>(), [], [], [], [], "BuilderBrainV1");

    private static string RenderSystemScanHtml(AppBuilderProject project, ScreenDesignSpec spec)
    {
        var darker = project.Requirements.Any(r => r.Id == "REQ-ALEXIS-SYSTEMSCAN-DARKER");
        var largerTopology = project.Requirements.Any(r => r.Id == "REQ-ALEXIS-SYSTEMSCAN-TOPOLOGY-SCALE");
        var module = spec.ComponentTree.Single(c => c.SemanticId == "alexis.systemScan.moduleNavigator");
        var topologyComponent = spec.ComponentTree.Single(c => c.SemanticId == "alexis.systemScan.networkTopology");
        var evidence = spec.ComponentTree.Single(c => c.SemanticId == "alexis.systemScan.evidenceInspector");
        var mutedBorders = project.Requirements.Any(r => r.Id == "REQ-ALEXIS-SYSTEMSCAN-BORDERS");
        var bg = darker ? "#02050D" : project.DesignAuthority.ColorTokens["pageBackground"];
        var surface = project.DesignAuthority.ColorTokens["surfaceElevated"];
        var border = mutedBorders ? "rgba(125,140,165,.18)" : "rgba(47,124,255,.38)";
        var left = module.Properties.GetValueOrDefault("widthPx", "270") + "px";
        var topology = topologyComponent.Properties.GetValueOrDefault("allocation", largerTopology ? "1.65" : "1.2") + "fr";
        var strategy = evidence.Properties.GetValueOrDefault("allocation", ".8") + "fr";
        var revision = spec.Metadata.RevisionId.Split('-').Last().TrimStart('r', 'R');
        return $$"""
<!doctype html><html><head><meta charset="utf-8"><title>ALEXIS System Scan R{{revision}}</title>
<style>
*{box-sizing:border-box} body{margin:0;background:{{bg}};color:#F5F8FF;font-family:'Segoe UI',Arial,sans-serif;width:1440px;height:900px;overflow:hidden}
.shell{display:grid;grid-template-columns:{{left}} 1fr;gap:18px;padding:22px;height:900px}.panel{background:{{surface}};border:1px solid {{border}};border-radius:6px;padding:18px}.label{font-size:11px;letter-spacing:0;color:#7D8CA5;text-transform:uppercase}.nav{display:grid;gap:10px}.nav div{padding:11px 12px;border-left:3px solid transparent;color:#7D8CA5}.nav .active{border-color:#2F7CFF;color:#F5F8FF;background:rgba(47,124,255,.10)}
 .main{display:grid;grid-template-rows:auto 1fr auto;gap:16px}.top{display:flex;justify-content:space-between;align-items:end}.title{font-size:31px;font-weight:650}.grid{display:grid;grid-template-columns:{{topology}} {{strategy}};gap:16px}.topology{position:relative;min-height:520px}.node{position:absolute;width:126px;padding:13px;border:1px solid {{border}};background:rgba(5,9,21,.82);border-radius:5px;text-align:center}.node strong{display:block;font-size:15px}.node span{color:#18CC6F;font-size:12px}.line{position:absolute;height:2px;background:#2F7CFF;opacity:.55;transform-origin:left}.strategy{display:grid;gap:12px}.metric{display:flex;justify-content:space-between;border-bottom:1px solid rgba(125,140,165,.18);padding-bottom:9px}.controls{display:flex;gap:10px}.button{border:1px solid {{border}};padding:11px 14px;border-radius:4px;background:rgba(47,124,255,.12)}.button.green{border-color:rgba(24,204,111,.45);background:rgba(24,204,111,.13)}.modules{display:grid;grid-template-columns:repeat(4,1fr);gap:10px}.module{background:rgba(5,9,21,.7);border:1px solid rgba(125,140,165,.18);border-radius:5px;padding:12px}.mock{color:#7D8CA5;font-size:12px}
</style></head><body><div class="shell">
<aside class="panel" id="workflow-nav" data-semantic-id="alexis.systemScan.moduleNavigator"><div class="label">ALEXIS WORKFLOW</div><div class="nav"><div>Vehicle Identity</div><div class="active">System Scan</div><div>DTC Results</div><div>Freeze Frame</div><div>Live Data</div><div>Test Plan</div></div></aside>
<main class="main"><section class="top" data-semantic-id="alexis.systemScan.vehicleContext"><div><div class="label">SIMULATED UI MOCK - NOT VERIFIED VEHICLE DATA</div><div class="title">System Scan</div></div><div class="label" data-semantic-id="alexis.systemScan.scanProgress">Revision {{revision}} / Candidate</div></section>
<section class="grid"><div class="panel topology" id="network-topology" data-semantic-id="alexis.systemScan.networkTopology"><div class="label">NETWORK TOPOLOGY</div><div class="node" style="left:44%;top:42%"><strong>Gateway</strong><span>Simulated online</span></div><div class="node" style="left:10%;top:18%"><strong>Powertrain</strong><span>Mock group</span></div><div class="node" style="left:67%;top:16%"><strong>Chassis</strong><span>Mock group</span></div><div class="node" style="left:14%;top:68%"><strong>Body</strong><span>Mock group</span></div><div class="node" style="left:70%;top:66%"><strong>Infotainment</strong><span>Mock group</span></div><div class="line" style="left:25%;top:31%;width:285px;transform:rotate(18deg)"></div><div class="line" style="left:53%;top:43%;width:245px;transform:rotate(-28deg)"></div><div class="line" style="left:25%;top:72%;width:300px;transform:rotate(-17deg)"></div><div class="line" style="left:54%;top:57%;width:250px;transform:rotate(25deg)"></div></div>
<div class="strategy"><div class="panel" id="diagnostic-strategy" data-semantic-id="alexis.systemScan.findingsSummary"><div class="label">DIAGNOSTIC STRATEGY</div><div class="metric"><span>Scan scope</span><strong>Full network</strong></div><div class="metric"><span>Priority</span><strong>Gateway first</strong></div><div class="metric"><span>Evidence mode</span><strong>Simulated preview</strong></div></div><div class="panel" id="scan-controls" data-semantic-id="alexis.systemScan.primaryActions"><div class="label">SCAN CONTROLS</div><div class="controls"><div class="button green">Start Scan</div><div class="button">Pause</div><div class="button">Abort</div></div></div><div class="panel" id="acceptance-controls"><div class="label">DESIGN ACCEPTANCE</div><div class="mock">Candidate only. Leon must explicitly accept or reject.</div></div></div></section>
<section class="modules" id="module-evidence" data-semantic-id="alexis.systemScan.evidenceInspector"><div class="module"><div class="label">ECM</div>Simulated reachable</div><div class="module"><div class="label">ABS</div>Simulated reachable</div><div class="module"><div class="label">BCM</div>Simulated pending</div><div class="module"><div class="label">TCU</div>Simulated reachable</div></section></main></div>
<script>document.addEventListener('click',e=>{const n=e.target.closest('[data-semantic-id]'); if(n&&window.chrome&&window.chrome.webview) window.chrome.webview.postMessage({type:'builder.selection',semanticId:n.dataset.semanticId});});</script>
</body></html>
""";
    }
}
