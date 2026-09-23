using Jarvis.Voice;

var fish = FishAudioOptions.FromEnvironment();
if (!fish.IsConfigured) { Console.Error.WriteLine("CONFIGURATION_REQUIRED: FISH_AUDIO_API_KEY and FISH_AUDIO_REFERENCE_ID must be set in this PowerShell session."); return 2; }
var diagnostics = new JsonLineVoiceDiagnosticSink(Path.Combine(AppContext.BaseDirectory, "logs", "voice-gate3.jsonl"));
await using var session = new VoiceSessionController();
session.StateChanged += (_, state) => diagnostics.Write("voice_state", new Dictionary<string, object?> { ["turnId"] = state.TurnId, ["from"] = state.Previous.ToString(), ["to"] = state.Current.ToString(), ["reason"] = state.Reason });
await session.StartAsync();
var capture = new WindowsMicrophoneCapture(diagnostics);
var stt = new FasterWhisperSttProvider(diagnostics);
var reasoner = new DeterministicGate3Reasoner();
using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
var tts = new FishAudioTtsProvider(http, fish, diagnostics);
var playback = new WindowsAudioPlayback(diagnostics);
var completed = 0;
for (var attempt = 1; attempt <= 10; attempt++)
{
    var turn = await session.BeginTurnAsync();
    try
    {
        Console.WriteLine($"Gate 3 turn {attempt}/10. Turn {turn.Id}.");
        var audio = await capture.CaptureUntilEnterAsync(turn.Id, turn.CancellationToken);
        await session.MarkTranscribingAsync(turn.Id, turn.CancellationToken);
        var sttResult = await stt.TranscribeAsync(audio, turn.CancellationToken);
        Console.WriteLine($"FINAL TRANSCRIPT [{turn.Id}]: {sttResult.Transcript}");
        await session.MarkThinkingAsync(turn.Id, turn.CancellationToken);
        diagnostics.Write("reasoning_started", new Dictionary<string, object?> { ["turnId"] = turn.Id });
        var response = await reasoner.RespondAsync(sttResult.Transcript, turn.Id, turn.CancellationToken);
        diagnostics.Write("reasoning_completed", new Dictionary<string, object?> { ["turnId"] = turn.Id, ["responseText"] = response });
        Console.WriteLine($"RESPONSE [{turn.Id}]: {response}");
        var ttsAudio = await tts.SynthesizeAsync(response, turn.CancellationToken);
        await session.MarkSpeakingAsync(turn.Id, turn.CancellationToken);
        var outcome = await playback.PlayAsync(ttsAudio, turn.CancellationToken);
        if (!outcome.Completed) throw new InvalidOperationException(outcome.CancellationReason ?? "Windows playback did not complete.");
        if (!await session.CompleteTurnAsync(turn.Id, turn.CancellationToken)) throw new InvalidOperationException("Stale turn completion was rejected.");
        completed++;
    }
    catch (Exception ex) { diagnostics.Write("gate3_fault", new Dictionary<string, object?> { ["turnId"] = turn.Id, ["reason"] = ex.Message }); Console.Error.WriteLine($"GATE3_FAULT [{turn.Id}]: {ex.Message}"); break; }
}
Console.WriteLine($"gate3ConversationCompletionCount={completed}");
return completed == 10 ? 0 : 1;
