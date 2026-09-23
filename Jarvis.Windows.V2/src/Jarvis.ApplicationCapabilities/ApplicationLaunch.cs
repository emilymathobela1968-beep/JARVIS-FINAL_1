namespace Jarvis.ApplicationCapabilities;

public enum ApplicationId { Word, Calculator, FileExplorer, Notepad }
public enum LaunchStatus { Succeeded, Failed, AlreadyRunning, NotFound, PermissionDenied, TimedOut, Cancelled }
public enum AuthorizationLevel { ApprovedLocalApplication }
public sealed record LaunchApplicationRequest(ApplicationId Application);
public sealed record ApplicationDefinition(ApplicationId Id, string DisplayName, string[] Aliases, AuthorizationLevel Authorization);
public static class ApplicationRegistry
{
    private static readonly ApplicationDefinition[] entries = [
        new(ApplicationId.Word, "Microsoft Word", ["word", "microsoft word"], AuthorizationLevel.ApprovedLocalApplication),
        new(ApplicationId.Calculator, "Calculator", ["calculator"], AuthorizationLevel.ApprovedLocalApplication),
        new(ApplicationId.FileExplorer, "File Explorer", ["file explorer", "explorer"], AuthorizationLevel.ApprovedLocalApplication),
        new(ApplicationId.Notepad, "Notepad", ["notepad"], AuthorizationLevel.ApprovedLocalApplication)];
    public static IReadOnlyList<ApplicationDefinition> Entries => Array.AsReadOnly(entries);
    public static ApplicationDefinition? Find(ApplicationId id) => entries.FirstOrDefault(x => x.Id == id);
}
public sealed record ResolvedApplication(ApplicationId Id, string Executable);
public sealed record WindowEvidence(int ProcessId, long WindowHandle, string Executable, string WindowClass);
public sealed record LaunchResult(Guid OperationId, ApplicationId Application, LaunchStatus Status, string Message,
    WindowEvidence? Evidence, double ElapsedMs, bool LaunchAttempted);
public interface IApplicationAuthorization { bool IsAllowed(ApplicationDefinition application); }
public sealed class AllowListedApplicationAuthorization : IApplicationAuthorization
{
    public bool IsAllowed(ApplicationDefinition application) => ApplicationRegistry.Find(application.Id) is not null && application.Authorization == AuthorizationLevel.ApprovedLocalApplication;
}
public interface IApplicationHost
{
    ResolvedApplication? Resolve(ApplicationDefinition application);
    WindowEvidence? FindWindow(ResolvedApplication application);
    int? Launch(ResolvedApplication application);
}
public interface ILaunchAudit { void Write(string stage, Guid operationId, ApplicationId application, LaunchResult? result = null); }

/// <summary>Typed execution only. Owns its deadline and cancellation; has no voice dependency.</summary>
public sealed class ApplicationLaunchExecutor(IApplicationHost host, IApplicationAuthorization authorization, ILaunchAudit audit)
{
    // One operation per executor at a time prevents duplicate concurrent launch races.
    private readonly SemaphoreSlim serial = new(1, 1);
    public async Task<LaunchResult> ExecuteAsync(LaunchApplicationRequest request, TimeSpan timeout, CancellationToken operationCancellation = default, Action<string, object?>? trace = null)
    {
        if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromMinutes(1)) throw new ArgumentOutOfRangeException(nameof(timeout));
        var id = Guid.NewGuid();
        void Trace(string stage, object? data) => trace?.Invoke(stage, new { operationId = id, data });
        Trace("executor_invoked", request);
        var clock = System.Diagnostics.Stopwatch.StartNew(); bool launched = false, entered = false;
        LaunchResult Finish(LaunchStatus status, string message, WindowEvidence? evidence = null)
        {
            var result = new LaunchResult(id, request.Application, status, message, evidence, clock.Elapsed.TotalMilliseconds, launched);
            audit.Write("result", id, request.Application, result); Trace("structured_result", result); return result;
        }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(operationCancellation);
        deadline.CancelAfter(timeout);
        var token = deadline.Token;
        audit.Write("requested", id, request.Application);
        try
        {
            await serial.WaitAsync(token); entered = true;
            token.ThrowIfCancellationRequested();
            var application = ApplicationRegistry.Find(request.Application);
            Trace("registry_resolved", application);
            var allowed = application is not null && authorization.IsAllowed(application);
            Trace("authorization_decision", new { allowed });
            if (!allowed) return Finish(LaunchStatus.PermissionDenied, "Application launch is not authorized.");
            audit.Write("authorized", id, request.Application);
            var resolved = host.Resolve(application!);
            Trace("executable_resolved", resolved);
            token.ThrowIfCancellationRequested();
            if (resolved is null) return Finish(LaunchStatus.NotFound, $"{application!.DisplayName} was not found in approved installation locations.");
            var existing = host.FindWindow(resolved);
            Trace("verification_before_launch", existing);
            token.ThrowIfCancellationRequested();
            if (existing is not null) return Finish(LaunchStatus.AlreadyRunning, $"{application!.DisplayName} is already open.", existing);
            token.ThrowIfCancellationRequested();
            audit.Write("launch_requested", id, request.Application);
            Trace("process_start_requested", resolved);
            launched = true; var launchPid = host.Launch(resolved);
            Trace("process_start_returned", new { launchPid, verified = false });
            while (true)
            {
                token.ThrowIfCancellationRequested();
                var evidence = host.FindWindow(resolved);
                Trace("verification_after_launch", evidence);
                token.ThrowIfCancellationRequested();
                if (evidence is not null) return Finish(LaunchStatus.Succeeded, $"{application!.DisplayName} is open; its application window was verified.", evidence);
                await Task.Delay(100, token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return Finish(operationCancellation.IsCancellationRequested ? LaunchStatus.Cancelled : LaunchStatus.TimedOut,
                launched ? "Launch was requested, but verification did not complete. The application may still open." : "Operation ended before launch.");
        }
        catch (UnauthorizedAccessException) { return Finish(LaunchStatus.PermissionDenied, "Windows denied access to the application."); }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return Finish(ex.NativeErrorCode == 5 ? LaunchStatus.PermissionDenied : LaunchStatus.Failed, $"Windows application operation failed (code {ex.NativeErrorCode}).");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.Security.SecurityException)
        { return Finish(LaunchStatus.Failed, $"Application operation failed ({ex.GetType().Name})."); }
        finally { if (entered) serial.Release(); }
    }
}

