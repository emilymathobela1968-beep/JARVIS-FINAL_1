namespace Jarvis.Developer;

public sealed class DeveloperTaskOrchestrator(
    WorkspaceAuthorization workspaceAuthorization,
    DeveloperCommandPolicy policy,
    CodexAgentClient codex,
    JsonDeveloperAudit audit)
{
    public async Task<DeveloperTaskEvidence> RunAsync(DeveloperTaskRequest request, CancellationToken cancellationToken = default)
    {
        audit.Write(request.TaskId, "task_received", new { request.ApprovedWorkspace, request.Objective, request.IterationLimit });
        var workspace = workspaceAuthorization.Authorize(request.ApprovedWorkspace);
        if (!workspace.Allowed)
            return Reject(request, workspace.Reason);

        request = request with { ApprovedWorkspace = workspace.CanonicalWorkspace! };
        if (!policy.ValidateEnvelope(request, out var policyReason))
            return Reject(request, policyReason);

        var allChanged = new List<string>();
        var evidence = new List<string>();
        string finalReport = string.Empty;
        int? lastExit = null;

        for (var iteration = 1; iteration <= request.IterationLimit; iteration++)
        {
            audit.Write(request.TaskId, "iteration_started", new { iteration });
            var reportPath = Path.Combine(request.ApprovedWorkspace, ".jarvis", "developer", request.TaskId.ToString("N"), $"codex-report-{iteration}.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            var invocation = codex.CreateInvocation(request with { CurrentIteration = iteration }, BuildPrompt(request, iteration), reportPath);
            audit.Write(request.TaskId, "codex_invocation", new { invocation.FileName, invocation.Arguments, invocation.WorkingDirectory });
            var result = await codex.RunAsync(invocation, request.Timeout, cancellationToken).ConfigureAwait(false);
            lastExit = result.ExitCode;
            finalReport = policy.RedactSecrets(result.FinalReport ?? result.StandardOutput);
            allChanged.AddRange(result.ChangedFiles);
            evidence.Add($"iteration={iteration}; exit={result.ExitCode}; timedOut={result.TimedOut}");
            audit.Write(request.TaskId, "codex_result", new { result.ExitCode, result.TimedOut, result.ChangedFiles, finalReport });

            if (result.TimedOut)
                return Finish(request, DeveloperTaskStatus.TimedOut, iteration, lastExit, true, allChanged, evidence, finalReport, "codex_timeout");
            if (policy.IsAgentAuthorizationExpansion(result.StandardOutput + result.StandardError + finalReport, out var expansionReason))
                return Finish(request, DeveloperTaskStatus.PolicyRejected, iteration, lastExit, false, allChanged, evidence, finalReport, expansionReason);
            if (result.ExitCode == 0)
                return Finish(request, DeveloperTaskStatus.Succeeded, iteration, lastExit, false, allChanged, evidence, finalReport, null);
        }

        return Finish(request, DeveloperTaskStatus.Failed, request.IterationLimit, lastExit, false, allChanged, evidence, finalReport, "iteration_limit_reached");
    }

    private DeveloperTaskEvidence Reject(DeveloperTaskRequest request, string reason)
    {
        audit.Write(request.TaskId, "policy_rejected", new { reason });
        return new(request.TaskId, request.ApprovedWorkspace, request.Objective, DeveloperTaskStatus.PolicyRejected, 0, null, false, [], [], string.Empty, reason);
    }

    private DeveloperTaskEvidence Finish(DeveloperTaskRequest request, DeveloperTaskStatus status, int iterations, int? exit, bool timedOut, IReadOnlyList<string> changed, IReadOnlyList<string> evidence, string report, string? reason)
    {
        var final = new DeveloperTaskEvidence(request.TaskId, request.ApprovedWorkspace, request.Objective, status, iterations, exit, timedOut, changed.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), evidence, report, reason);
        audit.Write(request.TaskId, "task_finished", final);
        return final;
    }

    private static string BuildPrompt(DeveloperTaskRequest request, int iteration) => $"""
        You are running as Developer Jarvis's bounded development agent.
        Approved workspace: {request.ApprovedWorkspace}
        Objective from Leon: {request.Objective}
        Iteration: {iteration} of {request.IterationLimit}
        Permitted operation classes: {string.Join(", ", request.PermittedOperations)}
        Prohibited operations: {string.Join("; ", request.ProhibitedOperations)}

        Stay inside the approved workspace. Do not request or use credentials. Do not deploy, publish, alter security, alter Jarvis governance, or modify unrelated files.
        Inspect the code, make the smallest needed implementation, run relevant build/test commands, and report files changed plus evidence.
        If the task requires authority outside this envelope, stop and say exactly what approval is required.
        """;
}
