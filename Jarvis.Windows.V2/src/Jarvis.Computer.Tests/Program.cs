using System.Text.Json;
using Jarvis.Computer;
using Jarvis.Governance;

var tests = new ComputerTests();
await tests.RunAll();

internal sealed class ComputerTests
{
    private int passed;
    private int failed;

    public async Task RunAll()
    {
        await Run("risk classifier separates A B C", RiskClassifierSeparatesABC);
        await Run("class B requires confirmation", ClassBRequiresConfirmation);
        await Run("class C is rejected", ClassCRejected);
        await Run("file path outside approved root rejected", FileOutsideRootRejected);
        await Run("file write read metadata cleans evidence", FileWriteReadMetadata);
        await Run("file delete preserves confirmation boundary", FileDeleteRequiresConfirmationThenVerifies);
        await Run("app launch observes acts verifies", AppLaunchObservesActsVerifies);
        await Run("app launch resolves aliases and verifies requested name", AppLaunchResolvesAliasesAndVerifiesRequestedName);
        await Run("app launch missing application returns app not found", AppLaunchMissingApplicationReturnsAppNotFound);
        await Run("app launch verification failure is not completed", AppLaunchVerificationFailureIsNotCompleted);
        await Run("packaged Calculator launch waits for observable app identity", PackagedCalculatorLaunchWaitsForObservableIdentity);
        await Run("explicit computer launch tool reaches application operator", ExplicitComputerLaunchToolReachesApplicationOperator);
        await Run("window actions route to host", WindowActionsRouteToHost);
        await Run("application close resolves human-readable aliases", ApplicationCloseResolvesHumanReadableAliases);
        await Run("application close reports not running", ApplicationCloseReportsNotRunning);
        await Run("application close rejects ambiguous instances", ApplicationCloseRejectsAmbiguousInstances);
        await Run("application close verifies graceful success", ApplicationCloseVerifiesGracefulSuccess);
        await Run("application close protects unsaved work", ApplicationCloseProtectsUnsavedWork);
        await Run("application close blocks Jarvis", ApplicationCloseBlocksJarvis);
        await Run("application close tool requires no PID", ApplicationCloseToolRequiresNoPid);
        await Run("application status remains non-mutating", ApplicationStatusRemainsNonMutating);
        await Run("visible window discovery preserves window identity", VisibleWindowDiscoveryPreservesIdentity);
        await Run("Explorer close targets window not shell process", ExplorerCloseTargetsWindowNotShellProcess);
        await Run("multiple Explorer windows are ambiguous", MultipleExplorerWindowsAreAmbiguous);
        await Run("qualified Explorer title resolves intended window", QualifiedExplorerTitleResolvesIntendedWindow);
        await Run("Explorer status observes visible windows", ExplorerStatusObservesVisibleWindows);
        await Run("human-readable focus targets visible window", HumanReadableFocusTargetsVisibleWindow);
        await Run("ui automation unavailable is explicit", UiAutomationUnavailableExplicit);
        await Run("clipboard write requires confirmation", ClipboardWriteRequiresConfirmation);
        await Run("clipboard secret write rejected", ClipboardSecretWriteRejected);
        await Run("browser purchase requires confirmation", BrowserPurchaseRequiresConfirmation);
        await Run("browser password boundary rejected", BrowserPasswordBoundaryRejected);
        await Run("system status succeeds", SystemStatusSucceeds);
        await Run("documents PDF find returns candidate evidence", DocumentsPdfFindReturnsCandidateEvidence);
        await Run("PDF read reports unavailable document reader", PdfReadReportsUnavailableDocumentReader);
        await Run("governed computer tool maps confirmation", GovernedComputerToolMapsConfirmation);
        await Run("audit redacts secret-like material", AuditRedactsSecretLikeMaterial);

        Console.WriteLine($"COMPUTER_TESTS_TOTAL passed={passed} failed={failed}");
        if (failed > 0) Environment.Exit(1);
    }

    private Task RiskClassifierSeparatesABC()
    {
        var policy = new ComputerPolicy();
        Assert(policy.Classify(Op(ComputerOperationKind.Application, "launch")) == ComputerRiskClass.ClassA, "launch should be class A");
        Assert(policy.Classify(Op(ComputerOperationKind.FileSystem, "delete")) == ComputerRiskClass.ClassB, "delete should be class B");
        Assert(policy.Classify(Op(ComputerOperationKind.System, "change_security_settings")) == ComputerRiskClass.ClassC, "security change should be class C");
        return Task.CompletedTask;
    }

    private Task ClassBRequiresConfirmation()
    {
        var result = new ComputerPolicy().Validate(Op(ComputerOperationKind.FileSystem, "delete"));
        Assert(result?.Status == ComputerOperationStatus.ConfirmationRequired, "class B should require confirmation");
        return Task.CompletedTask;
    }

    private Task ClassCRejected()
    {
        var result = new ComputerPolicy().Validate(Op(ComputerOperationKind.System, "disable_antivirus", confirmed: true));
        Assert(result?.Status == ComputerOperationStatus.Rejected, "class C should be rejected");
        Assert(result?.FailureReason == "class_c_rejected", "reason should be class_c_rejected");
        return Task.CompletedTask;
    }

    private async Task FileOutsideRootRejected()
    {
        var root = TempRoot();
        var op = Op(ComputerOperationKind.FileSystem, "metadata", new Dictionary<string, string> { ["path"] = Path.Combine(Path.GetTempPath(), "outside.txt") });
        var result = await FileOperator(root).ExecuteAsync(op);
        Assert(result.Status == ComputerOperationStatus.Rejected, "outside path should be rejected");
        Assert(result.FailureReason == "path_outside_approved_roots", "outside root reason expected");
    }

    private async Task FileWriteReadMetadata()
    {
        var root = TempRoot();
        var file = Path.Combine(root, "job.txt");
        var write = await FileOperator(root).ExecuteAsync(Op(ComputerOperationKind.FileSystem, "write", new Dictionary<string, string> { ["path"] = file, ["content"] = "done" }));
        var read = await FileOperator(root).ExecuteAsync(Op(ComputerOperationKind.FileSystem, "read", new Dictionary<string, string> { ["path"] = file }));
        var meta = await FileOperator(root).ExecuteAsync(Op(ComputerOperationKind.FileSystem, "metadata", new Dictionary<string, string> { ["path"] = file }));
        Assert(write.Status == ComputerOperationStatus.Succeeded, "write should succeed");
        Assert(read.Status == ComputerOperationStatus.Succeeded, "read should succeed");
        Assert(meta.Evidence!["length"] == "4", "metadata should include file length");
    }

    private async Task FileDeleteRequiresConfirmationThenVerifies()
    {
        var root = TempRoot();
        var file = Path.Combine(root, "delete-me.txt");
        await File.WriteAllTextAsync(file, "x");
        var pending = await FileOperator(root).ExecuteAsync(Op(ComputerOperationKind.FileSystem, "delete", new Dictionary<string, string> { ["path"] = file }));
        var deleted = await FileOperator(root).ExecuteAsync(Op(ComputerOperationKind.FileSystem, "delete", new Dictionary<string, string> { ["path"] = file }, confirmed: true));
        Assert(pending.Status == ComputerOperationStatus.ConfirmationRequired, "delete should require confirmation");
        Assert(deleted.Status == ComputerOperationStatus.Succeeded, "confirmed delete should succeed");
        Assert(!File.Exists(file), "file should be removed");
    }

    private async Task AppLaunchObservesActsVerifies()
    {
        var host = new FakeApplicationHost();
        var op = new ApplicationOperator(new FakeCatalog(), host, new ComputerPolicy(), new MemoryComputerAudit());
        var result = await op.ExecuteAsync(Op(ComputerOperationKind.Application, "launch", new Dictionary<string, string> { ["name"] = "notepad" }));
        Assert(result.Status == ComputerOperationStatus.Succeeded, "launch should succeed");
        Assert(host.Launched == "C:\\Windows\\notepad.exe", "catalog path should be launched");
        Assert(result.After!.Available, "after observation should be available");
    }

    private async Task AppLaunchResolvesAliasesAndVerifiesRequestedName()
    {
        var host = new FakeApplicationHost();
        var op = new ApplicationOperator(new FakeCatalog(), host, new ComputerPolicy(), new MemoryComputerAudit());
        var result = await op.ExecuteAsync(Op(ComputerOperationKind.Application, "launch", new Dictionary<string, string> { ["name"] = "Microsoft Word" }));

        Assert(result.Status == ComputerOperationStatus.Succeeded, "Microsoft Word alias should resolve and verify");
        Assert(host.Launched == "C:\\Program Files\\Microsoft Office\\root\\Office16\\WINWORD.EXE", "Word executable from catalog should be launched");
        Assert(result.Evidence!["resolvedName"] == "Microsoft Word", "resolved application name should be evidenced");
    }

    private async Task AppLaunchMissingApplicationReturnsAppNotFound()
    {
        var host = new FakeApplicationHost();
        var op = new ApplicationOperator(new FakeCatalog(), host, new ComputerPolicy(), new MemoryComputerAudit());
        var result = await op.ExecuteAsync(Op(ComputerOperationKind.Application, "launch", new Dictionary<string, string> { ["name"] = "Definitely Missing App" }));

        Assert(result.Status == ComputerOperationStatus.Unavailable, "missing app should be unavailable");
        Assert(result.FailureReason == "application_not_found", "missing app reason should be explicit");
        Assert(host.Launched is null, "missing app must not execute launch");
    }

    private async Task AppLaunchVerificationFailureIsNotCompleted()
    {
        var host = new FakeApplicationHost { VerificationAvailable = false };
        var op = new ApplicationOperator(new FakeCatalog(), host, new ComputerPolicy(), new MemoryComputerAudit());
        var result = await op.ExecuteAsync(Op(ComputerOperationKind.Application, "launch", new Dictionary<string, string> { ["name"] = "notepad" }));

        Assert(result.Status == ComputerOperationStatus.VerificationFailed, "unverified launch must not be completed");
        Assert(result.FailureReason == "launch_verification_failed", "verification failure reason should be explicit");
        Assert(host.Launched == "C:\\Windows\\notepad.exe", "launch attempt should still be evidenced");
    }

    private async Task PackagedCalculatorLaunchWaitsForObservableIdentity()
    {
        var host = new FakeApplicationHost { VerificationObservationsBeforeAvailable = 3 };
        var result = await Operator(host).ExecuteAsync(Op(ComputerOperationKind.Application, "launch", new Dictionary<string, string> { ["name"] = "Calculator" }));

        Assert(result.Status == ComputerOperationStatus.Succeeded, "Calculator should succeed when CalculatorApp becomes observable within the bounded window");
        Assert(host.PostLaunchObservationCount >= 4, "verification should poll until the packaged Calculator identity appears");
        Assert(host.Launched == "C:\\Windows\\System32\\calc.exe", "Calculator should keep using the existing launcher");
    }

    private async Task ExplicitComputerLaunchToolReachesApplicationOperator()
    {
        var host = new FakeApplicationHost();
        var op = new ApplicationOperator(new FakeCatalog(), host, new ComputerPolicy(), new MemoryComputerAudit());
        var tool = new ComputerLaunchApplicationGovernedTool((operation, _) => op.ExecuteAsync(operation));
        var result = await tool.InvokeAsync(new(Guid.NewGuid(), Guid.NewGuid(), ComputerLaunchApplicationGovernedTool.Name, JsonSerializer.SerializeToElement(new { name = "Calculator" })));

        Assert(result.Status == GovernedToolStatus.Succeeded, "explicit launch tool should complete through Computer abstraction");
        Assert(host.Launched == "C:\\Windows\\System32\\calc.exe", "explicit launch tool should launch resolved Calculator");
    }

    private async Task WindowActionsRouteToHost()
    {
        var host = new FakeApplicationHost();
        var op = new ApplicationOperator(new FakeCatalog(), host, new ComputerPolicy(), new MemoryComputerAudit());
        var result = await op.ExecuteAsync(Op(ComputerOperationKind.Application, "maximize", new Dictionary<string, string> { ["process_id"] = "7" }));
        Assert(result.Status == ComputerOperationStatus.Succeeded, "window action should succeed");
        Assert(host.LastWindowAction == "maximize", "window action should be recorded");
    }

    private async Task ApplicationCloseResolvesHumanReadableAliases()
    {
        foreach (var (request, process) in new[]
        {
            ("Word", "WINWORD"),
            ("Microsoft Word", "WINWORD"),
            ("Excel", "EXCEL"),
            ("Microsoft Excel", "EXCEL"),
            ("Notepad", "notepad"),
            ("Calculator", "CalculatorApp")
        })
        {
            var host = new FakeApplicationHost([new(17, process, request)]);
            var result = await Operator(host).ExecuteAsync(Op(ComputerOperationKind.Application, "close", new Dictionary<string, string> { ["name"] = request }));
            Assert(result.Status == ComputerOperationStatus.Succeeded, $"{request} should resolve and close");
            Assert(host.LastWindowProcessId == 17, $"{request} should target the resolved process");
        }
    }

    private async Task ApplicationCloseReportsNotRunning()
    {
        var host = new FakeApplicationHost([]);
        var result = await Operator(host).ExecuteAsync(Op(ComputerOperationKind.Application, "close", new Dictionary<string, string> { ["name"] = "Microsoft Word" }));
        Assert(result.Status == ComputerOperationStatus.Unavailable, "missing target should report unavailable");
        Assert(result.FailureReason == "application_not_running", "missing target should report not running");
        Assert(host.LastWindowAction is null, "not-running target must not receive a close request");
    }

    private async Task ApplicationCloseRejectsAmbiguousInstances()
    {
        var host = new FakeApplicationHost([new(17, "WINWORD", "One - Word"), new(18, "WINWORD", "Two - Word")]);
        var result = await Operator(host).ExecuteAsync(Op(ComputerOperationKind.Application, "close", new Dictionary<string, string> { ["name"] = "Word" }));
        Assert(result.Status == ComputerOperationStatus.ConfirmationRequired, "multiple targets should be ambiguous");
        Assert(result.FailureReason == "ambiguous_application_instances", "ambiguity reason should be explicit");
        Assert(host.LastWindowAction is null, "ambiguous targets must not be closed");
    }

    private async Task ApplicationCloseVerifiesGracefulSuccess()
    {
        var host = new FakeApplicationHost([new(17, "notepad", "Untitled - Notepad")]);
        var result = await Operator(host).ExecuteAsync(Op(ComputerOperationKind.Application, "close", new Dictionary<string, string> { ["name"] = "Notepad" }));
        Assert(result.Status == ComputerOperationStatus.Succeeded, "closed process should succeed");
        Assert(result.After?.Summary == "not_running", "success should verify the process disappeared");
        Assert(!host.ForceKillCalled, "graceful close must never force kill");
    }

    private async Task ApplicationCloseProtectsUnsavedWork()
    {
        var host = new FakeApplicationHost([new(17, "notepad", "Untitled - Notepad")]) { KeepRunningAfterClose = true };
        var result = await Operator(host).ExecuteAsync(Op(ComputerOperationKind.Application, "close", new Dictionary<string, string> { ["name"] = "Notepad" }));
        Assert(result.Status == ComputerOperationStatus.ConfirmationRequired, "remaining process should require user action");
        Assert(result.FailureReason == "user_action_required", "unsaved-work boundary should be explicit");
        Assert(!host.ForceKillCalled, "pending close must never force kill");
    }

    private async Task ApplicationCloseBlocksJarvis()
    {
        var host = new FakeApplicationHost([new(17, "Jarvis.App", "JARVIS V2")]);
        var result = await Operator(host).ExecuteAsync(Op(ComputerOperationKind.Application, "close", new Dictionary<string, string> { ["name"] = "Jarvis" }));
        Assert(result.Status == ComputerOperationStatus.Rejected, "Jarvis close should be blocked");
        Assert(result.FailureReason == "protected_application", "protected reason should be explicit");
        Assert(host.LastWindowAction is null, "protected process must not receive close");
    }

    private async Task ApplicationCloseToolRequiresNoPid()
    {
        var host = new FakeApplicationHost([new(17, "WINWORD", "Document - Word")]);
        var tool = new ComputerGovernedTool((operation, _) => Operator(host).ExecuteAsync(operation));
        var result = await tool.InvokeAsync(new(Guid.NewGuid(), Guid.NewGuid(), "computer_operation", JsonSerializer.SerializeToElement(new { kind = "Application", action = "close", name = "Microsoft Word" })));
        Assert(result.Status == GovernedToolStatus.Succeeded, "close by name should pass through governed tool");
        Assert(host.LastWindowProcessId == 17, "native layer should discover PID");
    }

    private async Task ApplicationStatusRemainsNonMutating()
    {
        var host = new FakeApplicationHost([new(17, "notepad", "Untitled - Notepad")]);
        var result = await Operator(host).ExecuteAsync(Op(ComputerOperationKind.Application, "running", new Dictionary<string, string> { ["name"] = "Notepad" }));
        Assert(result.Status == ComputerOperationStatus.Succeeded, "status query should succeed");
        Assert(host.LastWindowAction is null, "status query must not mutate the application");
        Assert(host.Launched is null, "status query must not launch the application");
    }

    private Task VisibleWindowDiscoveryPreservesIdentity()
    {
        var host = new FakeApplicationHost([new(13044, "explorer", "Home")]);
        var window = host.VisibleWindows().Single();
        Assert(window.WindowHandle != 0, "visible window should preserve HWND identity");
        Assert(window.ProcessId == 13044, "visible window should preserve process identity separately");
        Assert(window.Title == "Home", "visible window should preserve title identity");
        return Task.CompletedTask;
    }

    private async Task ExplorerCloseTargetsWindowNotShellProcess()
    {
        var host = new FakeApplicationHost([new(13044, "explorer", "Home")]);
        var result = await Operator(host).ExecuteAsync(Op(ComputerOperationKind.Application, "close", new Dictionary<string, string> { ["name"] = "File Explorer" }));
        Assert(result.Status == ComputerOperationStatus.Succeeded, "single Explorer window should close");
        Assert(host.LastWindowHandle is not null, "Explorer close should target an HWND");
        Assert(host.Running().Any(process => process.ProcessId == 13044), "Explorer shell process must remain running");
        Assert(!host.ForceKillCalled, "Explorer shell must never be terminated");
    }

    private async Task MultipleExplorerWindowsAreAmbiguous()
    {
        var host = new FakeApplicationHost([new(13044, "explorer", "Home"), new(13044, "explorer", "Downloads")]);
        var result = await Operator(host).ExecuteAsync(Op(ComputerOperationKind.Application, "close", new Dictionary<string, string> { ["name"] = "File Explorer" }));
        Assert(result.Status == ComputerOperationStatus.ConfirmationRequired, "multiple Explorer windows should be ambiguous");
        Assert(result.FailureReason == "ambiguous_application_instances", "Explorer ambiguity should be explicit");
        Assert(host.LastWindowHandle is null, "ambiguous Explorer request must not close a window");
    }

    private async Task QualifiedExplorerTitleResolvesIntendedWindow()
    {
        var host = new FakeApplicationHost([new(13044, "explorer", "Home"), new(13044, "explorer", "Downloads")]);
        var downloadsHandle = host.VisibleWindows().Single(window => window.Title == "Downloads").WindowHandle;
        var result = await Operator(host).ExecuteAsync(Op(ComputerOperationKind.Application, "close", new Dictionary<string, string> { ["name"] = "Downloads File Explorer window" }));
        Assert(result.Status == ComputerOperationStatus.Succeeded, "qualified Explorer title should resolve");
        Assert(host.LastWindowHandle == downloadsHandle, "only the Downloads window should be targeted");
        Assert(host.VisibleWindows().Any(window => window.Title == "Home"), "unrelated Explorer window should remain");
    }

    private async Task ExplorerStatusObservesVisibleWindows()
    {
        var host = new FakeApplicationHost([new(13044, "explorer", "Home")]);
        var result = await Operator(host).ExecuteAsync(Op(ComputerOperationKind.Application, "running", new Dictionary<string, string> { ["name"] = "File Explorer" }));
        Assert(result.Status == ComputerOperationStatus.Succeeded, "Explorer status should succeed");
        Assert(result.After?.Available == true, "Explorer status should observe the visible window");
        Assert(host.LastWindowAction is null, "status must not mutate Explorer");
    }

    private async Task HumanReadableFocusTargetsVisibleWindow()
    {
        var host = new FakeApplicationHost([new(17, "notepad", "Untitled - Notepad")]);
        var expectedHandle = host.VisibleWindows().Single().WindowHandle;
        var result = await Operator(host).ExecuteAsync(Op(ComputerOperationKind.Application, "focus", new Dictionary<string, string> { ["name"] = "Notepad" }));
        Assert(result.Status == ComputerOperationStatus.Succeeded, "focus by human-readable name should succeed");
        Assert(host.LastWindowHandle == expectedHandle, "focus should target the resolved visible window");
    }

    private async Task UiAutomationUnavailableExplicit()
    {
        var result = await new UiAutomationOperator(new UnavailableUiAutomationProvider(), new ComputerPolicy(), new MemoryComputerAudit()).ExecuteAsync(Op(ComputerOperationKind.UiAutomation, "inspect_tree"));
        Assert(result.Status == ComputerOperationStatus.Unsupported, "UIA should report unsupported without provider");
        Assert(result.FailureReason == "ui_automation_provider_unavailable", "UIA reason should be explicit");
    }

    private async Task ClipboardWriteRequiresConfirmation()
    {
        var result = await new ClipboardOperator(new InMemoryClipboardHost(), new ComputerPolicy(), new MemoryComputerAudit()).ExecuteAsync(Op(ComputerOperationKind.Clipboard, "write", new Dictionary<string, string> { ["text"] = "hello" }));
        Assert(result.Status == ComputerOperationStatus.ConfirmationRequired, "clipboard write should require confirmation");
    }

    private async Task ClipboardSecretWriteRejected()
    {
        var result = await new ClipboardOperator(new InMemoryClipboardHost(), new ComputerPolicy(), new MemoryComputerAudit()).ExecuteAsync(Op(ComputerOperationKind.Clipboard, "write", new Dictionary<string, string> { ["text"] = "token=abc" }, confirmed: true));
        Assert(result.Status == ComputerOperationStatus.Rejected, "secret write should be rejected");
    }

    private async Task BrowserPurchaseRequiresConfirmation()
    {
        var result = await new BrowserOperator(new ComputerPolicy(), new MemoryComputerAudit()).ExecuteAsync(Op(ComputerOperationKind.Browser, "purchase"));
        Assert(result.Status == ComputerOperationStatus.ConfirmationRequired, "purchase should require confirmation");
    }

    private async Task BrowserPasswordBoundaryRejected()
    {
        var result = await new BrowserOperator(new ComputerPolicy(), new MemoryComputerAudit()).ExecuteAsync(Op(ComputerOperationKind.Browser, "form_fill", new Dictionary<string, string> { ["password"] = "abc" }, confirmed: true));
        Assert(result.Status == ComputerOperationStatus.Rejected, "password field should be rejected");
    }

    private async Task SystemStatusSucceeds()
    {
        var result = await new SystemOperator(new ComputerPolicy(), new MemoryComputerAudit()).ExecuteAsync(Op(ComputerOperationKind.System, "status"));
        Assert(result.Status == ComputerOperationStatus.Succeeded, "system status should succeed");
        Assert(result.Evidence!.ContainsKey("processorCount"), "processor count should be present");
    }

    private async Task DocumentsPdfFindReturnsCandidateEvidence()
    {
        var root = TempRoot();
        var docs = Path.Combine(root, "Documents");
        Directory.CreateDirectory(docs);
        var pdf = Path.Combine(docs, "first.pdf");
        await File.WriteAllTextAsync(pdf, "%PDF-test");
        var result = await FileOperator(root).ExecuteAsync(Op(ComputerOperationKind.FileSystem, "find", new Dictionary<string, string> { ["path"] = docs, ["extension"] = "pdf" }));

        Assert(result.Status == ComputerOperationStatus.Succeeded, "PDF find should succeed inside approved root");
        Assert(result.Evidence!["matches"] == "1", "PDF find should report one match");
        Assert(result.Evidence["match1"] == pdf, "PDF find should return candidate path");
    }

    private async Task PdfReadReportsUnavailableDocumentReader()
    {
        var root = TempRoot();
        var pdf = Path.Combine(root, "manual.pdf");
        await File.WriteAllTextAsync(pdf, "%PDF-test");
        var result = await FileOperator(root).ExecuteAsync(Op(ComputerOperationKind.FileSystem, "read", new Dictionary<string, string> { ["path"] = pdf }));

        Assert(result.Status == ComputerOperationStatus.ProviderUnavailable, "PDF read should not fake extraction");
        Assert(result.FailureReason == "document_reader_unavailable", "PDF unavailable reason should be explicit");
    }

    private async Task GovernedComputerToolMapsConfirmation()
    {
        var tool = new ComputerGovernedTool((operation, _) => Task.FromResult(new ComputerOperationResult(operation.OperationId, operation.Kind, operation.Action, ComputerOperationStatus.ConfirmationRequired, "confirm", ComputerRiskClass.ClassB, FailureReason: "confirmation_required")));
        var result = await tool.InvokeAsync(new(Guid.NewGuid(), Guid.NewGuid(), "computer_operation", JsonSerializer.SerializeToElement(new { kind = "FileSystem", action = "delete", path = "C:\\x" })));
        Assert(result.Status == GovernedToolStatus.AuthorizationRequired, "confirmation should map to authorization required");
    }

    private Task AuditRedactsSecretLikeMaterial()
    {
        var audit = new MemoryComputerAudit();
        audit.Write(Guid.NewGuid(), "test", new { value = "FISH_AUDIO_API_KEY=abc123" });
        Assert(!audit.Entries[0].Contains("abc123", StringComparison.Ordinal), "audit should redact secret value");
        return Task.CompletedTask;
    }

    private static FileSystemOperator FileOperator(string root) => new([root], new ComputerPolicy(), new MemoryComputerAudit());

    private static ApplicationOperator Operator(FakeApplicationHost host) => new(new FakeCatalog(), host, new ComputerPolicy(), new MemoryComputerAudit());

    private static ComputerOperation Op(ComputerOperationKind kind, string action, IReadOnlyDictionary<string, string>? args = null, bool confirmed = false) =>
        new(Guid.NewGuid(), kind, action, args ?? new Dictionary<string, string>(), confirmed);

    private static string TempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "jarvis-computer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private async Task Run(string name, Func<Task> test)
    {
        try
        {
            await test();
            passed++;
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            failed++;
            Console.WriteLine($"FAIL {name}: {ex.Message}");
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakeCatalog : IApplicationCatalog
    {
        public IReadOnlyList<ApplicationIdentity> Discover() =>
        [
            new("notepad", "C:\\Windows\\notepad.exe", "fake"),
            new("Microsoft Word", "C:\\Program Files\\Microsoft Office\\root\\Office16\\WINWORD.EXE", "fake"),
            new("Microsoft Excel", "C:\\Program Files\\Microsoft Office\\root\\Office16\\EXCEL.EXE", "fake"),
            new("Calculator", "C:\\Windows\\System32\\calc.exe", "fake")
        ];
    }

    private sealed class FakeApplicationHost : IApplicationHost
    {
        private readonly List<RunningApplication> running;
        private readonly List<VisibleApplicationWindow> windows;

        public FakeApplicationHost() : this([new(7, "notepad", "Untitled - Notepad")]) { }
        public FakeApplicationHost(IEnumerable<RunningApplication> running)
        {
            this.running = [.. running];
            windows = this.running.Select((application, index) => new VisibleApplicationWindow(
                application.ProcessId * 100L + index + 1,
                application.ProcessId,
                application.Name,
                application.MainWindowTitle ?? application.Name,
                application.Name.Equals("explorer", StringComparison.OrdinalIgnoreCase) ? "CabinetWClass" : "TestWindowClass")).ToList();
        }

        public string? Launched { get; private set; }
        public string? LastWindowAction { get; private set; }
        public int? LastWindowProcessId { get; private set; }
        public long? LastWindowHandle { get; private set; }
        public bool VerificationAvailable { get; init; } = true;
        public int VerificationObservationsBeforeAvailable { get; init; }
        public int PostLaunchObservationCount { get; private set; }
        public bool KeepRunningAfterClose { get; init; }
        public bool ForceKillCalled { get; private set; }
        public ComputerObservation Observe(string nameOrPath)
        {
            if (Launched is not null)
            {
                PostLaunchObservationCount++;
                return new(VerificationAvailable && PostLaunchObservationCount > VerificationObservationsBeforeAvailable, "observed");
            }

            return new(running.Any(application => application.ProcessId.ToString() == nameOrPath || application.Name.Contains(nameOrPath, StringComparison.OrdinalIgnoreCase) || (application.MainWindowTitle?.Contains(nameOrPath, StringComparison.OrdinalIgnoreCase) ?? false)), "observed");
        }
        public ComputerOperationStatus Launch(string executablePath, out string evidence) { Launched = executablePath; evidence = "7"; return ComputerOperationStatus.Succeeded; }
        public ComputerOperationStatus Focus(int processId) => ComputerOperationStatus.Succeeded;
        public ComputerOperationStatus Window(int processId, string action)
        {
            LastWindowProcessId = processId;
            LastWindowAction = action;
            if (action.Equals("close", StringComparison.OrdinalIgnoreCase) && !KeepRunningAfterClose)
                running.RemoveAll(application => application.ProcessId == processId);
            return action.Equals("close", StringComparison.OrdinalIgnoreCase) && KeepRunningAfterClose
                ? ComputerOperationStatus.ConfirmationRequired
                : ComputerOperationStatus.Succeeded;
        }
        public IReadOnlyList<RunningApplication> Running() => [.. running];
        public IReadOnlyList<VisibleApplicationWindow> VisibleWindows()
        {
            if (Launched is null)
                return [.. windows];

            PostLaunchObservationCount++;
            if (!VerificationAvailable || PostLaunchObservationCount <= VerificationObservationsBeforeAvailable)
                return [];

            var processName = Path.GetFileNameWithoutExtension(Launched) switch
            {
                "WINWORD" => "WINWORD",
                "EXCEL" => "EXCEL",
                "calc" => "CalculatorApp",
                var name => name
            };
            var title = processName == "CalculatorApp" ? "Calculator" : processName;
            return [new(7001, 70, processName, title, "TestWindowClass")];
        }
        public ComputerOperationStatus WindowByHandle(long windowHandle, string action)
        {
            LastWindowHandle = windowHandle;
            LastWindowAction = action;
            var target = windows.FirstOrDefault(window => window.WindowHandle == windowHandle);
            LastWindowProcessId = target?.ProcessId;
            if (action.Equals("close", StringComparison.OrdinalIgnoreCase) && !KeepRunningAfterClose)
                windows.RemoveAll(window => window.WindowHandle == windowHandle);
            return action.Equals("close", StringComparison.OrdinalIgnoreCase) && KeepRunningAfterClose
                ? ComputerOperationStatus.ConfirmationRequired
                : ComputerOperationStatus.Succeeded;
        }
    }
}
