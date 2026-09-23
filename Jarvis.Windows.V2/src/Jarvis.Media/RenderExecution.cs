using System.Diagnostics;

namespace Jarvis.Media;

public interface IRendererProcess
{
    Task<RendererExecutionResult> RunAsync(RendererCommand command, TimeSpan timeout, CancellationToken cancellationToken);
}

public sealed class LocalRendererProcess : IRendererProcess
{
    public async Task<RendererExecutionResult> RunAsync(RendererCommand command, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        var outputPath = command.Arguments.Last();
        var start = new ProcessStartInfo(command.FileName)
        {
            WorkingDirectory = command.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        foreach (var arg in command.Arguments) start.ArgumentList.Add(arg);

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Renderer process could not be started.");
        var stderrTask = process.StandardError.ReadToEndAsync(deadline.Token);
        try
        {
            await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            return new(-1, true, "renderer_timeout", outputPath, File.Exists(outputPath) ? new FileInfo(outputPath).Length : 0);
        }

        var stderr = await stderrTask.ConfigureAwait(false);
        return new(process.ExitCode, false, stderr, outputPath, File.Exists(outputPath) ? new FileInfo(outputPath).Length : 0);
    }
}
