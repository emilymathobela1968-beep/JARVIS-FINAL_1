namespace Jarvis.Developer;

public enum DeveloperOperationClass { Inspect, Search, EditSource, Build, Test }
public enum DeveloperTaskStatus { Pending, Running, Succeeded, Failed, TimedOut, PolicyRejected }

public sealed record DeveloperTaskRequest(
    Guid TaskId,
    string ApprovedWorkspace,
    string Objective,
    IReadOnlySet<DeveloperOperationClass> PermittedOperations,
    IReadOnlyList<string> ProhibitedOperations,
    int IterationLimit,
    TimeSpan Timeout,
    int CurrentIteration = 0,
    DeveloperTaskStatus FinalStatus = DeveloperTaskStatus.Pending);

public sealed record WorkspaceAuthorizationResult(bool Allowed, string? CanonicalWorkspace, string Reason);

public sealed record CodexInvocation(string FileName, IReadOnlyList<string> Arguments, string WorkingDirectory, string Prompt, string ReportPath);

public sealed record CodexRunResult(
    int ExitCode,
    bool TimedOut,
    string StandardOutput,
    string StandardError,
    string? FinalReport,
    IReadOnlyList<string> ChangedFiles);

public sealed record DeveloperTaskEvidence(
    Guid TaskId,
    string Workspace,
    string Objective,
    DeveloperTaskStatus Status,
    int Iterations,
    int? LastCodexExitCode,
    bool TimedOut,
    IReadOnlyList<string> ChangedFiles,
    IReadOnlyList<string> BuildOrTestEvidence,
    string FinalReport,
    string? FailureReason);
