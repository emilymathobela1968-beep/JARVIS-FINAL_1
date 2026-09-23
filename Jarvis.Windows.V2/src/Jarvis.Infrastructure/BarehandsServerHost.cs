using System.Diagnostics;
using System.Net;
using System.Net.Http;

namespace Jarvis.Infrastructure;

/// <summary>
/// Owns the packaged Barehands localhost process for the lifetime of its Jarvis view.
/// This class launches one fixed Python entry point; it never accepts executable,
/// argument, address, or media-path input from the UI or a model.
/// </summary>
public sealed class BarehandsServerHost : IAsyncDisposable
{
    public const int LoopbackPort = 8794;
    public static readonly Uri StageUri = new($"http://127.0.0.1:{LoopbackPort}/stage.html");

    private static readonly string VoiceEnvironmentRoot = @"C:\JARVIS_CODEX\backtalk-main\backtalk-main";
    private static readonly string PythonExe = Path.Combine(VoiceEnvironmentRoot, ".venv", "Scripts", "python.exe");
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(1) };

    private readonly SemaphoreSlim gate = new(1, 1);
    private Process? process;

    public bool IsRunning => process is { HasExited: false };

    public static string RuntimeRoot => Path.Combine(AppContext.BaseDirectory, "Barehands");

    public static BarehandsRuntimeLayout GetRuntimeLayout(string? runtimeRoot = null)
    {
        var root = Path.GetFullPath(runtimeRoot ?? RuntimeRoot);
        return new BarehandsRuntimeLayout(
            root,
            Path.Combine(root, "server.py"),
            Path.Combine(root, "stage.html"),
            Path.Combine(root, "LICENSE"),
            StageUri);
    }

    public static bool IsValidRuntimeLayout(BarehandsRuntimeLayout layout) =>
        layout.StageUri.Scheme == Uri.UriSchemeHttp &&
        string.Equals(layout.StageUri.Host, IPAddress.Loopback.ToString(), StringComparison.Ordinal) &&
        layout.StageUri.Port == LoopbackPort &&
        File.Exists(layout.ServerScriptPath) &&
        File.Exists(layout.StagePath) &&
        File.Exists(layout.LicensePath);

    public async Task<BarehandsServerResult> StartAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsRunning)
            {
                return new BarehandsServerResult(true, false, "Barehands is already running.");
            }

            var layout = GetRuntimeLayout();
            if (!IsValidRuntimeLayout(layout))
            {
                return new BarehandsServerResult(false, false, "Barehands package is incomplete.", layout.RootPath);
            }
            if (!File.Exists(PythonExe))
            {
                return new BarehandsServerResult(false, false, "Jarvis Python runtime is unavailable.", PythonExe);
            }
            if (await IsStageAvailableAsync(cancellationToken).ConfigureAwait(false))
            {
                return new BarehandsServerResult(false, false,
                    "Barehands loopback port is already in use; Jarvis will not adopt an unknown server.",
                    StageUri.ToString());
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = PythonExe,
                WorkingDirectory = layout.RootPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            startInfo.ArgumentList.Add(layout.ServerScriptPath);

            process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            if (!process.Start())
            {
                process.Dispose();
                process = null;
                return new BarehandsServerResult(false, false, "Barehands server did not start.");
            }

            _ = DrainAsync(process.StandardOutput);
            _ = DrainAsync(process.StandardError);
            for (var attempt = 0; attempt < 20; attempt++)
            {
                if (await IsStageAvailableAsync(cancellationToken).ConfigureAwait(false))
                {
                    return new BarehandsServerResult(true, true, "Barehands loopback server started.");
                }
                if (process.HasExited)
                {
                    var exitCode = process.ExitCode;
                    process.Dispose();
                    process = null;
                    return new BarehandsServerResult(false, false, "Barehands server exited during startup.", $"Exit code {exitCode}.");
                }
                await Task.Delay(150, cancellationToken).ConfigureAwait(false);
            }

            await StopOwnedProcessAsync().ConfigureAwait(false);
            return new BarehandsServerResult(false, false, "Barehands loopback server did not become ready.");
        }
        catch (OperationCanceledException)
        {
            await StopOwnedProcessAsync().ConfigureAwait(false);
            return new BarehandsServerResult(false, false, "Barehands startup was canceled.");
        }
        catch (Exception ex)
        {
            await StopOwnedProcessAsync().ConfigureAwait(false);
            return new BarehandsServerResult(false, false, "Barehands startup failed.", ex.Message);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task StopAsync()
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await StopOwnedProcessAsync().ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task<bool> IsStageAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await Http.GetAsync(StageUri, cancellationToken).ConfigureAwait(false);
            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private async Task StopOwnedProcessAsync()
    {
        var owned = process;
        process = null;
        if (owned is null)
        {
            return;
        }

        try
        {
            if (!owned.HasExited)
            {
                owned.Kill(entireProcessTree: true);
                await owned.WaitForExitAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            owned.Dispose();
        }
    }

    private static async Task DrainAsync(StreamReader reader)
    {
        while (await reader.ReadLineAsync().ConfigureAwait(false) is not null)
        {
            // The bundled server logs only startup/access diagnostics. Draining avoids a child pipe deadlock.
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        gate.Dispose();
    }
}

public sealed record BarehandsRuntimeLayout(
    string RootPath,
    string ServerScriptPath,
    string StagePath,
    string LicensePath,
    Uri StageUri);

public sealed record BarehandsServerResult(bool Success, bool Started, string Message, string? Error = null);
