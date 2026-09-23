using System.Text.Json;
using Jarvis.Governance;

namespace Jarvis.Computer;

public enum ComputerRiskClass { ClassA, ClassB, ClassC }
public enum ComputerOperationStatus { Succeeded, Unsupported, Unavailable, ProviderUnavailable, Rejected, ConfirmationRequired, AuthorizationRequired, VerificationFailed, Failed, TimedOut, Cancelled }
public enum ComputerOperationKind { Application, UiAutomation, FileSystem, Clipboard, Browser, System }

public sealed record ComputerOperation(
    Guid OperationId,
    ComputerOperationKind Kind,
    string Action,
    IReadOnlyDictionary<string, string> Arguments,
    bool Confirmed = false);

public sealed record ComputerObservation(
    bool Available,
    string Summary,
    IReadOnlyDictionary<string, string>? Evidence = null);

public sealed record ComputerOperationResult(
    Guid OperationId,
    ComputerOperationKind Kind,
    string Action,
    ComputerOperationStatus Status,
    string Message,
    ComputerRiskClass RiskClass,
    ComputerObservation? Before = null,
    ComputerObservation? After = null,
    string? FailureReason = null,
    IReadOnlyDictionary<string, string>? Evidence = null);

public interface IComputerAudit
{
    void Write(Guid operationId, string name, object? data = null);
}

public sealed class MemoryComputerAudit : IComputerAudit
{
    public List<string> Entries { get; } = [];
    public void Write(Guid operationId, string name, object? data = null) =>
        Entries.Add(GovernanceText.RedactSecrets(JsonSerializer.Serialize(new { operationId, name, data })));
}

public sealed class JsonComputerAudit(string path) : IComputerAudit
{
    private readonly object sync = new();

    public void Write(Guid operationId, string name, object? data = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var line = GovernanceText.RedactSecrets(JsonSerializer.Serialize(new { timestamp = DateTimeOffset.UtcNow, operationId, name, data }));
        lock (sync)
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }
}

public static class ComputerText
{
    private static readonly string[] SecretMarkers = ["password", "secret", "token", "api_key", "apikey", "cookie", "authorization", "fish_audio_api_key", "azure_openai_api_key", "openai_api_key"];

    public static bool ContainsSecretMarker(string? value) =>
        !string.IsNullOrWhiteSpace(value) && SecretMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyDictionary<string, string> RedactArguments(IReadOnlyDictionary<string, string> arguments) =>
        arguments.ToDictionary(kvp => kvp.Key, kvp => ContainsSecretMarker(kvp.Key) || ContainsSecretMarker(kvp.Value) ? "<REDACTED>" : GovernanceText.RedactSecrets(kvp.Value), StringComparer.OrdinalIgnoreCase);
}
