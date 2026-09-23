using Jarvis.Developer;

namespace Jarvis.VisualEngineering;

public sealed class VisualEngineeringPlanner
{
    public VisualEngineeringPlan CreatePlan(VisualEngineeringRequest request)
    {
        request.Authority.Validate();
        if (string.IsNullOrWhiteSpace(request.ApprovedWorkspace)) throw new InvalidOperationException("Approved workspace is required.");
        if (string.IsNullOrWhiteSpace(request.TargetSurface)) throw new InvalidOperationException("Target surface is required.");
        if (request.IterationLimit < 1 || request.IterationLimit > 3) throw new InvalidOperationException("Visual Engineering iteration limit must be 1-3.");

        var objective = $"""
            Visual Engineering task for {request.Authority.ProductName}.
            Target surface: {request.TargetSurface}
            Objective: {request.Objective}
            Preserve functionality. Use rendered evidence, authority rules, build evidence, and captured result evidence before claiming completion.
            Required authority rules: {string.Join("; ", request.Authority.Rules.Where(rule => rule.Severity == MissionIntelligence.AuthoritySeverity.Required).Select(rule => $"{rule.Id}: {rule.Description}"))}
            """;

        return new(
            request.TaskId,
            [
                VisualEngineeringStage.InspectRenderedUi,
                VisualEngineeringStage.CompareAuthority,
                VisualEngineeringStage.IdentifyDefects,
                VisualEngineeringStage.ImplementWithDeveloper,
                VisualEngineeringStage.BuildPreview,
                VisualEngineeringStage.CaptureRenderedResult,
                VisualEngineeringStage.ReviewRenderedResult,
                VisualEngineeringStage.PresentCandidate
            ],
            [
                "current_render_capture",
                "authority_rule_comparison",
                "source_change_summary",
                "build_result",
                "preview_render_capture",
                "visual_review_result"
            ],
            [
                new("composition", "Rendered layout must be evaluated, not inferred from source.", "Check balance, focal hierarchy, and use of the 16:9 canvas."),
                new("alignment", "Misaligned controls or inconsistent gutters reduce authority.", "Check grid rhythm and edge alignment."),
                new("hierarchy", "Operational screens must scan quickly.", "Check primary diagnostic signal, secondary details, and action clarity."),
                new("readability", "Text must remain legible at desktop capture size.", "Check contrast, truncation, density, and label clarity."),
                new("asset fidelity", "Vehicle/product imagery must not be cropped or degraded.", "Check recognizability and brand fit."),
                new("responsiveness", "The candidate must preserve the intended canvas and avoid accidental scroll.", "Check target viewport behavior.")
            ],
            objective);
    }

    public DeveloperTaskRequest ToDeveloperTask(VisualEngineeringRequest request, VisualEngineeringPlan plan) => new(
        request.TaskId,
        request.ApprovedWorkspace,
        plan.DeveloperObjective,
        new HashSet<DeveloperOperationClass>
        {
            DeveloperOperationClass.Inspect,
            DeveloperOperationClass.Search,
            DeveloperOperationClass.EditSource,
            DeveloperOperationClass.Build,
            DeveloperOperationClass.Test
        },
        [
            "no credential changes",
            "no security changes",
            "no deployment or publishing",
            "no Jarvis governance changes",
            "no functional workflow changes unless explicitly authorized",
            "no changes outside the approved workspace"
        ],
        Math.Min(request.IterationLimit, 2),
        TimeSpan.FromMinutes(20));
}
