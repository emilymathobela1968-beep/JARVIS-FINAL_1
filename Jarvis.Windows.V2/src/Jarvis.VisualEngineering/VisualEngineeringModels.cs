using Jarvis.MissionIntelligence;

namespace Jarvis.VisualEngineering;

public enum VisualInspectionMode { RenderedApplication, ScreenshotSet }
public enum VisualEngineeringStage { InspectRenderedUi, CompareAuthority, IdentifyDefects, ImplementWithDeveloper, BuildPreview, CaptureRenderedResult, ReviewRenderedResult, PresentCandidate }

public sealed record VisualEngineeringRequest(
    Guid TaskId,
    string ApprovedWorkspace,
    string TargetSurface,
    string Objective,
    ProjectAuthority Authority,
    VisualInspectionMode InspectionMode,
    int IterationLimit);

public sealed record VisualDefect(string Dimension, string Evidence, string Recommendation);

public sealed record VisualEngineeringPlan(
    Guid TaskId,
    IReadOnlyList<VisualEngineeringStage> Stages,
    IReadOnlyList<string> RequiredEvidence,
    IReadOnlyList<VisualDefect> ExpectedReviewDimensions,
    string DeveloperObjective);
