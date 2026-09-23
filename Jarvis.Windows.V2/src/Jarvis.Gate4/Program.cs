using System.Diagnostics;
using Jarvis.Voice;

var fish = FishAudioOptions.FromEnvironment();
if (!fish.IsConfigured) { Console.Error.WriteLine("CONFIGURATION_REQUIRED: Set FISH_AUDIO_API_KEY and FISH_AUDIO_REFERENCE_ID in this PowerShell session."); return 2; }
var diagnostics = new HandoffSink(Path.Combine(AppContext.BaseDirectory, "logs", "voice-gate4.jsonl"));
await using var session = new VoiceSessionController();
session.StateChanged += (_, s) => diagnostics.Write("voice_state", new Dictionary<string, object?> { ["turnId"] = s.TurnId, ["from"] = s.Previous.ToString(), ["to"] = s.Current.ToString() });
session.StaleTurnRejected += (_, s) => diagnostics.Write("stale_turn_rejected", new Dictionary<string, object?> { ["turnId"] = s.TurnId, ["callback"] = s.Callback });
var gate = new Gate4Orchestrator(session, diagnostics);
var microphone = new WindowsMicrophoneCapture(diagnostics);
var replacementCapture = new WindowsReplacementCapture(diagnostics);
var stt = new FasterWhisperSttProvider(diagnostics);
var reasoner = new DeterministicGate3Reasoner();
using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
var tts = new FishAudioTtsProvider(http, fish, diagnostics);
var playback = new WindowsAudioPlayback(diagnostics);
Task<TtsAudio> Speak(Guid id, string text, CancellationToken token)
{
    Console.WriteLine($"RESPONSE [{id}]: {text}");
    return tts.SynthesizeAsync(text, token);
}
Task<PlaybackResult> PlayReplacement(Guid id, TtsAudio audio, CancellationToken token) => playback.PlayAsync(audio, id, token);
await session.StartAsync();
var replacements = 0;
for (int attempt = 1; attempt <= 10; attempt++)
{
    Console.WriteLine($"Gate 4 attempt {attempt}/10. Press Enter when ready to record the initial utterance.");
    if (Console.ReadLine() is null) return 2;
    var turn = await session.BeginTurnAsync();
    SttAudio? initial = null;
    bool replaced = false;
    try
    {
        initial = await microphone.CaptureUntilEnterAsync(turn.Id, turn.CancellationToken);
        async Task<PlaybackResult> PlayInitial(Guid id, TtsAudio audio, CancellationToken token)
        {
            await using var detector = new WindowsBargeInDetector(diagnostics);
            var detected = new TaskCompletionSource<WindowsBargeInDetector.Detection>(TaskCreationOptions.RunContinuationsAsynchronously);
            detector.SpeechDetected += (_, d) => detected.TrySetResult(d);
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnStarted(Guid startedId) { if (startedId == id) started.TrySetResult(); }
            diagnostics.PlaybackStarted += OnStarted;
            var playing = playback.PlayAsync(audio, id, token);
            try
            {
                await Task.WhenAny(started.Task, playing);
                if (!started.Task.IsCompletedSuccessfully) return await playing;
                detector.Start(id);
                Console.WriteLine("DETECTOR ACTIVE: interrupt aloud now; continue speaking until prompted to press Enter.");
                await Task.WhenAny(detected.Task, playing);
                await detector.StopAsync();
                if (!detected.Task.IsCompletedSuccessfully) return await playing;
                var detection = await detected.Task;
                var next = await gate.AcceptBargeInAsync(detection, oldPlayback: playing);
                if (next is null) throw new InvalidOperationException("Detected interruption was not accepted.");
                replaced = await gate.RunReplacementAsync(next, detection.PreRollPcm16, replacementCapture, stt, reasoner, Speak, PlayReplacement);
                return await playing;
            }
            catch
            {
                // Cancel and drain the current device before leaving this attempt.
                await session.InterruptAsync("Gate 4 playback/handoff fault");
                try { await playing; } catch { }
                throw;
            }
            finally { diagnostics.PlaybackStarted -= OnStarted; }
        }
        await gate.RunTurnAsync(turn, initial, stt, reasoner, Speak, PlayInitial);
        if (!replaced || session.State != VoiceSessionState.Listening)
        {
            Console.Error.WriteLine("ATTEMPT_INCOMPLETE: no completed replacement turn. Stop and inspect the log."); return 1;
        }
        replacements++;
        Console.WriteLine($"Replacement lifecycle completed {replacements}/10. Physically verify no old-response resumption and no clipped interruption words.");
    }
    catch (Exception ex)
    {
        diagnostics.Write("gate4_fault", new Dictionary<string, object?> { ["turnId"] = turn.Id, ["exceptionType"] = ex.GetType().Name });
        Console.Error.WriteLine($"GATE4_FAULT: {ex.GetType().Name}. Inspect voice-gate4.jsonl; no gate classification has been assigned."); return 1;
    }
    finally { if (initial is not null) File.Delete(initial.WavePath); }
}
Console.WriteLine($"replacementLifecycleCompletionCount={replacements}; physical classification remains Leon's decision.");
return 0;

sealed class HandoffSink(string path) : IVoiceDiagnosticSink
{
    private readonly JsonLineVoiceDiagnosticSink inner = new(path);
    private readonly object sync = new();
    private readonly Dictionary<Guid, long> detections = new();
    private readonly Dictionary<Guid, long> replacementStarts = new();
    public event Action<Guid>? PlaybackStarted;
    public void Write(string name, IReadOnlyDictionary<string, object?> fields)
    {
        var values = new Dictionary<string, object?>(fields);
        lock (sync)
        {
            if (fields.TryGetValue("turnId", out var value) && value is Guid id)
            {
                if (name == "barge_in_detected") detections[id] = Stopwatch.GetTimestamp();
                if (name == "new_turn_created" && fields.TryGetValue("oldTurnId", out var old) && old is Guid oldId && detections.TryGetValue(oldId, out var start)) replacementStarts[id] = start;
                if (name == "replacement_first_continuation_pcm" && replacementStarts.TryGetValue(id, out var detectedAt))
                {
                    values["bargeInToFirstContinuationMs"] = Stopwatch.GetElapsedTime(detectedAt).TotalMilliseconds;
                    Console.WriteLine($"MIC HANDOFF [{id}]: {values["bargeInToFirstContinuationMs"]:F2} ms");
                }
                inner.Write(name, values);
                if (name == "playback_started") PlaybackStarted?.Invoke(id);
            }
            else inner.Write(name, values);
        }
    }
}
