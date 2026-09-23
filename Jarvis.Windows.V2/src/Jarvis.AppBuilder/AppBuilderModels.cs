using System.Text.Json.Serialization;

namespace Jarvis.AppBuilder;

public enum RequirementClassification { Must, Should, May, Prohibited, Unresolved, Accepted, Rejected, Deferred }
public enum ArtifactAcceptanceState { Candidate, Accepted, Rejected, Superseded }
public enum OperatingContext { InVehicleMoving, StationaryUnsecured, WorkshopStationary }
public enum UserRole { Driver, Technician, Engineer, ServiceAdvisor }
public enum PrimaryTask { IdentifyVehicle, Scan, InvestigateFault, InspectTopology, InspectFreezeFrame, LiveData, ActuatorTest, ServiceFunction, Coding, Programming, GuidedDiagnosis, Verification }
public enum MotionState { Unknown, Stationary, Moving }
public enum IgnitionState { Unknown, Off, Accessory, On, EngineRunning }
public enum ConnectionState { Disconnected, Connecting, Connected, Degraded, TimedOut, Unsupported }
public enum ScanState { Idle, Running, Paused, Cancelled, Partial, Complete, Failed }
public enum DiagnosticState { NoFaults, ActiveFaults, HistoricalFaults, PendingFaults, CommunicationFaults, EvidenceAvailable }
public enum DataState { Current, Stale, Unavailable, Unsupported, Missing, Invalid, Failed, Unknown }
public enum OperationState { Idle, Ready, Blocked, AwaitingConfirmation, Running, Paused, Completed, Failed, Cancelled, Recoverable }
public enum SafetyState { Unknown, Safe, Caution, Blocked, Unsafe }
public enum NormativeDataSemantic { Zero, Missing, Unavailable, Unsupported, Stale, Invalid, Failed, Unknown }
public enum DesignRevisionOrigin { Human, Critic, SystemConstraint }
public enum DesignRevisionAcceptanceState { Draft, Candidate, CriticRejected, CriticPassed, HumanRejected, Accepted, Superseded, Locked }
public enum BuilderCapabilityState { Available, Partial, Blocked, NotImplemented }
public enum BuilderConversationIntent { Information, DesignDiscussion, DelegatedObjective, Approval, Rejection, Clarification }
public enum BuilderInitiativeActionKind { Answer, ContinueExistingObjective, RenderCandidate, ReviseSelection, AskClarification, StopUnsupported }
public enum BuilderDeliveryState { Planned, Designed, Rendered, Implemented, Built, Tested, PhysicalAccepted }

public sealed record ProductAuthority(string Purpose, string TargetUsers, string Domain, string Platform);

public sealed record Requirement(
    string Id,
    RequirementClassification Classification,
    string Area,
    string Statement,
    string Rationale,
    DateTimeOffset UpdatedAt);

public sealed record DesignDecision(
    string Id,
    string Area,
    string Value,
    string Rationale,
    bool Accepted);

public sealed record DesignAuthority(
    string VisualDirection,
    IReadOnlyDictionary<string, string> ColorTokens,
    IReadOnlyList<DesignDecision> SurfaceHierarchy,
    IReadOnlyList<DesignDecision> Typography,
    IReadOnlyList<DesignDecision> Spacing,
    IReadOnlyList<DesignDecision> Geometry,
    IReadOnlyList<DesignDecision> Density,
    IReadOnlyList<DesignDecision> Navigation,
    IReadOnlyList<DesignDecision> InformationHierarchy,
    IReadOnlyList<DesignDecision> InteractionBehaviour,
    IReadOnlyList<DesignDecision> StateSemantics,
    IReadOnlyList<DesignDecision> AnimationGuidance,
    IReadOnlyList<DesignDecision> IconographyGuidance,
    IReadOnlyList<string> ReferenceArtifacts,
    IReadOnlyList<string> AcceptedExamples,
    IReadOnlyList<string> RejectedExamples,
    IReadOnlyDictionary<string, IReadOnlyList<DesignDecision>> PageSpecificExceptions);

public sealed record MockArtifact(
    string Id,
    int Revision,
    string ScreenObjective,
    string ArtifactPath,
    string EvaluationPath,
    ArtifactAcceptanceState AcceptanceState,
    DateTimeOffset CreatedAt);

public sealed record VisualEvaluation(
    string ArtifactId,
    int Revision,
    int ViewportWidth,
    int ViewportHeight,
    bool OverflowDetected,
    bool ClippingRiskDetected,
    IReadOnlyList<string> RequiredComponentsPresent,
    IReadOnlyList<string> TokenUsage,
    IReadOnlyList<string> Findings,
    IReadOnlyList<string> Boundaries);

public sealed record ScreenDesignMetadata(
    string ProjectId,
    string ScreenId,
    string ScreenName,
    string RevisionId,
    string DesignProfile,
    string SchemaVersion,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    string? ParentRevisionId);

public sealed record ContextModel(
    OperatingContext OperatingContext,
    string Environment,
    string VehicleContext,
    string InteractionMode,
    string TimePressure,
    string HardwareContext,
    SafetyState SafetyContext,
    MotionState MotionState,
    IgnitionState IgnitionState,
    ConnectionState ConnectionState,
    ScanState ScanState,
    IReadOnlyList<DiagnosticState> DiagnosticStates,
    DataState DataState,
    OperationState OperationState,
    SafetyState SafetyState);

public sealed record UserModel(UserRole Role, string Expertise, IReadOnlyList<string> ExpectedTerminology, IReadOnlyList<string> InteractionPreferences, IReadOnlyList<string> AccessibilityNeeds);
public sealed record TaskModel(PrimaryTask PrimaryTask, string UserGoal, string PrimaryDecision, IReadOnlyList<string> SecondaryDecisions, string CompletionCondition, IReadOnlyList<string> EvidenceNeeded, IReadOnlyList<string> AllowedInterruptions, IReadOnlyList<string> RecoveryExpectations);
public sealed record DomainModel(string FixtureId, IReadOnlyDictionary<string, string> Facts, IReadOnlyList<string> ProhibitedConclusions, IReadOnlyList<string> RequiredCapabilities, IReadOnlyList<NormativeDataSemantic> DataSemantics);
public sealed record InformationArchitecture(IReadOnlyList<string> PrimaryInformation, IReadOnlyList<string> SecondaryInformation, IReadOnlyList<string> SupportingEvidence, IReadOnlyList<string> TechnicalDetail, IReadOnlyList<string> DisclosureRules, IReadOnlyList<string> PersistentContext, IReadOnlyList<string> CrossScreenRelationships);
public sealed record VisualHierarchy(string PrimaryFocalRegion, IReadOnlyList<string> SecondaryFocalRegions, IReadOnlyList<string> LowPriorityRegions, IReadOnlyList<string> AlertPriority, IReadOnlyList<string> ReadingOrder, string EmphasisRationale);
public sealed record LayoutRegion(string RegionId, string Purpose, IReadOnlyList<string> ContainsComponentIds);
public sealed record LayoutModel(string ViewportIntent, string GridIntent, IReadOnlyList<LayoutRegion> Regions, IReadOnlyList<string> Constraints, IReadOnlyList<string> ResizingRules, string MinimumUsableSize, string Density, string Alignment, string Distribution, string OverflowPolicy);
public sealed record ComponentSpec(string SemanticId, string ComponentType, string Purpose, string? Parent, IReadOnlyList<string> Children, IReadOnlyDictionary<string, string> Properties, IReadOnlyList<string> Constraints, IReadOnlyList<string> Bindings, IReadOnlyList<string> States, IReadOnlyList<string> Interactions, string Provenance);
public sealed record TypographySpec(string SemanticId, string Role, string FontFamily, int SizePx, string Weight, int LineHeightPx, string ColorToken, string Rationale);
public sealed record LayoutAttributeSpec(string SemanticId, string Region, string Width, string Height, string FitMode, string ResponsiveRule, string Rationale);
public sealed record StateModel(IReadOnlyDictionary<string, string> ComponentStates, IReadOnlyList<NormativeDataSemantic> DataSemantics);
public sealed record ActionDesignSpec(string ActionId, string Purpose, string RiskClass, IReadOnlyList<string> Preconditions, IReadOnlyList<string> Permissions, string AvailableState, string WarningState, string BlockedState, string UnsupportedState, IReadOnlyList<string> ConfirmationRequirements, string ProgressState, string CancellationPolicy, string SuccessState, string FailureState, IReadOnlyList<string> RecoveryActions, IReadOnlyList<string> EvidenceImpact);
public sealed record InteractionModel(IReadOnlyList<string> SelectionModel, IReadOnlyList<string> KeyboardModel, IReadOnlyList<string> RevisionModel);
public sealed record VisualizationModel(string DataSource, IReadOnlyList<NormativeDataSemantic> DataSemantics, string TaskPurpose, string EncodingIntent, string ComparisonModel, string SelectionModel, string TimeModel, IReadOnlyList<string> Units, string Freshness, IReadOnlyList<string> Thresholds, IReadOnlyList<string> InteractionConstraints);

public sealed record ScreenDesignSpec(
    ScreenDesignMetadata Metadata,
    ContextModel ContextModel,
    UserModel UserModel,
    TaskModel TaskModel,
    DomainModel DomainModel,
    InformationArchitecture InformationArchitecture,
    VisualHierarchy VisualHierarchy,
    LayoutModel LayoutModel,
    IReadOnlyList<ComponentSpec> ComponentTree,
    StateModel StateModel,
    IReadOnlyList<ActionDesignSpec> ActionModel,
    InteractionModel InteractionModel,
    VisualizationModel VisualizationModel,
    IReadOnlyList<string> TokenReferences,
    IReadOnlyList<string> AccessibilityRequirements,
    IReadOnlyList<string> SafetyRequirements,
    IReadOnlyList<string> EvidenceRequirements,
    IReadOnlyList<string> AcceptanceCriteria,
    IReadOnlyList<string> DesignRationale,
    IReadOnlyList<TypographySpec>? Typography = null,
    IReadOnlyList<LayoutAttributeSpec>? LayoutAttributes = null);

public sealed record AppBuilderMockEvidence(
    string ProjectId,
    string ArtifactId,
    int Revision,
    string ArtifactPath,
    string EvaluationPath,
    string AcceptanceState,
    string ScreenObjective,
    IReadOnlyList<string> Findings,
    IReadOnlyList<string> Boundaries);

public sealed record DesignChange(string ComponentId, string Property, string Before, string After, string Rationale);

public sealed record SystemScanRevisionRequest(
    string TargetComponentId,
    string TargetProperty,
    string RequestedChange,
    string ReferenceResolution,
    string ConstraintResult,
    string OriginatingInstruction);

public sealed record SystemScanRevisionResult(
    AppBuilderProject Project,
    MockArtifact Artifact,
    VisualEvaluation Evaluation,
    SystemScanRevisionRequest Request,
    IReadOnlyList<DesignChange> DirectChanges,
    IReadOnlyList<DesignChange> CollateralChanges,
    IReadOnlyList<string> PreservedDecisions);

public sealed record BuilderCapability(
    string Id,
    string Name,
    BuilderCapabilityState State,
    string Reason = "",
    string Dependency = "");

public sealed record BuilderCapabilityInventory(
    string SchemaVersion,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<BuilderCapability> Capabilities);

public sealed record ActiveBuilderSelection(
    string SemanticId,
    string ComponentType,
    string? Parent,
    IReadOnlyDictionary<string, string> SafeEditableProperties,
    DateTimeOffset UpdatedAt);

public sealed record BuilderProjectContext(
    string? ActiveProjectId,
    string? ActiveProjectName,
    string Purpose,
    string Objective,
    string? ScreenId,
    string? ScreenName,
    string? RevisionId,
    string? AcceptanceState,
    string? ArtifactPath,
    IReadOnlyList<string> AcceptedDecisions,
    IReadOnlyList<string> UnresolvedDecisions,
    IReadOnlyList<string> ExplicitLeonRequirements,
    string CurrentTask,
    string? LastGovernedAction,
    string? PendingGovernedAction,
    IReadOnlyList<string> CapabilityBlockers,
    ActiveBuilderSelection? Selection,
    IReadOnlyList<BuilderDeliveryState> DeliveryStates,
    DateTimeOffset Timestamp);

public sealed record BuilderRealtimeContextBridge(
    string Workspace,
    string? Project,
    string Objective,
    string? Screen,
    string? Revision,
    string? Acceptance,
    string? PendingAction,
    IReadOnlyList<string> Blockers,
    IReadOnlyList<string> SemanticComponents,
    string? Selection);

public sealed record BuilderInitiativeDecision(
    BuilderConversationIntent Intent,
    BuilderInitiativeActionKind Action,
    string Rationale,
    string? GovernedToolName,
    IReadOnlyDictionary<string, string> Arguments,
    string? ClarifyingQuestion);

public sealed record BuilderImplementationTaskEvidence(
    string ProjectId,
    string ApprovedWorkspace,
    string AcceptedRevisionId,
    BuilderDeliveryState State,
    string Status,
    string Message,
    string? FailureReason);

public sealed class DesignRevision
{
    public DesignRevision() { }

    public DesignRevision(int revision, string summary, IReadOnlyList<string> requirementIds, IReadOnlyList<string> artifactIds, DateTimeOffset createdAt)
    {
        Revision = revision;
        Summary = summary;
        RequirementIds = requirementIds;
        ArtifactIds = artifactIds;
        CreatedAt = createdAt;
        RevisionId = $"legacy-r{revision:000}";
        OriginatingInstruction = summary;
        Timestamp = createdAt;
    }

    public int Revision { get; init; }
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> RequirementIds { get; init; } = [];
    public IReadOnlyList<string> ArtifactIds { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public string RevisionId { get; init; } = string.Empty;
    public string? ParentRevisionId { get; init; }
    public string OriginatingInstruction { get; init; } = string.Empty;
    public DesignRevisionOrigin Origin { get; init; } = DesignRevisionOrigin.Human;
    public IReadOnlyList<string> AffectedComponents { get; init; } = [];
    public IReadOnlyList<DesignChange> DirectChanges { get; init; } = [];
    public IReadOnlyList<DesignChange> CollateralChanges { get; init; } = [];
    public IReadOnlyList<string> PreservedDecisions { get; init; } = [];
    public IReadOnlyList<string> CriticFindingsBefore { get; init; } = [];
    public IReadOnlyList<string> CriticFindingsAfter { get; init; } = [];
    public ScreenDesignSpec? DesignSpecSnapshot { get; init; }
    public string RenderArtifact { get; init; } = string.Empty;
    public string ScreenshotArtifact { get; init; } = string.Empty;
    public string SemanticTreeArtifact { get; init; } = string.Empty;
    public DesignRevisionAcceptanceState AcceptanceState { get; init; } = DesignRevisionAcceptanceState.Candidate;
    public DateTimeOffset Timestamp { get; init; }
}

public sealed record AppBuilderProject
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ProductAuthority Product { get; init; }
    public required string CurrentPhase { get; init; }
    public required List<Requirement> Requirements { get; init; }
    public required DesignAuthority DesignAuthority { get; init; }
    public required List<DesignDecision> AcceptedDecisions { get; init; }
    public required List<DesignDecision> RejectedDecisions { get; init; }
    public required List<string> UnresolvedQuestions { get; init; }
    public required List<string> References { get; init; }
    public required List<MockArtifact> GeneratedArtifacts { get; init; }
    public required List<DesignRevision> RevisionHistory { get; init; }
    public required ArtifactAcceptanceState AcceptanceStatus { get; init; }
    public ScreenDesignSpec? CurrentScreenDesignSpec { get; init; }
}
