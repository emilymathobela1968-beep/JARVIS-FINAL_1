namespace Jarvis.Computer;

public interface IUiAutomationProvider
{
    ComputerOperationResult Execute(ComputerOperation operation, ComputerRiskClass risk);
}

public sealed class UnavailableUiAutomationProvider : IUiAutomationProvider
{
    public ComputerOperationResult Execute(ComputerOperation operation, ComputerRiskClass risk) =>
        new(operation.OperationId, operation.Kind, operation.Action, ComputerOperationStatus.Unsupported, "UI Automation provider is not attached in this build.", risk, FailureReason: "ui_automation_provider_unavailable");
}

public sealed class UiAutomationOperator(IUiAutomationProvider provider, ComputerPolicy policy, IComputerAudit audit)
{
    public Task<ComputerOperationResult> ExecuteAsync(ComputerOperation operation)
    {
        audit.Write(operation.OperationId, "uia_request", new { operation.Action, arguments = ComputerText.RedactArguments(operation.Arguments) });
        if (policy.Validate(operation) is { } rejected) return Task.FromResult(rejected);
        var result = provider.Execute(operation, policy.Classify(operation));
        audit.Write(operation.OperationId, "uia_result", new { result.Status, result.FailureReason });
        return Task.FromResult(result);
    }
}

public interface IClipboardHost
{
    string? ReadText();
    void WriteText(string text);
}

public sealed class ClipboardOperator(IClipboardHost host, ComputerPolicy policy, IComputerAudit audit)
{
    public Task<ComputerOperationResult> ExecuteAsync(ComputerOperation operation)
    {
        audit.Write(operation.OperationId, "clipboard_request", new { operation.Action, arguments = ComputerText.RedactArguments(operation.Arguments) });
        if (policy.Validate(operation) is { } rejected) return Task.FromResult(rejected);
        var risk = policy.Classify(operation);
        if (operation.Action.Equals("read", StringComparison.OrdinalIgnoreCase))
        {
            var text = host.ReadText() ?? string.Empty;
            var secret = ComputerText.ContainsSecretMarker(text);
            return Task.FromResult(new ComputerOperationResult(operation.OperationId, operation.Kind, operation.Action, ComputerOperationStatus.Succeeded, "Clipboard read.", risk, Evidence: new Dictionary<string, string> { ["length"] = text.Length.ToString(), ["containsSecretLikeContent"] = secret.ToString() }));
        }
        if (operation.Action.Equals("write", StringComparison.OrdinalIgnoreCase))
        {
            var text = operation.Arguments.GetValueOrDefault("text", string.Empty);
            if (ComputerText.ContainsSecretMarker(text))
                return Task.FromResult(ComputerPolicy.Failure(operation, risk, ComputerOperationStatus.Rejected, "Secret-like clipboard write rejected.", "secret_material_blocked"));
            host.WriteText(text);
            return Task.FromResult(new ComputerOperationResult(operation.OperationId, operation.Kind, operation.Action, ComputerOperationStatus.Succeeded, "Clipboard written.", risk, Evidence: new Dictionary<string, string> { ["length"] = text.Length.ToString() }));
        }
        return Task.FromResult(ComputerPolicy.Failure(operation, risk, ComputerOperationStatus.Unsupported, "Unsupported clipboard action.", "unsupported_clipboard_action"));
    }
}

public sealed class BrowserOperator(ComputerPolicy policy, IComputerAudit audit)
{
    public Task<ComputerOperationResult> ExecuteAsync(ComputerOperation operation)
    {
        audit.Write(operation.OperationId, "browser_request", new { operation.Action, arguments = ComputerText.RedactArguments(operation.Arguments) });
        if (policy.Validate(operation) is { } rejected) return Task.FromResult(rejected);
        var risk = policy.Classify(operation);
        var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "open_url", "search", "back", "forward", "refresh", "new_tab", "close_tab", "inspect_page", "click", "type", "scroll", "form_fill" };
        if (!supported.Contains(operation.Action))
            return Task.FromResult(ComputerPolicy.Failure(operation, risk, ComputerOperationStatus.Unsupported, "Unsupported browser action.", "unsupported_browser_action"));
        if (operation.Arguments.Any(kvp => kvp.Key.Contains("password", StringComparison.OrdinalIgnoreCase) || kvp.Key.Contains("cookie", StringComparison.OrdinalIgnoreCase)))
            return Task.FromResult(ComputerPolicy.Failure(operation, risk, ComputerOperationStatus.Rejected, "Browser password/cookie/token handling is forbidden.", "browser_secret_boundary"));
        return Task.FromResult(new ComputerOperationResult(operation.OperationId, operation.Kind, operation.Action, ComputerOperationStatus.ProviderUnavailable, "Browser automation provider is not attached.", risk, FailureReason: "browser_provider_unavailable", Evidence: ComputerText.RedactArguments(operation.Arguments)));
    }
}

public sealed class SystemOperator(ComputerPolicy policy, IComputerAudit audit)
{
    public Task<ComputerOperationResult> ExecuteAsync(ComputerOperation operation)
    {
        audit.Write(operation.OperationId, "system_request", new { operation.Action, arguments = ComputerText.RedactArguments(operation.Arguments) });
        if (policy.Validate(operation) is { } rejected) return Task.FromResult(rejected);
        var risk = policy.Classify(operation);
        if (operation.Action.Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => d.Name).ToArray();
            return Task.FromResult(new ComputerOperationResult(operation.OperationId, operation.Kind, operation.Action, ComputerOperationStatus.Succeeded, "System status read.", risk, Evidence: new Dictionary<string, string>
            {
                ["machine"] = Environment.MachineName,
                ["os"] = Environment.OSVersion.VersionString,
                ["processorCount"] = Environment.ProcessorCount.ToString(),
                ["readyDrives"] = string.Join(";", drives)
            }));
        }
        return Task.FromResult(ComputerPolicy.Failure(operation, risk, ComputerOperationStatus.Unsupported, "Unsupported system action.", "unsupported_system_action"));
    }
}
