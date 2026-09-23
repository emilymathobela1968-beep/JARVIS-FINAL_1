using System.Diagnostics;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text;

namespace Jarvis.Computer;

public sealed record ApplicationIdentity(string Name, string ExecutablePath, string Source);
public sealed record RunningApplication(int ProcessId, string Name, string? MainWindowTitle);
public sealed record VisibleApplicationWindow(long WindowHandle, int ProcessId, string ProcessName, string Title, string ClassName);

public interface IApplicationCatalog
{
    IReadOnlyList<ApplicationIdentity> Discover();
}

public interface IApplicationHost
{
    ComputerObservation Observe(string nameOrPath);
    ComputerOperationStatus Launch(string executablePath, out string evidence);
    ComputerOperationStatus Focus(int processId);
    ComputerOperationStatus Window(int processId, string action);
    IReadOnlyList<RunningApplication> Running();
    IReadOnlyList<VisibleApplicationWindow> VisibleWindows();
    ComputerOperationStatus WindowByHandle(long windowHandle, string action);
}

public sealed class WindowsApplicationCatalog : IApplicationCatalog
{
    public IReadOnlyList<ApplicationIdentity> Discover()
    {
        var discovered = new List<ApplicationIdentity>();
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
        };

        discovered.AddRange(roots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                .Select(path => new ApplicationIdentity(Path.GetFileNameWithoutExtension(path), path, root))));

        discovered.AddRange(DiscoverAppPaths(Registry.CurrentUser));
        discovered.AddRange(DiscoverAppPaths(Registry.LocalMachine));
        discovered.AddRange(KnownWindowsApplications());

        return discovered
            .GroupBy(x => Normalize(x.Name) + "|" + x.ExecutablePath, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Take(500)
            .ToArray();
    }

    private static IEnumerable<ApplicationIdentity> DiscoverAppPaths(RegistryKey hive)
    {
        using var key = hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths");
        if (key is null)
            yield break;

        foreach (var subKeyName in key.GetSubKeyNames())
        {
            using var appKey = key.OpenSubKey(subKeyName);
            var path = appKey?.GetValue(null) as string;
            if (!string.IsNullOrWhiteSpace(path))
                yield return new ApplicationIdentity(Path.GetFileNameWithoutExtension(subKeyName), path, "app_paths");
        }
    }

    private static IEnumerable<ApplicationIdentity> KnownWindowsApplications()
    {
        var system = Environment.SystemDirectory;
        yield return new("Calculator", Path.Combine(system, "calc.exe"), "windows_known_app");
        yield return new("Notepad", Path.Combine(system, "notepad.exe"), "windows_known_app");
        yield return new("File Explorer", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"), "windows_known_app");
        yield return new("Settings", "ms-settings:", "windows_known_app");
        yield return new("Microsoft Edge", "msedge.exe", "windows_known_app");
    }

    internal static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}

public sealed class WindowsApplicationHost : IApplicationHost
{
    private const uint WmClose = 0x0010;

    public ComputerObservation Observe(string nameOrPath)
    {
        var target = WindowsApplicationCatalog.Normalize(Path.GetFileNameWithoutExtension(nameOrPath));
        var running = Running().Where(x =>
            WindowsApplicationCatalog.Normalize(x.Name).Contains(target, StringComparison.OrdinalIgnoreCase) ||
            WindowsApplicationCatalog.Normalize(x.MainWindowTitle ?? string.Empty).Contains(target, StringComparison.OrdinalIgnoreCase)).ToArray();
        return new(running.Length > 0, running.Length > 0 ? "running" : "not_running", new Dictionary<string, string> { ["matches"] = running.Length.ToString() });
    }

    public ComputerOperationStatus Launch(string executablePath, out string evidence)
    {
        var process = Process.Start(new ProcessStartInfo(executablePath) { UseShellExecute = true });
        evidence = process?.Id.ToString() ?? "started_via_shell";
        return ComputerOperationStatus.Succeeded;
    }

    public ComputerOperationStatus Focus(int processId)
    {
        var process = Process.GetProcessById(processId);
        return process.MainWindowHandle != IntPtr.Zero && SetForegroundWindow(process.MainWindowHandle)
            ? ComputerOperationStatus.Succeeded
            : ComputerOperationStatus.VerificationFailed;
    }

    public ComputerOperationStatus Window(int processId, string action)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (process.MainWindowHandle == IntPtr.Zero) return ComputerOperationStatus.Unavailable;
            var command = action.ToLowerInvariant() switch
            {
                "minimize" => 6,
                "maximize" => 3,
                "restore" => 9,
                "close" => 0,
                _ => -1
            };
            if (command < 0) return ComputerOperationStatus.Unsupported;
            if (!action.Equals("close", StringComparison.OrdinalIgnoreCase))
                return ShowWindow(process.MainWindowHandle, command) ? ComputerOperationStatus.Succeeded : ComputerOperationStatus.VerificationFailed;

            if (!process.CloseMainWindow())
                return ComputerOperationStatus.VerificationFailed;

            return process.WaitForExit(3000)
                ? ComputerOperationStatus.Succeeded
                : ComputerOperationStatus.ConfirmationRequired;
        }
        catch (ArgumentException)
        {
            return action.Equals("close", StringComparison.OrdinalIgnoreCase)
                ? ComputerOperationStatus.Succeeded
                : ComputerOperationStatus.Unavailable;
        }
        catch (InvalidOperationException)
        {
            return ComputerOperationStatus.Failed;
        }
    }

    public IReadOnlyList<RunningApplication> Running() =>
        Process.GetProcesses()
            .Where(p => !string.IsNullOrWhiteSpace(p.ProcessName))
            .Select(p =>
            {
                string? title = null;
                try { title = p.MainWindowTitle; } catch { }
                return new RunningApplication(p.Id, p.ProcessName, title);
            })
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public IReadOnlyList<VisibleApplicationWindow> VisibleWindows()
    {
        var windows = new List<VisibleApplicationWindow>();
        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle))
                return true;

            var titleLength = GetWindowTextLength(handle);
            if (titleLength <= 0)
                return true;

            var title = new StringBuilder(titleLength + 1);
            if (GetWindowText(handle, title, title.Capacity) <= 0 || string.IsNullOrWhiteSpace(title.ToString()))
                return true;

            GetWindowThreadProcessId(handle, out var processId);
            try
            {
                using var process = Process.GetProcessById((int)processId);
                var className = new StringBuilder(256);
                GetClassName(handle, className, className.Capacity);
                windows.Add(new(handle.ToInt64(), process.Id, process.ProcessName, title.ToString(), className.ToString()));
            }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
            return true;
        }, IntPtr.Zero);
        return windows.OrderBy(window => window.Title, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public ComputerOperationStatus WindowByHandle(long windowHandle, string action)
    {
        var handle = new IntPtr(windowHandle);
        if (!IsWindow(handle))
            return action.Equals("close", StringComparison.OrdinalIgnoreCase) ? ComputerOperationStatus.Succeeded : ComputerOperationStatus.Unavailable;

        if (action.Equals("focus", StringComparison.OrdinalIgnoreCase))
            return SetForegroundWindow(handle) ? ComputerOperationStatus.Succeeded : ComputerOperationStatus.VerificationFailed;

        var command = action.ToLowerInvariant() switch
        {
            "minimize" => 6,
            "maximize" => 3,
            "restore" => 9,
            _ => -1
        };
        if (command >= 0)
            return ShowWindow(handle, command) ? ComputerOperationStatus.Succeeded : ComputerOperationStatus.VerificationFailed;
        if (!action.Equals("close", StringComparison.OrdinalIgnoreCase))
            return ComputerOperationStatus.Unsupported;
        if (!PostMessage(handle, WmClose, IntPtr.Zero, IntPtr.Zero))
            return ComputerOperationStatus.VerificationFailed;

        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(3))
        {
            if (!IsWindow(handle))
                return ComputerOperationStatus.Succeeded;
            Thread.Sleep(50);
        }
        return ComputerOperationStatus.ConfirmationRequired;
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder className, int count);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);
}

public sealed class ApplicationOperator(IApplicationCatalog catalog, IApplicationHost host, ComputerPolicy policy, IComputerAudit audit)
{
    private static readonly TimeSpan LaunchVerificationTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan LaunchVerificationInterval = TimeSpan.FromMilliseconds(50);

    private static readonly HashSet<string> ProtectedApplications = new(StringComparer.OrdinalIgnoreCase)
    {
        "jarvis",
        "jarvisapp",
        "system",
        "registry",
        "wininit",
        "winlogon",
        "lsass",
        "csrss",
        "services",
        "smss",
        "securityhealthservice",
        "msmpeng"
    };

    private static readonly Dictionary<string, string[]> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["word"] = ["word", "microsoftword", "officeword", "winword"],
        ["microsoftword"] = ["word", "microsoftword", "officeword", "winword"],
        ["excel"] = ["excel", "microsoftexcel", "officeexcel"],
        ["microsoftexcel"] = ["excel", "microsoftexcel", "officeexcel"],
        ["edge"] = ["edge", "microsoftedge", "msedge"],
        ["microsoftedge"] = ["edge", "microsoftedge", "msedge"],
        ["calculator"] = ["calculator", "calc"],
        ["calc"] = ["calculator", "calc"],
        ["notepad"] = ["notepad"],
        ["fileexplorer"] = ["fileexplorer", "explorer", "files"],
        ["explorer"] = ["fileexplorer", "explorer", "files"],
        ["settings"] = ["settings", "mssettings"]
    };

    public Task<ComputerOperationResult> ExecuteAsync(ComputerOperation operation, CancellationToken cancellationToken = default)
    {
        audit.Write(operation.OperationId, "application_request", new { operation.Action, arguments = ComputerText.RedactArguments(operation.Arguments) });
        if (policy.Validate(operation) is { } rejected) return Task.FromResult(rejected);
        cancellationToken.ThrowIfCancellationRequested();

        var requestedName = operation.Arguments.GetValueOrDefault("name", operation.Arguments.GetValueOrDefault("path", string.Empty));
        var before = ObserveVisibleWindows(ResolveVisibleWindows(requestedName));
        ComputerOperationResult result = operation.Action.ToLowerInvariant() switch
        {
            "discover" => Success(operation, before, before, new Dictionary<string, string> { ["count"] = catalog.Discover().Count.ToString() }),
            "running" => RunningStatus(operation, requestedName, before),
            "launch" => Launch(operation, before),
            "close" => Close(operation, before),
            "focus" or "minimize" or "maximize" or "restore" => Window(operation, before),
            _ => ComputerPolicy.Failure(operation, policy.Classify(operation), ComputerOperationStatus.Unsupported, "Unsupported application action.", "unsupported_application_action")
        };
        audit.Write(operation.OperationId, "application_result", new { result.Status, result.FailureReason });
        return Task.FromResult(result);
    }

    private ComputerOperationResult Launch(ComputerOperation operation, ComputerObservation before)
    {
        var path = operation.Arguments.GetValueOrDefault("path");
        var requestedName = operation.Arguments.GetValueOrDefault("name", string.Empty);
        ApplicationIdentity? resolved = null;
        if (string.IsNullOrWhiteSpace(path))
        {
            resolved = ResolveApplication(requestedName);
            path = resolved?.ExecutablePath;
        }
        if (string.IsNullOrWhiteSpace(path)) return ComputerPolicy.Failure(operation, policy.Classify(operation), ComputerOperationStatus.Unavailable, "Application was not found.", "application_not_found");
        var status = host.Launch(path, out var evidence);
        var observeTarget = string.IsNullOrWhiteSpace(requestedName) ? resolved?.Name ?? path : requestedName;
        var after = ObserveLaunchUntilAvailable(observeTarget);
        return new(operation.OperationId, operation.Kind, operation.Action, after.Available ? status : ComputerOperationStatus.VerificationFailed, after.Available ? "Application launched and verified." : "Launch was not verified.", policy.Classify(operation), before, after, after.Available ? null : "launch_verification_failed", new Dictionary<string, string>
        {
            ["launchEvidence"] = evidence,
            ["resolvedName"] = resolved?.Name ?? requestedName,
            ["resolvedSource"] = resolved?.Source ?? "direct_path"
        });
    }

    private ComputerObservation ObserveLaunchUntilAvailable(string target)
    {
        var stopwatch = Stopwatch.StartNew();
        ComputerObservation observation;
        do
        {
            observation = ObserveVisibleWindows(ResolveVisibleWindows(target));
            if (observation.Available)
                return observation;
            if (stopwatch.Elapsed >= LaunchVerificationTimeout)
                return observation;
            Thread.Sleep(LaunchVerificationInterval);
        }
        while (true);
    }

    private ApplicationIdentity? ResolveApplication(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalized = WindowsApplicationCatalog.Normalize(name);
        var accepted = Aliases.TryGetValue(normalized, out var aliases)
            ? aliases.ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase) { normalized };

        return catalog.Discover().FirstOrDefault(app =>
        {
            var appName = WindowsApplicationCatalog.Normalize(app.Name);
            var fileName = WindowsApplicationCatalog.Normalize(Path.GetFileNameWithoutExtension(app.ExecutablePath));
            return accepted.Contains(appName) ||
                   accepted.Contains(fileName) ||
                   appName.Contains(normalized, StringComparison.OrdinalIgnoreCase);
        });
    }

    private ComputerOperationResult Window(ComputerOperation operation, ComputerObservation before)
    {
        if (operation.Arguments.TryGetValue("process_id", out var raw) && int.TryParse(raw, out var pid))
        {
            var processStatus = operation.Action.Equals("focus", StringComparison.OrdinalIgnoreCase) ? host.Focus(pid) : host.Window(pid, operation.Action);
            return new(operation.OperationId, operation.Kind, operation.Action, processStatus, processStatus == ComputerOperationStatus.Succeeded ? "Window action succeeded." : "Window action was not verified.", policy.Classify(operation), before, host.Observe(pid.ToString()), processStatus == ComputerOperationStatus.Succeeded ? null : "window_action_not_verified");
        }

        var requestedName = operation.Arguments.GetValueOrDefault("name", string.Empty);
        if (string.IsNullOrWhiteSpace(requestedName))
            return ComputerPolicy.Failure(operation, policy.Classify(operation), ComputerOperationStatus.Rejected, "Missing application or window name.", "missing_window_name");
        var matches = ResolveVisibleWindows(requestedName);
        if (matches.Count == 0)
            return new(operation.OperationId, operation.Kind, operation.Action, ComputerOperationStatus.Unavailable, $"No visible window matches {requestedName}.", policy.Classify(operation), before, ObserveVisibleWindows(matches), "window_not_found");
        if (matches.Count > 1)
            return new(operation.OperationId, operation.Kind, operation.Action, ComputerOperationStatus.ConfirmationRequired, $"Multiple visible windows match {requestedName}; a more specific target is required.", policy.Classify(operation), before, ObserveVisibleWindows(matches), "ambiguous_windows", new Dictionary<string, string> { ["matchCount"] = matches.Count.ToString() });

        var target = matches[0];
        var status = host.WindowByHandle(target.WindowHandle, operation.Action);
        var afterMatches = ResolveVisibleWindows(requestedName);
        var verified = operation.Action.Equals("focus", StringComparison.OrdinalIgnoreCase)
            ? status == ComputerOperationStatus.Succeeded
            : status == ComputerOperationStatus.Succeeded && afterMatches.Any(window => window.WindowHandle == target.WindowHandle);
        return new(operation.OperationId, operation.Kind, operation.Action, verified ? ComputerOperationStatus.Succeeded : ComputerOperationStatus.VerificationFailed, verified ? "Window action succeeded and was verified." : "Window action was not verified.", policy.Classify(operation), before, ObserveVisibleWindows(afterMatches), verified ? null : "window_action_not_verified", WindowEvidence(target));
    }

    private ComputerOperationResult Close(ComputerOperation operation, ComputerObservation before)
    {
        var requestedName = operation.Arguments.GetValueOrDefault("name", string.Empty);
        if (string.IsNullOrWhiteSpace(requestedName))
            return ComputerPolicy.Failure(operation, policy.Classify(operation), ComputerOperationStatus.Rejected, "Missing application name.", "missing_application_name");

        var normalized = WindowsApplicationCatalog.Normalize(requestedName);
        if (ProtectedApplications.Contains(normalized))
            return ComputerPolicy.Failure(operation, policy.Classify(operation), ComputerOperationStatus.Rejected, "Closing this protected application is blocked.", "protected_application");

        var matches = ResolveVisibleWindows(requestedName);
        if (matches.Count == 0)
            return new(operation.OperationId, operation.Kind, operation.Action, ComputerOperationStatus.Unavailable, $"{requestedName} is not running.", policy.Classify(operation), before, ObserveVisibleWindows(matches), "application_not_running");

        if (matches.Count > 1)
            return new(operation.OperationId, operation.Kind, operation.Action, ComputerOperationStatus.ConfirmationRequired, $"Multiple visible windows match {requestedName}; a specific target is required.", policy.Classify(operation), before, ObserveVisibleWindows(matches), "ambiguous_application_instances", new Dictionary<string, string> { ["matchCount"] = matches.Count.ToString() });

        var target = matches[0];
        var status = host.WindowByHandle(target.WindowHandle, "close");
        var stillVisible = host.VisibleWindows().Any(window => window.WindowHandle == target.WindowHandle);
        var after = new ComputerObservation(!stillVisible, stillVisible ? "close_pending" : "not_running", WindowEvidence(target));

        if (!stillVisible && status == ComputerOperationStatus.Succeeded)
            return new(operation.OperationId, operation.Kind, operation.Action, ComputerOperationStatus.Succeeded, $"{requestedName} closed and is no longer running.", policy.Classify(operation), before, after, Evidence: after.Evidence);

        if (stillVisible && status is ComputerOperationStatus.Succeeded or ComputerOperationStatus.ConfirmationRequired)
            return new(operation.OperationId, operation.Kind, operation.Action, ComputerOperationStatus.ConfirmationRequired, $"{requestedName} is still open and may require a save, discard, or cancel decision.", policy.Classify(operation), before, after, "user_action_required", after.Evidence);

        return new(operation.OperationId, operation.Kind, operation.Action, status, $"The close request for {requestedName} was not verified.", policy.Classify(operation), before, after, "close_not_verified", after.Evidence);
    }

    private IReadOnlyList<VisibleApplicationWindow> ResolveVisibleWindows(string requestedName)
    {
        if (string.IsNullOrWhiteSpace(requestedName))
            return [];

        var normalized = WindowsApplicationCatalog.Normalize(requestedName);
        var aliasEntry = Aliases.FirstOrDefault(entry =>
            entry.Key.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains(entry.Key, StringComparison.OrdinalIgnoreCase) ||
            entry.Value.Any(alias => normalized.Contains(alias, StringComparison.OrdinalIgnoreCase)));
        var accepted = aliasEntry.Value is not null
            ? aliasEntry.Value.ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase) { normalized };
        var installed = ResolveApplication(requestedName);
        if (installed is not null)
        {
            accepted.Add(WindowsApplicationCatalog.Normalize(installed.Name));
            accepted.Add(WindowsApplicationCatalog.Normalize(Path.GetFileNameWithoutExtension(installed.ExecutablePath)));
        }

        var qualifier = accepted
            .OrderByDescending(alias => alias.Length)
            .Aggregate(normalized, (value, alias) => value.Replace(alias, string.Empty, StringComparison.OrdinalIgnoreCase));
        foreach (var generic in new[] { "window", "application", "app", "the" })
            qualifier = qualifier.Replace(generic, string.Empty, StringComparison.OrdinalIgnoreCase);

        return host.VisibleWindows()
            .Where(window =>
            {
                var processName = WindowsApplicationCatalog.Normalize(window.ProcessName);
                var title = WindowsApplicationCatalog.Normalize(window.Title);
                var applicationMatch = accepted.Contains(processName) || accepted.Any(alias => alias.Length >= 4 && title.Contains(alias, StringComparison.OrdinalIgnoreCase));
                return applicationMatch && (string.IsNullOrWhiteSpace(qualifier) || title.Contains(qualifier, StringComparison.OrdinalIgnoreCase));
            })
            .GroupBy(window => window.WindowHandle)
            .Select(group => group.First())
            .ToArray();
    }

    private static ComputerObservation ObserveVisibleWindows(IReadOnlyList<VisibleApplicationWindow> matches) =>
        new(matches.Count > 0, matches.Count switch { 0 => "not_running", 1 => "running", _ => "ambiguous" }, new Dictionary<string, string> { ["matches"] = matches.Count.ToString() });

    private ComputerOperationResult RunningStatus(ComputerOperation operation, string requestedName, ComputerObservation observation) =>
        new(operation.OperationId, operation.Kind, operation.Action, ComputerOperationStatus.Succeeded, observation.Available ? $"{requestedName} has a visible window." : $"{requestedName} does not have a visible window.", policy.Classify(operation), observation, observation, Evidence: observation.Evidence);

    private static IReadOnlyDictionary<string, string> WindowEvidence(VisibleApplicationWindow window) => new Dictionary<string, string>
    {
        ["windowHandle"] = window.WindowHandle.ToString(),
        ["processId"] = window.ProcessId.ToString(),
        ["processName"] = window.ProcessName,
        ["windowTitle"] = window.Title,
        ["windowClass"] = window.ClassName
    };

    private ComputerOperationResult Success(ComputerOperation operation, ComputerObservation before, ComputerObservation after, IReadOnlyDictionary<string, string> evidence) =>
        new(operation.OperationId, operation.Kind, operation.Action, ComputerOperationStatus.Succeeded, "Succeeded.", policy.Classify(operation), before, after, Evidence: evidence);
}
