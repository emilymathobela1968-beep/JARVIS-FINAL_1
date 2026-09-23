using Jarvis.MissionIntelligence;

namespace Jarvis.Media;

public enum MediaOutputFormat { Youtube16x9Master }
public enum MediaRenderStatus { Succeeded, Failed, Rejected, MissingRenderer }

public sealed record MediaSourcePanel(int Index, string Path, string Title);
public sealed record MediaTimelineShot(int PanelIndex, TimeSpan Start, TimeSpan Duration, string Motion, string Title, string Narration);

public sealed record MediaProductionRequest(
    Guid ProductionId,
    ProjectAuthority Authority,
    IReadOnlyList<MediaSourcePanel> Panels,
    TimeSpan TargetDuration,
    MediaOutputFormat OutputFormat);

public sealed record MediaProductionPlan(
    Guid ProductionId,
    IReadOnlyList<MediaTimelineShot> Timeline,
    IReadOnlyList<string> RequiredQaChecks,
    TimeSpan TotalDuration,
    MediaOutputFormat OutputFormat);

public sealed record MediaProjectMetadata(string Title, string Subtitle, string Producer);

public sealed record MediaStudioRenderRequest(
    Guid MissionId,
    ProjectAuthority Authority,
    IReadOnlyList<MediaSourcePanel> Panels,
    TimeSpan TargetDuration,
    string OutputPath,
    MediaProjectMetadata Metadata,
    string? NarrationAudioPath,
    string? MusicAudioPath,
    string ApprovedInputRoot,
    string ApprovedOutputRoot);

public sealed record RendererTools(string FfmpegPath, string FfprobePath);

public sealed record RendererCommand(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    string DisplayCommand);

public sealed record RendererExecutionResult(
    int ExitCode,
    bool TimedOut,
    string StandardError,
    string OutputPath,
    long OutputFileSize);

public sealed record VideoProbeResult(
    string Path,
    TimeSpan Duration,
    int Width,
    int Height,
    double FrameRate,
    bool HasAudio);

public sealed record MediaQaResult(
    bool Accepted,
    IReadOnlyList<string> PassedChecks,
    IReadOnlyList<string> FailedChecks,
    IReadOnlyList<string> EvidenceFiles);

public sealed record MediaRenderEvidence(
    Guid MissionId,
    MediaRenderStatus Status,
    string OutputPath,
    RendererCommand? Command,
    RendererExecutionResult? Execution,
    VideoProbeResult? Probe,
    MediaQaResult? Qa,
    string? FailureReason);
