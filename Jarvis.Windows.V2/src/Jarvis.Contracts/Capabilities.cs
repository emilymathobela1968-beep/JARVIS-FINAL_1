namespace Jarvis.Contracts;

public static class CapabilityNames
{
    public const string OpenFileExplorer = "open_file_explorer";
    public const string OpenFolder = "open_folder";
    public const string OpenNotepad = "open_notepad";
    public const string OpenCalculator = "open_calculator";
    public const string OpenUrl = "open_url";
    public const string ListDirectory = "list_directory";
    public const string GetCurrentDirectory = "get_current_directory";
    public const string ListRunningProcesses = "list_running_processes";
}

public sealed record CapabilityRequest(
    string Name,
    IReadOnlyDictionary<string, string>? Arguments = null);

public sealed record CapabilityResult(
    bool Success,
    string Capability,
    string Message,
    IReadOnlyDictionary<string, object?>? Data = null,
    string? Error = null);

public sealed record CapabilityAuditEvent(
    DateTimeOffset Timestamp,
    string Capability,
    IReadOnlyDictionary<string, string> Arguments,
    bool Success,
    string? Error);

public interface ICapabilityDispatcher
{
    IReadOnlyCollection<string> SupportedCapabilities { get; }

    Task<CapabilityResult> ExecuteAsync(CapabilityRequest request, CancellationToken cancellationToken = default);
}

public interface ICapabilityAuditSink
{
    void Write(CapabilityAuditEvent auditEvent);
}
