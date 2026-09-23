using Jarvis.Governance;
using Jarvis.Developer;

namespace Jarvis.AppBuilder;

public sealed class AppBuilderDesignSystemScanMockTool(
    AppBuilderService appBuilder,
    Action<AppBuilderMockEvidence>? rendered = null) : IGovernedTool
{
    public const string Name = "design_system_scan_mock";

    public GovernedToolDefinition Definition { get; } = new(
        Name,
        "Create or load the ALEXIS App Builder project, render the existing System Scan mock, and return candidate artifact evidence.",
        ["objective"]);

    public async Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var objective = request.Arguments.GetProperty("objective").GetString();
        if (string.IsNullOrWhiteSpace(objective))
        {
            return new(
                request.ConversationId,
                request.CorrelationId,
                request.Name,
                GovernedToolStatus.Rejected,
                "App Builder objective is required.",
                null,
                "objective_required");
        }

        var project = await appBuilder.CreateOrLoadAlexisProjectAsync(cancellationToken).ConfigureAwait(false);
        var result = await appBuilder.GenerateSystemScanMockAsync(project.Id, objective.Trim(), cancellationToken).ConfigureAwait(false);
        var evidence = new AppBuilderMockEvidence(
            result.Project.Id,
            result.Artifact.Id,
            result.Artifact.Revision,
            result.Artifact.ArtifactPath,
            result.Artifact.EvaluationPath,
            result.Artifact.AcceptanceState.ToString(),
            result.Artifact.ScreenObjective,
            result.Evaluation.Findings,
            result.Evaluation.Boundaries);

        rendered?.Invoke(evidence);
        return new(
            request.ConversationId,
            request.CorrelationId,
            request.Name,
            GovernedToolStatus.Succeeded,
            $"ALEXIS System Scan mock rendered as {evidence.ArtifactId}, revision {evidence.Revision}, state {evidence.AcceptanceState}.",
            evidence);
    }
}

public sealed class AppBuilderSystemScanRevisionTool(
    AppBuilderService appBuilder,
    Action<AppBuilderMockEvidence>? rendered = null) : IGovernedTool
{
    public const string Name = "revise_system_scan_design";

    public GovernedToolDefinition Definition { get; } = new(
        Name,
        "Apply a bounded conversational revision to the active ALEXIS System Scan candidate and rerender it.",
        ["objective"]);

    public async Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var objective = request.Arguments.GetProperty("objective").GetString();
        if (string.IsNullOrWhiteSpace(objective))
            return new(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Rejected, "Revision objective is required.", null, "objective_required");

        try
        {
            var project = await appBuilder.CreateOrLoadAlexisProjectAsync(cancellationToken).ConfigureAwait(false);
            var result = await appBuilder.ReviseSystemScanAsync(project.Id, objective.Trim(), cancellationToken).ConfigureAwait(false);
            var evidence = new AppBuilderMockEvidence(result.Project.Id, result.Artifact.Id, result.Artifact.Revision, result.Artifact.ArtifactPath, result.Artifact.EvaluationPath, result.Artifact.AcceptanceState.ToString(), result.Artifact.ScreenObjective, result.Evaluation.Findings, result.Evaluation.Boundaries);
            rendered?.Invoke(evidence);
            return new(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Succeeded,
                $"System Scan revision {result.Artifact.Revision} rendered. Target {result.Request.TargetComponentId}, property {result.Request.TargetProperty}. Direct: {string.Join(", ", result.DirectChanges.Select(c => $"{c.Property} {c.Before}->{c.After}"))}. Collateral: {result.CollateralChanges.Count}. Preserved: {result.PreservedDecisions.Count}.", result);
        }
        catch (InvalidOperationException ex)
        {
            return new(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Rejected, ex.Message, null, "clarification_required");
        }
    }
}

public sealed class GetBuilderCapabilityInventoryTool(AppBuilderService appBuilder) : IGovernedTool
{
    public const string Name = "get_builder_capability_inventory";
    public GovernedToolDefinition Definition { get; } = new(Name, "Read the truthful Builder capability inventory with AVAILABLE, PARTIAL, BLOCKED, and NOT_IMPLEMENTED states.", []);

    public Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var inventory = appBuilder.GetCapabilityInventory();
        var blocked = inventory.Capabilities.Count(c => c.State is BuilderCapabilityState.Blocked or BuilderCapabilityState.NotImplemented);
        return Task.FromResult(new GovernedToolResult(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Succeeded,
            $"Builder inventory returned: {inventory.Capabilities.Count} capabilities, {blocked} blocked or not implemented.", inventory));
    }
}

public sealed class GetBuilderProjectContextTool(AppBuilderService appBuilder) : IGovernedTool
{
    public const string Name = "get_builder_project_context";
    public GovernedToolDefinition Definition { get; } = new(Name, "Read compact durable Builder project context: project, objective, screen, revision, acceptance, pending action, blockers and semantic components.", []);

    public async Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var context = await appBuilder.LoadLatestContextAsync(cancellationToken).ConfigureAwait(false);
        var project = await appBuilder.LoadLatestActiveProjectAsync(cancellationToken).ConfigureAwait(false);
        var bridge = appBuilder.BuildRealtimeBridge(project, context);
        return new(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Succeeded,
            $"Builder context: {bridge.Project ?? "none"}, revision {bridge.Revision ?? "none"}, acceptance {bridge.Acceptance ?? "none"}.", bridge);
    }
}

public sealed class SelectBuilderComponentTool(AppBuilderService appBuilder) : IGovernedTool
{
    public const string Name = "select_builder_component";
    public GovernedToolDefinition Definition { get; } = new(Name, "Record active Builder semantic component selection for inspect-mode safe revisions.", ["project_id", "semantic_id"]);

    public async Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var projectId = request.Arguments.GetProperty("project_id").GetString();
        var semanticId = request.Arguments.GetProperty("semantic_id").GetString();
        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(semanticId))
            return new(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Rejected, "Project id and semantic id are required.", null, "selection_arguments_required");
        try
        {
            var selection = await appBuilder.SelectComponentAsync(projectId, semanticId, cancellationToken).ConfigureAwait(false);
            return new(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Succeeded,
                $"Selected {selection.SemanticId} ({selection.ComponentType}) for safe inspection.", selection);
        }
        catch (InvalidOperationException ex)
        {
            return new(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Rejected, ex.Message, null, "selection_not_found");
        }
    }
}

public sealed class SubmitBuilderImplementationTaskTool(AppBuilderService appBuilder, IBuilderDeveloperTaskRunner developer) : IGovernedTool
{
    public const string Name = "submit_builder_implementation_task";
    public GovernedToolDefinition Definition { get; } = new(Name, "Submit a bounded Builder-to-Developer implementation task from an accepted spec and approved workspace. Does not deploy.", ["project_id", "approved_workspace", "bounded_goal"]);

    public async Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var projectId = request.Arguments.GetProperty("project_id").GetString();
        var workspace = request.Arguments.GetProperty("approved_workspace").GetString();
        var goal = request.Arguments.GetProperty("bounded_goal").GetString();
        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(workspace) || string.IsNullOrWhiteSpace(goal))
            return new(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Rejected, "Project id, approved workspace, and bounded goal are required.", null, "handoff_arguments_required");
        try
        {
            var evidence = await appBuilder.SubmitImplementationTaskAsync(projectId, workspace, goal, developer, cancellationToken).ConfigureAwait(false);
            return new(request.ConversationId, request.CorrelationId, request.Name,
                evidence.Status == DeveloperTaskStatus.PolicyRejected.ToString() ? GovernedToolStatus.Rejected : GovernedToolStatus.Succeeded,
                $"Builder implementation handoff ended: {evidence.Status}. State: {evidence.State}.", evidence, evidence.FailureReason);
        }
        catch (InvalidOperationException ex)
        {
            return new(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Rejected, ex.Message, null, "handoff_precondition_failed");
        }
    }
}

public sealed class BuilderDeveloperTaskRunnerAdapter(DeveloperTaskOrchestrator orchestrator) : IBuilderDeveloperTaskRunner
{
    public Task<DeveloperTaskEvidence> RunAsync(DeveloperTaskRequest request, CancellationToken cancellationToken = default) =>
        orchestrator.RunAsync(request, cancellationToken);
}
