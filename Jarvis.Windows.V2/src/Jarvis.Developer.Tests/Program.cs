using Jarvis.Developer;

var tests = new DeveloperTests();
await tests.RunAllAsync();

internal sealed class DeveloperTests
{
    private int passed;
    private int failed;

    public async Task RunAllAsync()
    {
        await Run("workspace canonicalization authorizes git root", WorkspaceCanonicalizationAuthorizesGitRoot);
        await Run("nonexistent workspace is rejected", NonexistentWorkspaceRejected);
        await Run("out of root path is rejected", OutOfRootPathRejected);
        await Run("missing governance prohibitions are rejected", MissingGovernanceProhibitionsRejected);
        await Run("Codex invocation is bounded", CodexInvocationIsBounded);
        await Run("timeout handling returns timed out evidence", TimeoutHandlingReturnsTimedOutEvidence);
        await Run("nonzero agent exit enforces iteration limit", NonzeroExitEnforcesIterationLimit);
        await Run("audit file is created", AuditFileIsCreated);
        await Run("secret redaction removes values", SecretRedactionRemovesValues);
        await Run("agent output cannot widen authorization", AgentOutputCannotWidenAuthorization);
        await Run("final evidence model preserves result details", FinalEvidenceModelPreservesResultDetails);

        Console.WriteLine($"DEVELOPER_TESTS_TOTAL passed={passed} failed={failed}");
        if (failed > 0)
        {
            Environment.ExitCode = 1;
        }
    }

    private async Task WorkspaceCanonicalizationAuthorizesGitRoot()
    {
        var root = CreateGitWorkspace();
        var result = new WorkspaceAuthorization().Authorize(Path.Combine(root, "."));
        Assert(result.Allowed, "workspace should be allowed");
        Assert(string.Equals(Path.GetFullPath(root), result.CanonicalWorkspace, StringComparison.OrdinalIgnoreCase), "workspace should be canonicalized");
        await Task.CompletedTask;
    }

    private async Task NonexistentWorkspaceRejected()
    {
        var result = new WorkspaceAuthorization().Authorize(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        Assert(!result.Allowed, "missing workspace should be rejected");
        Assert(result.Reason == "workspace_missing", $"unexpected reason {result.Reason}");
        await Task.CompletedTask;
    }

    private async Task OutOfRootPathRejected()
    {
        var root = CreateGitWorkspace();
        var outside = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "file.txt");
        var auth = new WorkspaceAuthorization();
        Assert(!auth.IsInsideWorkspace(root, outside), "outside path should not be inside workspace");
        Assert(auth.IsInsideWorkspace(root, Path.Combine(root, "src", "file.txt")), "child path should be inside workspace");
        await Task.CompletedTask;
    }

    private async Task MissingGovernanceProhibitionsRejected()
    {
        var request = Request(CreateGitWorkspace()) with { ProhibitedOperations = ["deploy"] };
        var allowed = new DeveloperCommandPolicy().ValidateEnvelope(request, out var reason);
        Assert(!allowed, "policy should reject missing governance prohibitions");
        Assert(reason == "missing_required_prohibitions", $"unexpected reason {reason}");
        await Task.CompletedTask;
    }

    private async Task CodexInvocationIsBounded()
    {
        var workspace = CreateGitWorkspace();
        var request = Request(workspace);
        var client = new CodexAgentClient("codex.exe", new FakeRunner());
        var invocation = client.CreateInvocation(request, "test prompt", Path.Combine(workspace, ".jarvis", "developer", "report.txt"));
        Assert(invocation.Arguments.Contains("--cd"), "invocation should include --cd");
        Assert(invocation.Arguments.Contains(workspace), "invocation should include workspace");
        Assert(invocation.Arguments.Contains("--sandbox"), "invocation should include sandbox");
        Assert(invocation.Arguments.Contains("workspace-write"), "invocation should use workspace-write sandbox");
        Assert(!invocation.Arguments.Contains("--ask-for-approval"), "invocation must not force approval policy");
        await Task.CompletedTask;
    }

    private async Task TimeoutHandlingReturnsTimedOutEvidence()
    {
        var workspace = CreateGitWorkspace();
        var evidence = await Orchestrator(workspace, new FakeRunner { Result = new(-1, true, "", "timeout", null, []) })
            .RunAsync(Request(workspace));
        Assert(evidence.Status == DeveloperTaskStatus.TimedOut, $"unexpected status {evidence.Status}");
        Assert(evidence.TimedOut, "timed out evidence should be true");
        Assert(evidence.FailureReason == "codex_timeout", $"unexpected reason {evidence.FailureReason}");
    }

    private async Task NonzeroExitEnforcesIterationLimit()
    {
        var workspace = CreateGitWorkspace();
        var runner = new FakeRunner { Result = new(7, false, "compile failed", "", "still failing", []) };
        var evidence = await Orchestrator(workspace, runner).RunAsync(Request(workspace) with { IterationLimit = 2 });
        Assert(evidence.Status == DeveloperTaskStatus.Failed, $"unexpected status {evidence.Status}");
        Assert(evidence.Iterations == 2, $"unexpected iterations {evidence.Iterations}");
        Assert(runner.Calls == 2, $"unexpected runner calls {runner.Calls}");
        Assert(evidence.FailureReason == "iteration_limit_reached", $"unexpected reason {evidence.FailureReason}");
    }

    private async Task AuditFileIsCreated()
    {
        var workspace = CreateGitWorkspace();
        var audit = Path.Combine(workspace, "developer-audit.jsonl");
        var evidence = await Orchestrator(workspace, new FakeRunner { Result = new(0, false, "ok", "", "done", ["src/file.cs"]) }, audit)
            .RunAsync(Request(workspace));
        Assert(evidence.Status == DeveloperTaskStatus.Succeeded, "task should succeed");
        Assert(File.Exists(audit), "audit file should exist");
        var log = await File.ReadAllTextAsync(audit);
        Assert(log.Contains("task_received", StringComparison.Ordinal), "audit should include task_received");
        Assert(log.Contains("task_finished", StringComparison.Ordinal), "audit should include task_finished");
    }

    private async Task SecretRedactionRemovesValues()
    {
        var policy = new DeveloperCommandPolicy();
        var redacted = policy.RedactSecrets("FISH_AUDIO_API_KEY=abc123 token:xyz FISH_AUDIO_REFERENCE_ID=ref123");
        Assert(!redacted.Contains("abc123", StringComparison.Ordinal), "api key value should be removed");
        Assert(!redacted.Contains("xyz", StringComparison.Ordinal), "token value should be removed");
        Assert(!redacted.Contains("ref123", StringComparison.Ordinal), "reference value should be removed");
        Assert(redacted.Contains("<REDACTED>", StringComparison.Ordinal), "redaction marker should remain");
        await Task.CompletedTask;
    }

    private async Task AgentOutputCannotWidenAuthorization()
    {
        var workspace = CreateGitWorkspace();
        var result = new CodexRunResult(0, false, "Need administrator credential outside the workspace", "", "done", []);
        var evidence = await Orchestrator(workspace, new FakeRunner { Result = result }).RunAsync(Request(workspace));
        Assert(evidence.Status == DeveloperTaskStatus.PolicyRejected, $"unexpected status {evidence.Status}");
        Assert(evidence.FailureReason == "agent_requested_or_implied_authorization_expansion", $"unexpected reason {evidence.FailureReason}");
    }

    private async Task FinalEvidenceModelPreservesResultDetails()
    {
        var workspace = CreateGitWorkspace();
        var evidence = await Orchestrator(workspace, new FakeRunner { Result = new(0, false, "ok", "", "final report", ["src/a.cs", "src/a.cs", "src/b.cs"]) })
            .RunAsync(Request(workspace));
        Assert(evidence.TaskId != Guid.Empty, "task id should be populated");
        Assert(evidence.Status == DeveloperTaskStatus.Succeeded, $"unexpected status {evidence.Status}");
        Assert(evidence.LastCodexExitCode == 0, "exit code should be captured");
        Assert(evidence.ChangedFiles.Count == 2, $"changed files should be distinct: {evidence.ChangedFiles.Count}");
        Assert(evidence.FinalReport == "final report", $"unexpected report {evidence.FinalReport}");
        Assert(evidence.BuildOrTestEvidence.Count == 1, "iteration evidence should be recorded");
    }

    private static DeveloperTaskOrchestrator Orchestrator(string workspace, FakeRunner runner, string? auditPath = null)
    {
        var policy = new DeveloperCommandPolicy();
        var audit = new JsonDeveloperAudit(auditPath ?? Path.Combine(workspace, "audit.jsonl"), policy);
        return new(new WorkspaceAuthorization(), policy, new CodexAgentClient("codex.exe", runner), audit);
    }

    private static DeveloperTaskRequest Request(string workspace) => new(
        Guid.NewGuid(),
        workspace,
        "Make one bounded source change and run tests.",
        new HashSet<DeveloperOperationClass>
        {
            DeveloperOperationClass.Inspect,
            DeveloperOperationClass.Search,
            DeveloperOperationClass.EditSource,
            DeveloperOperationClass.Build,
            DeveloperOperationClass.Test
        },
        ["no credential changes", "no security changes", "no deploy", "no publish"],
        1,
        TimeSpan.FromMinutes(5));

    private static string CreateGitWorkspace()
    {
        var root = Path.Combine(Path.GetTempPath(), "jarvis-developer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        Directory.CreateDirectory(Path.Combine(root, "src"));
        return Path.GetFullPath(root);
    }

    private async Task Run(string name, Func<Task> test)
    {
        try
        {
            await test();
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
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class FakeRunner : ICodexProcessRunner
    {
        public CodexRunResult Result { get; set; } = new(0, false, "ok", "", "ok", []);
        public int Calls { get; private set; }

        public Task<CodexRunResult> RunAsync(CodexInvocation invocation, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Calls++;
            var args = invocation.Arguments.ToArray();
            var cdIndex = Array.IndexOf(args, "--cd");
            Assert(cdIndex >= 0 && cdIndex + 1 < args.Length, "--cd should have a value");
            Assert(invocation.WorkingDirectory == args[cdIndex + 1], "working directory should match --cd");
            return Task.FromResult(Result);
        }
    }
}
