using Jarvis.Contracts;
using Jarvis.Core;
using Jarvis.Infrastructure;
using System.Net;
using System.Net.Sockets;

var failures = new List<string>();
void Check(bool condition, string message)
{
    if (!condition) failures.Add(message);
}

var root = Path.Combine(Path.GetTempPath(), "jarvis-capability-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
Directory.CreateDirectory(Path.Combine(root, "visible-folder"));
File.WriteAllText(Path.Combine(root, "visible.txt"), "safe test fixture");
File.WriteAllText(Path.Combine(root, ".env"), "not listed");
File.WriteAllText(Path.Combine(root, "api-token.txt"), "not listed");

try
{
    var host = new FakeWindowsCapabilityHost();
    var audit = new MemoryAuditSink();
    var service = new WindowsCapabilityService(host, audit, root);
    var router = new NaturalLanguageCapabilityRouter();

    var phrases = new Dictionary<string, string>
    {
        ["Jarvis, open File Explorer."] = CapabilityNames.OpenFileExplorer,
        ["Open the Jarvis folder."] = CapabilityNames.OpenFolder,
        ["Open Calculator."] = CapabilityNames.OpenCalculator,
        ["Open Notepad."] = CapabilityNames.OpenNotepad,
        ["Open https://www.google.com."] = CapabilityNames.OpenUrl,
        [@"Show me the files in C:\JARVIS_CODEX."] = CapabilityNames.ListDirectory,
        ["What folder are you working in?"] = CapabilityNames.GetCurrentDirectory,
        ["What processes are running?"] = CapabilityNames.ListRunningProcesses,
    };

    foreach (var phrase in phrases)
    {
        Check(router.TryRoute(phrase.Key, out var request) && request?.Name == phrase.Value,
            $"Route failed: {phrase.Key}");
    }
    Check(!router.TryRoute("delete C:\\JARVIS_CODEX", out _), "Destructive phrase was routed.");
    Check(!router.TryRoute("run powershell Get-ChildItem", out _), "Shell phrase was routed.");

    Check((await service.ExecuteAsync(new(CapabilityNames.OpenFileExplorer))).Success, "Explorer failed.");
    Check((await service.ExecuteAsync(new(CapabilityNames.OpenNotepad))).Success, "Notepad failed.");
    Check((await service.ExecuteAsync(new(CapabilityNames.OpenCalculator))).Success, "Calculator failed.");
    Check((await service.ExecuteAsync(new(CapabilityNames.OpenUrl, new Dictionary<string, string> { ["url"] = "https://example.com" }))).Success, "HTTPS URL failed.");
    Check((await service.ExecuteAsync(new(CapabilityNames.OpenFolder, new Dictionary<string, string> { ["path"] = root }))).Success, "Folder failed.");
    var listing = await service.ExecuteAsync(new(CapabilityNames.ListDirectory, new Dictionary<string, string> { ["path"] = root }));
    Check(listing.Success, "Directory listing failed.");
    var entryNames = ((IEnumerable<Dictionary<string, object?>>)listing.Data!["entries"]!)
        .Select(entry => (string)entry["name"]!);
    Check(!entryNames.Contains(".env", StringComparer.OrdinalIgnoreCase) &&
          !entryNames.Contains("api-token.txt", StringComparer.OrdinalIgnoreCase), "Sensitive filename was listed.");
    Check((await service.ExecuteAsync(new(CapabilityNames.GetCurrentDirectory))).Success, "Current directory failed.");
    Check((await service.ExecuteAsync(new(CapabilityNames.ListRunningProcesses))).Success, "Process list failed.");

    Check(!(await service.ExecuteAsync(new("delete_files"))).Success, "Unsupported destructive capability succeeded.");
    Check(!(await service.ExecuteAsync(new(CapabilityNames.OpenUrl, new Dictionary<string, string> { ["url"] = "file:///C:/secret.txt" }))).Success, "file URL succeeded.");
    Check(!(await service.ExecuteAsync(new(CapabilityNames.OpenUrl, new Dictionary<string, string> { ["url"] = "javascript:alert(1)" }))).Success, "javascript URL succeeded.");
    Check(!(await service.ExecuteAsync(new(CapabilityNames.OpenFolder, new Dictionary<string, string> { ["path"] = Path.Combine(root, "missing") }))).Success, "Missing folder succeeded.");
    Check(audit.Events.Count == 12, "Every capability call was not audited.");
    Check(host.OpenedApplications.SequenceEqual(new[] { "notepad.exe", "calc.exe" }), "Unexpected executable dispatch.");
    Check(host.OpenedUrls.SequenceEqual(new[] { "https://example.com/" }), "Unexpected URL dispatch.");

    var incompleteBarehandsLayout = BarehandsServerHost.GetRuntimeLayout(root);
    Check(incompleteBarehandsLayout.StageUri == BarehandsServerHost.StageUri, "Barehands stage URI changed.");
    Check(incompleteBarehandsLayout.StageUri.Host == "127.0.0.1" && incompleteBarehandsLayout.StageUri.Port == 8794,
        "Barehands is not pinned to loopback.");
    Check(!BarehandsServerHost.IsValidRuntimeLayout(incompleteBarehandsLayout),
        "An incomplete Barehands layout was accepted.");

    await using (var barehands = new BarehandsServerHost())
    {
        Check(BarehandsServerHost.IsValidRuntimeLayout(BarehandsServerHost.GetRuntimeLayout()),
            "Packaged Barehands layout is incomplete.");
        for (var cycle = 1; cycle <= 2; cycle++)
        {
            var started = await barehands.StartAsync();
            Check(started.Success && started.Started && barehands.IsRunning,
                $"Barehands lifecycle start failed on cycle {cycle}: {started.Error ?? started.Message}");
            await barehands.StopAsync();
            Check(!barehands.IsRunning, $"Barehands lifecycle stop left the owned server running on cycle {cycle}.");
            Check(!await IsLoopbackPortListeningAsync(BarehandsServerHost.LoopbackPort),
                $"Barehands loopback port remained open after cycle {cycle}.");
        }
    }
}
finally
{
    Directory.Delete(root, recursive: true);
}

if (failures.Count > 0)
{
    Console.Error.WriteLine(string.Join(Environment.NewLine, failures));
    return 1;
}
Console.WriteLine("Capability tests passed: 12 audited explicit requests; packaged Barehands loopback lifecycle and port release verified twice.");
return 0;

static async Task<bool> IsLoopbackPortListeningAsync(int port)
{
    try
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        return true;
    }
    catch (SocketException)
    {
        return false;
    }
}

sealed class FakeWindowsCapabilityHost : IWindowsCapabilityHost
{
    public List<string> OpenedApplications { get; } = [];
    public List<string> OpenedFolders { get; } = [];
    public List<string> OpenedUrls { get; } = [];
    public void OpenApplication(string executable) => OpenedApplications.Add(executable);
    public void OpenFolder(string resolvedPath) => OpenedFolders.Add(resolvedPath);
    public void OpenUrl(Uri url) => OpenedUrls.Add(url.AbsoluteUri);
    public IReadOnlyList<(string Name, int Id)> ListProcesses() => [("Jarvis.Test", 4242)];
}

sealed class MemoryAuditSink : ICapabilityAuditSink
{
    public List<CapabilityAuditEvent> Events { get; } = [];
    public void Write(CapabilityAuditEvent auditEvent) => Events.Add(auditEvent);
}
