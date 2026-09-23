using ApplicationId = Jarvis.ApplicationCapabilities.ApplicationId;
using Jarvis.ApplicationCapabilities;
using Jarvis.Core;
using Jarvis.Gate5;

int passed = 0, failed = 0;
void Check(bool value, string name) { Console.WriteLine($"{(value ? "PASS" : "FAIL")} {name}"); if (value) passed++; else failed++; }
var router = new ApplicationLaunchIntentRouter();
foreach (var (phrase, app) in new[] { ("Jarvis, open Word.", ApplicationId.Word), ("Open Calculator", ApplicationId.Calculator), ("Launch File Explorer", ApplicationId.FileExplorer), ("Open Notepad", ApplicationId.Notepad), ("start microsoft word!", ApplicationId.Word) })
    Check(router.TryRoute(phrase, out var request) && request!.Application == app, $"Core routes {phrase}");
foreach (var phrase in new[] { "open cmd", "open powershell", "open C:\\bad.exe", "open Word and write a letter", "open Notepad & calc.exe", "open https://example.com", "close Word", "open Word --macro", "" })
    Check(!router.TryRoute(phrase, out _), $"Core rejects unsupported input: {phrase}");
var timeout = TimeSpan.FromSeconds(2);
async Task<(LaunchResult Result, FakeHost Host, Audit Audit)> Run(FakeHost? host = null, bool allow = true, ApplicationId app = ApplicationId.Word, CancellationToken token = default, TimeSpan? limit = null)
{
    host ??= new(); var audit = new Audit(); var executor = new ApplicationLaunchExecutor(host, new Policy(allow), audit);
    return (await executor.ExecuteAsync(new(app), limit ?? timeout, token), host, audit);
}
var success = await Run();
Check(success.Result.Status == LaunchStatus.Succeeded && success.Result.Evidence?.ProcessId == 4242, "success requires independently observed window evidence");
Check(success.Host.Launches == 1 && success.Host.Probes == 2, "one launch, pre-launch and post-launch verification");
Check(success.Audit.Stages.SequenceEqual(new[] { "requested", "authorized", "launch_requested", "result" }), "authorization precedes launch, terminal result audited");
Check(success.Audit.Ids.Distinct().Count() == 1 && success.Result.OperationId != Guid.Empty, "one capability operation ID throughout");
var already = await Run(new() { Existing = true });
Check(already.Result.Status == LaunchStatus.AlreadyRunning && already.Host.Launches == 0 && already.Result.Evidence is not null, "already running requires evidence and does not relaunch");
var denied = await Run(allow: false);
Check(denied.Result.Status == LaunchStatus.PermissionDenied && denied.Host.Resolutions == 0 && denied.Host.Launches == 0, "authorization denial prevents resolution and execution");
var unknown = await Run(app: (ApplicationId)999);
Check(unknown.Result.Status == LaunchStatus.PermissionDenied && unknown.Host.Launches == 0, "unknown typed application is denied");
var missing = await Run(new() { Missing = true });
Check(missing.Result.Status == LaunchStatus.NotFound && missing.Host.Launches == 0, "missing approved executable returns NotFound");
var osDenied = await Run(new() { Error = new System.ComponentModel.Win32Exception(5) });
Check(osDenied.Result.Status == LaunchStatus.PermissionDenied, "Windows access denial is structured");
var failure = await Run(new() { Error = new System.ComponentModel.Win32Exception(193) });
Check(failure.Result.Status == LaunchStatus.Failed && failure.Result.Evidence is null, "failed launch never claims success");
var never = await Run(new() { NeverVisible = true }, limit: TimeSpan.FromMilliseconds(40));
Check(never.Result.Status == LaunchStatus.TimedOut && never.Host.Launches == 1 && never.Result.LaunchAttempted, "process launch alone times out without window verification");
using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
var before = await Run(token: cancelled.Token);
Check(before.Result.Status == LaunchStatus.Cancelled && before.Host.Launches == 0, "pre-cancelled operation cannot launch");
using var during = new CancellationTokenSource();
var after = await Run(new() { OnLaunch = during.Cancel }, token: during.Token);
Check(after.Result.Status == LaunchStatus.Cancelled && after.Host.Launches == 1 && after.Result.Evidence is null, "cancellation after dispatch suppresses unverified success");
var serialHost = new FakeHost { NeverVisible = true }; var serialAudit = new Audit();
var serial = new ApplicationLaunchExecutor(serialHost, new Policy(true), serialAudit);
var firstOperation = serial.ExecuteAsync(new(ApplicationId.Word), timeout);
var secondOperation = serial.ExecuteAsync(new(ApplicationId.Word), timeout);
serialHost.NeverVisible = false;
var concurrent = await Task.WhenAll(firstOperation, secondOperation);
Check(serialHost.Launches == 1 && concurrent.Count(x => x.Status == LaunchStatus.AlreadyRunning) == 1, "duplicate operations reuse verified application");
var appPath = Path.Combine(Environment.SystemDirectory, "notepad.exe");
var resolved = new ResolvedApplication(ApplicationId.Notepad, appPath);
Check(WindowsApplicationHost.Matches(resolved, appPath), "verifier accepts expected absolute image");
Check(!WindowsApplicationHost.Matches(resolved, Path.Combine(Path.GetTempPath(), "notepad.exe")), "same filename in unapproved directory is rejected");
var packaged = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps", "Microsoft.WindowsCalculator_1.0.0.0_x64__8wekyb3d8bbwe", "CalculatorApp.exe");
var calc = new ResolvedApplication(ApplicationId.Calculator, Path.Combine(Environment.SystemDirectory, "calc.exe"));
Check(WindowsApplicationHost.Matches(calc, packaged), "approved Calculator package identity accepted");
Check(!WindowsApplicationHost.Matches(calc, packaged.Replace("8wekyb3d8bbwe", "untrusted")), "wrong package publisher rejected");
var adapterHost = new FakeHost(); var adapter = new ApplicationCapabilityReasoner(new(adapterHost, new Policy(true), new Audit()));
var response = await adapter.RespondAsync("Jarvis, open Word.", Guid.NewGuid());
Check(adapterHost.Launches == 1 && response.Contains("verified"), "Core to typed executor to Jarvis response integration");
await adapter.RespondAsync("open Word and write a letter", Guid.NewGuid());
Check(adapterHost.Launches == 1, "compound request cannot partially execute");
using var voiceCancellation = new CancellationTokenSource();
var ownedHost = new FakeHost { OnLaunch = voiceCancellation.Cancel };
var ownedAdapter = new ApplicationCapabilityReasoner(new(ownedHost, new Policy(true), new Audit()));
bool speechSuppressed = false;
try { await ownedAdapter.RespondAsync("open Word", Guid.NewGuid(), voiceCancellation.Token); } catch (OperationCanceledException) { speechSuppressed = true; }
Check(speechSuppressed && ownedHost.Probes == 2, "voice cancellation suppresses response but cannot own capability operation");
Check(!typeof(ApplicationLaunchExecutor).Assembly.GetReferencedAssemblies().Any(x => x.Name!.Contains("Voice") || x.Name.Contains("Jarvis.Core")), "executor assembly has no Voice or Core dependency");
foreach (var phrase in new[] { "open Word", "launch Calculator", "start File Explorer", "open Notepad" })
{
    var replayHost = new FakeHost(); var replayAudit = new Audit();
    var replay = new ApplicationCapabilityReasoner(new(replayHost, new Policy(true), replayAudit));
    var reply = await replay.RespondAsync(phrase, Guid.NewGuid());
    Check(replayHost.Launches == 1 && replayAudit.Stages.Count(x => x == "requested") == 1 && reply.Contains("verified"), $"baseline replay end-to-end: {phrase}");
}
var recordedHost = new FakeHost();
var recordedAdapter = new ApplicationCapabilityReasoner(new(recordedHost, new Policy(true), new Audit()));
await recordedAdapter.RespondAsync("The job is going to open up Office Microsoft Word.", Guid.NewGuid());
Check(recordedHost.Launches == 0 && recordedHost.Resolutions == 0, "recorded Word transcript stops before capability execution");
Check(router.TryRoute("open up Office Microsoft Word", out _), "natural open-up Office Word request is recognized");
foreach (var (phrase, app) in new[] { ("open Word", ApplicationId.Word), ("launch Calculator", ApplicationId.Calculator), ("start File Explorer", ApplicationId.FileExplorer), ("open Notepad", ApplicationId.Notepad) })
foreach (var failLaunch in new[] { false, true })
{
    var host = new FakeHost { Error = failLaunch ? new System.ComponentModel.Win32Exception(193) : null };
    var audit = new Audit(); var steps = new List<(string Stage, System.Text.Json.JsonElement Data)>();
    void Trace(string stage, object? data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } });
        steps.Add((stage, System.Text.Json.JsonDocument.Parse(json).RootElement.Clone()));
        Console.WriteLine($"TRACE {phrase} failedHost={failLaunch} {stage} {json}");
    }
    System.Text.Json.JsonElement Payload(string stage)
    {
        var data = steps.Single(x => x.Stage == stage).Data.GetProperty("data");
        return data.TryGetProperty("operationId", out _) ? data.GetProperty("data") : data;
    }
    var tracedAdapter = new ApplicationCapabilityReasoner(new(host, new Policy(true), audit), Trace);
    var reply = await tracedAdapter.RespondAsync(phrase, Guid.NewGuid());
    var label = $"{phrase}, launchFailure={failLaunch}";
    Check(Payload("parser_input").GetProperty("transcript").GetString() == phrase && steps.Count(x => x.Stage == "intent_recognized") == 1, $"{label}: exact input and one parsed intent");
    Check(steps.Count(x => x.Stage == "typed_request") == 1 && Payload("typed_request").GetProperty("Application").GetString() == app.ToString(), $"{label}: exactly one correct typed request");
    Check(Payload("registry_resolved").GetProperty("Id").GetString() == app.ToString(), $"{label}: correct registry selection");
    Check(Payload("authorization_decision").GetProperty("allowed").GetBoolean(), $"{label}: approved authorization");
    Check(steps.Count(x => x.Stage == "executor_invoked") == 1 && host.Launches == 1, $"{label}: one executor and one host launch call");
    Check(Payload("executable_resolved").GetProperty("Executable").GetString() == FakeHost.Target(app), $"{label}: resolved test target recorded");
    var result = Payload("structured_result");
    Check(result.GetProperty("Status").GetString() == (failLaunch ? "Failed" : "Succeeded") && (failLaunch ? host.Probes == 1 : host.Probes == 2), $"{label}: independent verification distinguishes success/failure");
    Check(reply == result.GetProperty("Message").GetString() && Payload("response_text").GetProperty("response").GetString() == reply, $"{label}: structured result reaches response layer");
    Check(failLaunch ? !steps.Any(x => x.Stage == "process_start_returned") : Payload("process_start_returned").GetProperty("launchPid").GetInt32() == 4242, $"{label}: host return is recorded without inventing successful launch evidence");
}
foreach (var phrase in new[] { "Jarvis, could you please open up Word?", "open up Office Microsoft Word", "launch Calculator please", "start File Explorer", "open Notepad" })
    Check(router.TryRoute(phrase, out _), $"bounded natural request recognized: {phrase}");
foreach (var phrase in new[] { "don't open Word", "do not open Word", "he said open Word", "The job is going to open up Office Microsoft Word.", "open Word and Notepad", "please open Word and write a letter", "Thank you.", "", new string('x', 257) })
    Check(!router.TryRoute(phrase, out _), $"ambiguous, negative or invalid input produces no request ({phrase.Length} chars)");
var rejectedSteps = new List<string>();
var rejectedAdapter = new ApplicationCapabilityReasoner(new(new FakeHost(), new Policy(true), new Audit()), (stage, _) => rejectedSteps.Add(stage));
await rejectedAdapter.RespondAsync("The job is going to open up Office Microsoft Word.", Guid.NewGuid());
Check(rejectedSteps.SequenceEqual(new[] { "parser_input", "intent_rejected", "response_text" }) && rejectedAdapter.LastResult is null, "exact recorded transcript trace proves first stopping boundary; no capability result counted");
foreach (var step in FormalAcceptance.Steps)
{
    var acceptance = new FormalAcceptance();
    var acceptanceHost = new FakeHost();
    var acceptanceAdapter = new ApplicationCapabilityReasoner(new(acceptanceHost, new Policy(true), new Audit()), acceptance.Observe);
    await acceptanceAdapter.RespondAsync(step.Command, Guid.NewGuid());
    Check(acceptance.Verify(step, acceptanceAdapter.LastResult, true, out _), $"formal acceptance verifies expected outcome: {step.Command}");
    Check(!acceptance.Verify(step, acceptanceAdapter.LastResult, false, out _), $"formal acceptance rejects incomplete conversation: {step.Command}");
    if (step.Application is not null)
    {
        acceptance.Observe("typed_request", new { data = new LaunchApplicationRequest(step.Application.Value) });
        Check(!acceptance.Verify(step, acceptanceAdapter.LastResult, true, out _), $"formal acceptance rejects duplicate typed request: {step.Command}");
    }
}
Check(FormalAcceptance.Steps.Count(x => x.Application is not null) == 10 && FormalAcceptance.Steps.Count(x => x.Application is null) == 1, "formal sequence has ten valid attempts plus one separate safety rejection");
await ProbeEvidenceTests.Run(Check);
Console.WriteLine($"GATE5_APPLICATION_TESTS passed={passed} failed={failed}");
return failed == 0 ? 0 : 1;

sealed class Policy(bool allowed) : IApplicationAuthorization { public bool IsAllowed(ApplicationDefinition app) => allowed; }
sealed class Audit : ILaunchAudit
{
    public List<string> Stages = []; public List<Guid> Ids = [];
    public void Write(string stage, Guid operationId, ApplicationId app, LaunchResult? result = null) { Stages.Add(stage); Ids.Add(operationId); }
}
sealed class FakeHost : IApplicationHost
{
    public static string Target(ApplicationId app) => @"C:\Approved\" + (app switch { ApplicationId.Word => "WINWORD.EXE", ApplicationId.Calculator => "calc.exe", ApplicationId.FileExplorer => "explorer.exe", _ => "notepad.exe" });
    public bool Existing, Missing, NeverVisible; public int Launches, Resolutions, Probes;
    public Exception? Error; public Action? OnLaunch;
    public ResolvedApplication? Resolve(ApplicationDefinition app) { Resolutions++; return Missing ? null : new(app.Id, Target(app.Id)); }
    public WindowEvidence? FindWindow(ResolvedApplication app) { Probes++; return !NeverVisible && (Existing || Launches > 0) ? new(4242, 123, app.Executable, "OpusApp") : null; }
    public int? Launch(ResolvedApplication app) { Launches++; if (Error is not null) throw Error; OnLaunch?.Invoke(); return 4242; }
}







