using System.Diagnostics;
using System.Text;

namespace Jarvis.Developer;

public interface ICodexProcessRunner
{
    Task<CodexRunResult> RunAsync(CodexInvocation invocation, TimeSpan timeout, CancellationToken cancellationToken);
}

public sealed class CodexAgentClient(string codexPath, ICodexProcessRunner runner)
{
    public CodexInvocation CreateInvocation(DeveloperTaskRequest request, string prompt, string reportPath)
    {
        var args = new[]
        {
            "exec",
            "--cd", request.ApprovedWorkspace,
            "--sandbox", "workspace-write",
            "--json",
            "--output-last-message", reportPath,
            prompt
        };
        return new(codexPath, args, request.ApprovedWorkspace, prompt, reportPath);
    }

    public Task<CodexRunResult> RunAsync(CodexInvocation invocation, TimeSpan timeout, CancellationToken cancellationToken) =>
        runner.RunAsync(invocation, timeout, cancellationToken);
}

public sealed class ProcessCodexRunner : ICodexProcessRunner
{
    public async Task<CodexRunResult> RunAsync(CodexInvocation invocation, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        var start = new ProcessStartInfo(invocation.FileName)
        {
            WorkingDirectory = invocation.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var arg in invocation.Arguments) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start Codex.");
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var outTask = process.StandardOutput.ReadToEndAsync(deadline.Token);
        var errTask = process.StandardError.ReadToEndAsync(deadline.Token);
        try { await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            return new(-1, true, string.Empty, "timeout", null, []);
        }
        stdout.Append(await outTask.ConfigureAwait(false));
        stderr.Append(await errTask.ConfigureAwait(false));
        var report = File.Exists(invocation.ReportPath) ? await File.ReadAllTextAsync(invocation.ReportPath, cancellationToken).ConfigureAwait(false) : null;
        return new(process.ExitCode, false, stdout.ToString(), stderr.ToString(), report, []);
    }
}
