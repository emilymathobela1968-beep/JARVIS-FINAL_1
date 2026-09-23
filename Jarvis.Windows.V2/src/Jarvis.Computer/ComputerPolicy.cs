namespace Jarvis.Computer;

public sealed class ComputerPolicy
{
    private static readonly HashSet<string> ClassCSystemActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "change_security_settings",
        "install_driver",
        "format_disk",
        "modify_firewall",
        "change_user_account",
        "elevate_privileges",
        "disable_antivirus"
    };

    private static readonly HashSet<string> ClassBFileActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "delete",
        "overwrite",
        "move",
        "rename"
    };

    private static readonly HashSet<string> ClassBBrowserActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "purchase",
        "payment",
        "submit_order",
        "download",
        "upload"
    };

    public ComputerRiskClass Classify(ComputerOperation operation)
    {
        if (operation.Kind == ComputerOperationKind.System && ClassCSystemActions.Contains(operation.Action))
            return ComputerRiskClass.ClassC;

        if (operation.Kind == ComputerOperationKind.FileSystem && ClassBFileActions.Contains(operation.Action))
            return ComputerRiskClass.ClassB;

        if (operation.Kind == ComputerOperationKind.Browser && ClassBBrowserActions.Contains(operation.Action))
            return ComputerRiskClass.ClassB;

        if (operation.Kind == ComputerOperationKind.Clipboard && operation.Action.Equals("write", StringComparison.OrdinalIgnoreCase))
            return ComputerRiskClass.ClassB;

        return ComputerRiskClass.ClassA;
    }

    public ComputerOperationResult? Validate(ComputerOperation operation)
    {
        var risk = Classify(operation);
        if (operation.Arguments.Any(kvp => ComputerText.ContainsSecretMarker(kvp.Key) || ComputerText.ContainsSecretMarker(kvp.Value)))
        {
            return Failure(operation, risk, ComputerOperationStatus.Rejected, "Operation rejected because it contains secret-like content.", "secret_material_blocked");
        }

        if (risk == ComputerRiskClass.ClassC)
        {
            return Failure(operation, risk, ComputerOperationStatus.Rejected, "Class C computer operation rejected.", "class_c_rejected");
        }

        if (risk == ComputerRiskClass.ClassB && !operation.Confirmed)
        {
            return Failure(operation, risk, ComputerOperationStatus.ConfirmationRequired, "This computer operation requires explicit confirmation.", "confirmation_required");
        }

        return null;
    }

    public static ComputerOperationResult Failure(ComputerOperation operation, ComputerRiskClass risk, ComputerOperationStatus status, string message, string reason) =>
        new(operation.OperationId, operation.Kind, operation.Action, status, message, risk, FailureReason: reason, Evidence: ComputerText.RedactArguments(operation.Arguments));
}
