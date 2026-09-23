using System.Text.Json;
using System.Text.Json.Serialization;
using Jarvis.Core;
using Jarvis.ApplicationCapabilities;
using Jarvis.Gate5;

if (args.Length == 1 && args[0] == "--acceptance") return await PhysicalVoiceHost.RunAsync(formalAcceptance: true);
if (args.Length == 1 && args[0] == "--voice-probe") return await PhysicalVoiceHost.RunAsync(preserveProbe: true);
if (args.Length == 1 && args[0] == "--voice") return await PhysicalVoiceHost.RunAsync();
if (args.Length != 2 || args[0] != "--text")
{
    Console.WriteLine("Gate 5.1: --text \"Jarvis, open Word.\" or --voice"); return 2;
}
var router = new ApplicationLaunchIntentRouter();
if (!router.TryRoute(args[1], out var request)) { Console.Error.WriteLine("Unsupported request. Ask to open Word, Calculator, File Explorer, or Notepad."); return 2; }
var executor = new ApplicationLaunchExecutor(new WindowsApplicationHost(), new AllowListedApplicationAuthorization(),
    new JsonLaunchAudit(Path.Combine(AppContext.BaseDirectory, "logs", "gate5-launch.jsonl")));
using var operation = new CancellationTokenSource();
ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; operation.Cancel(); };
Console.CancelKeyPress += cancel;
try
{
    var result = await executor.ExecuteAsync(request!, TimeSpan.FromSeconds(15), operation.Token);
    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } }));
    Console.WriteLine($"JARVIS: {result.Message}");
    return result.Status is LaunchStatus.Succeeded or LaunchStatus.AlreadyRunning ? 0 : 1;
}
finally { Console.CancelKeyPress -= cancel; }

