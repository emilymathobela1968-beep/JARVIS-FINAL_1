namespace Jarvis.Computer;

public sealed class ComputerControlService(
    ApplicationOperator applications,
    UiAutomationOperator uiAutomation,
    FileSystemOperator fileSystem,
    ClipboardOperator clipboard,
    BrowserOperator browser,
    SystemOperator system)
{
    public Task<ComputerOperationResult> ExecuteAsync(ComputerOperation operation, CancellationToken cancellationToken = default) =>
        operation.Kind switch
        {
            ComputerOperationKind.Application => applications.ExecuteAsync(operation, cancellationToken),
            ComputerOperationKind.UiAutomation => uiAutomation.ExecuteAsync(operation),
            ComputerOperationKind.FileSystem => fileSystem.ExecuteAsync(operation, cancellationToken),
            ComputerOperationKind.Clipboard => clipboard.ExecuteAsync(operation),
            ComputerOperationKind.Browser => browser.ExecuteAsync(operation),
            ComputerOperationKind.System => system.ExecuteAsync(operation),
            _ => Task.FromResult(new ComputerOperationResult(operation.OperationId, operation.Kind, operation.Action, ComputerOperationStatus.Unsupported, "Unsupported computer operation kind.", ComputerRiskClass.ClassA, FailureReason: "unsupported_computer_kind"))
        };
}

public sealed class InMemoryClipboardHost : IClipboardHost
{
    private string? text;
    public string? ReadText() => text;
    public void WriteText(string text) => this.text = text;
}

public sealed class ComputerControlFactory
{
    public static ComputerControlService CreateDefault(IEnumerable<string> allowedFileRoots, IComputerAudit audit)
    {
        var policy = new ComputerPolicy();
        return new(
            new ApplicationOperator(new WindowsApplicationCatalog(), new WindowsApplicationHost(), policy, audit),
            new UiAutomationOperator(new UnavailableUiAutomationProvider(), policy, audit),
            new FileSystemOperator(allowedFileRoots, policy, audit),
            new ClipboardOperator(new InMemoryClipboardHost(), policy, audit),
            new BrowserOperator(policy, audit),
            new SystemOperator(policy, audit));
    }
}
