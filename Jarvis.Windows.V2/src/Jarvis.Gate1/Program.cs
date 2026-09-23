using Jarvis.Voice;

var options = FishAudioOptions.FromEnvironment();
if (!options.IsConfigured)
{
    Console.Error.WriteLine("CONFIGURATION_REQUIRED: Set FISH_AUDIO_API_KEY and FISH_AUDIO_REFERENCE_ID in this PowerShell session. FISH_AUDIO_MODEL is optional and defaults to s2.1-pro-free.");
    return 2;
}

var diagnostics = new JsonLineVoiceDiagnosticSink();
using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
var runner = new Gate1Runner(new FishAudioTtsProvider(client, options, diagnostics), new WindowsAudioPlayback(diagnostics));
var results = await runner.RunAsync();
foreach (var run in results) Console.WriteLine($"attempt={run.Attempt} http={run.HttpSucceeded} playbackCompleted={run.PlaybackCompleted} error={run.Error ?? "none"}");
Console.WriteLine($"httpSuccessCount={results.Count(x => x.HttpSucceeded)} playbackCompletionCount={results.Count(x => x.PlaybackCompleted)}");
return results.Count == 10 && results.All(x => x.HttpSucceeded && x.PlaybackCompleted) ? 0 : 1;
