using System.Diagnostics;
using System.Text.Json;
using Jarvis.Contracts;

namespace Jarvis.Infrastructure;

public interface IWindowsCapabilityHost
{
    void OpenApplication(string executable);
    void OpenFolder(string resolvedPath);
    void OpenUrl(Uri url);
    IReadOnlyList<(string Name, int Id)> ListProcesses();
}

public sealed class WindowsCapabilityHost : IWindowsCapabilityHost
{
    public void OpenApplication(string executable) =>
        Process.Start(new ProcessStartInfo { FileName = executable, UseShellExecute = true });

    public void OpenFolder(string resolvedPath) =>
        Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = resolvedPath, UseShellExecute = true });

    public void OpenUrl(Uri url) =>
        Process.Start(new ProcessStartInfo { FileName = url.AbsoluteUri, UseShellExecute = true });

    public IReadOnlyList<(string Name, int Id)> ListProcesses() =>
        Process.GetProcesses()
            .Select(process =>
            {
                try { return (process.ProcessName, process.Id); }
                finally { process.Dispose(); }
            })
            .OrderBy(process => process.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(process => process.Id)
            .ToArray();
}

public sealed class JsonLineCapabilityAuditSink : ICapabilityAuditSink
{
    private readonly string auditPath;
    private readonly object gate = new();

    public JsonLineCapabilityAuditSink(string auditPath) => this.auditPath = auditPath;

    public void Write(CapabilityAuditEvent auditEvent)
    {
        var line = JsonSerializer.Serialize(auditEvent);
        lock (gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(auditPath)!);
            File.AppendAllText(auditPath, line + Environment.NewLine);
        }
    }
}

public sealed class WindowsCapabilityService : ICapabilityDispatcher
{
    private static readonly HashSet<string> SensitiveFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".env", "secrets.json", "credentials.json", "id_rsa", "id_ed25519",
    };

    private readonly IWindowsCapabilityHost host;
    private readonly ICapabilityAuditSink auditSink;
    private readonly string currentDirectory;

    public WindowsCapabilityService(
        IWindowsCapabilityHost? host = null,
        ICapabilityAuditSink? auditSink = null,
        string? currentDirectory = null)
    {
        this.host = host ?? new WindowsCapabilityHost();
        this.auditSink = auditSink ?? new JsonLineCapabilityAuditSink(
            Path.Combine(AppContext.BaseDirectory, "logs", "capability-audit.jsonl"));
        this.currentDirectory = CanonicalizeExistingDirectory(currentDirectory ?? AppContext.BaseDirectory);
    }

    public IReadOnlyCollection<string> SupportedCapabilities { get; } = new[]
    {
        CapabilityNames.OpenFileExplorer, CapabilityNames.OpenFolder, CapabilityNames.OpenNotepad,
        CapabilityNames.OpenCalculator, CapabilityNames.OpenUrl, CapabilityNames.ListDirectory,
        CapabilityNames.GetCurrentDirectory, CapabilityNames.ListRunningProcesses,
    };

    public Task<CapabilityResult> ExecuteAsync(CapabilityRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var arguments = SanitizeArguments(request.Arguments);
        CapabilityResult result;
        try
        {
            result = request.Name switch
            {
                CapabilityNames.OpenFileExplorer => OpenExplorer(),
                CapabilityNames.OpenFolder => OpenFolder(request.Arguments),
                CapabilityNames.OpenNotepad => OpenApplication(CapabilityNames.OpenNotepad, "notepad.exe"),
                CapabilityNames.OpenCalculator => OpenApplication(CapabilityNames.OpenCalculator, "calc.exe"),
                CapabilityNames.OpenUrl => OpenUrl(request.Arguments),
                CapabilityNames.ListDirectory => ListDirectory(request.Arguments),
                CapabilityNames.GetCurrentDirectory => Success(CapabilityNames.GetCurrentDirectory, "Current directory returned.", new Dictionary<string, object?> { ["path"] = currentDirectory }),
                CapabilityNames.ListRunningProcesses => ListRunningProcesses(),
                _ => Failure(request.Name, "Capability is not connected yet."),
            };
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            result = Failure(request.Name, "Capability request failed.", ex.Message);
        }

        auditSink.Write(new CapabilityAuditEvent(DateTimeOffset.UtcNow, request.Name, arguments, result.Success, result.Error));
        return Task.FromResult(result);
    }

    private CapabilityResult OpenExplorer()
    {
        host.OpenFolder(currentDirectory);
        return Success(CapabilityNames.OpenFileExplorer, "File Explorer opened.", new Dictionary<string, object?> { ["path"] = currentDirectory });
    }

    private CapabilityResult OpenFolder(IReadOnlyDictionary<string, string>? arguments)
    {
        var path = RequireDirectory(arguments);
        host.OpenFolder(path);
        return Success(CapabilityNames.OpenFolder, "Folder opened.", new Dictionary<string, object?> { ["path"] = path });
    }

    private CapabilityResult OpenApplication(string capability, string executable)
    {
        host.OpenApplication(executable);
        return Success(capability, $"{executable} opened.");
    }

    private CapabilityResult OpenUrl(IReadOnlyDictionary<string, string>? arguments)
    {
        var value = RequireArgument(arguments, "url");
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Failure(CapabilityNames.OpenUrl, "Only http and https URLs are allowed.");
        }

        // The host is only reached after the allow-list check; no custom URI scheme is dispatched.
        host.OpenUrl(uri);
        return Success(CapabilityNames.OpenUrl, "URL opened.", new Dictionary<string, object?> { ["url"] = uri.AbsoluteUri });
    }

    private CapabilityResult ListDirectory(IReadOnlyDictionary<string, string>? arguments)
    {
        var path = RequireDirectory(arguments);
        var entries = Directory.EnumerateFileSystemEntries(path)
            .Where(IsSafeDirectoryEntry)
            .Select(entry => new Dictionary<string, object?>
            {
                ["name"] = Path.GetFileName(entry),
                ["kind"] = Directory.Exists(entry) ? "directory" : "file",
            })
            .OrderBy(entry => (string)entry["name"]!, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return Success(CapabilityNames.ListDirectory, "Directory listed.", new Dictionary<string, object?>
        {
            ["path"] = path,
            ["entries"] = entries,
        });
    }

    private CapabilityResult ListRunningProcesses()
    {
        var processes = host.ListProcesses()
            .Select(process => new Dictionary<string, object?> { ["name"] = process.Name, ["pid"] = process.Id })
            .ToArray();
        return Success(CapabilityNames.ListRunningProcesses, "Running processes listed.", new Dictionary<string, object?> { ["processes"] = processes });
    }

    private static bool IsSafeDirectoryEntry(string entry)
    {
        var attributes = File.GetAttributes(entry);
        var name = Path.GetFileName(entry);
        return (attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0 &&
               !SensitiveFileNames.Contains(name) &&
               !IsSensitiveFileName(name);
    }

    private static string RequireDirectory(IReadOnlyDictionary<string, string>? arguments) =>
        CanonicalizeExistingDirectory(RequireArgument(arguments, "path"));

    private static string RequireArgument(IReadOnlyDictionary<string, string>? arguments, string name)
    {
        if (arguments is null || !arguments.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Missing required argument: {name}.");
        }
        return value;
    }

    private static string CanonicalizeExistingDirectory(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Directory does not exist: {fullPath}");
        }
        return Path.TrimEndingDirectorySeparator(fullPath);
    }

    private static IReadOnlyDictionary<string, string> SanitizeArguments(IReadOnlyDictionary<string, string>? arguments) =>
        (arguments ?? new Dictionary<string, string>())
            .ToDictionary(pair => pair.Key, pair => IsSensitiveKey(pair.Key) ? "[redacted]" : SanitizeValue(pair.Value));

    private static string SanitizeValue(string value)
    {
        if (value.Contains("token=", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("secret=", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("password=", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("credential=", StringComparison.OrdinalIgnoreCase))
        {
            return "[redacted]";
        }
        return value.Length > 512 ? value[..512] + "…" : value;
    }

    private static bool IsSensitiveKey(string key) =>
        key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("credential", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("authorization", StringComparison.OrdinalIgnoreCase);

    private static bool IsSensitiveFileName(string name) =>
        name.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("credential", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("token", StringComparison.OrdinalIgnoreCase);

    private static CapabilityResult Success(string capability, string message, IReadOnlyDictionary<string, object?>? data = null) =>
        new(true, capability, message, data);

    private static CapabilityResult Failure(string capability, string message, string? error = null) =>
        new(false, capability, message, Error: error ?? message);
}
