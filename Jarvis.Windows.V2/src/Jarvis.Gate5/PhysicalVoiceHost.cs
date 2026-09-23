using System.Diagnostics;
using Jarvis.Voice;
using Jarvis.ApplicationCapabilities;
namespace Jarvis.Gate5;
internal static class PhysicalVoiceHost { public static async Task<int> RunAsync(bool preserveProbe = false, bool formalAcceptance = false) {



var fish = FishAudioOptions.FromEnvironment();
if (!fish.IsConfigured) { Console.Error.WriteLine("CONFIGURATION_REQUIRED: Set FISH_AUDIO_API_KEY and FISH_AUDIO_REFERENCE_ID in this PowerShell session."); return 2; }
var acceptance = formalAcceptance ? new FormalAcceptance() : null;
var logDirectory = formalAcceptance ? Path.Combine(AppContext.BaseDirectory, "logs", "acceptance-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")) : Path.Combine(AppContext.BaseDirectory, "logs");
if (formalAcceptance) Console.WriteLine($"ACCEPTANCE_EVIDENCE: {logDirectory}");
var evidence = (preserveProbe || formalAcceptance) ? new ProbeEvidence(Path.Combine(logDirectory, "probe-audio", DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"))) : null;
var diagnostics = new Gate5HandoffSink(Path.Combine(logDirectory, "voice-gate5.jsonl"), evidence);
await using var session = new VoiceSessionController();
session.StateChanged += (_, s) => diagnostics.Write("voice_state", new Dictionary<string, object?> { ["turnId"] = s.TurnId, ["from"] = s.Previous.ToString(), ["to"] = s.Current.ToString() });
session.StaleTurnRejected += (_, s) => diagnostics.Write("stale_turn_rejected", new Dictionary<string, object?> { ["turnId"] = s.TurnId, ["callback"] = s.Callback });
var gate = new Gate4Orchestrator(session, diagnostics);
var microphone = new WindowsMicrophoneCapture(diagnostics);
var replacementCapture = new WindowsReplacementCapture(diagnostics);
ISttProvider stt = new FasterWhisperSttProvider(diagnostics);
if (evidence is not null) stt = new EvidenceRetainingSttProvider(stt, evidence);
var trace = new JsonCapabilityTrace(Path.Combine(logDirectory, "gate5-trace.jsonl"));
var reasoner = new ApplicationCapabilityReasoner(new ApplicationLaunchExecutor(new WindowsApplicationHost(), new AllowListedApplicationAuthorization(), new JsonLaunchAudit(Path.Combine(logDirectory, "gate5-launch.jsonl"))), (stage, data) => { trace.Write(stage, data); acceptance?.Observe(stage, data); });
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
var attemptLimit = formalAcceptance ? FormalAcceptance.Steps.Count : preserveProbe ? 1 : 10;
for (int attempt = 1; attempt <= attemptLimit; attempt++)
{
    acceptance?.Reset();
    if (formalAcceptance) Console.WriteLine($"FORMAL STEP {attempt}/{attemptLimit}: Say exactly once: {FormalAcceptance.Steps[attempt - 1].Command}. Press Enter immediately after finishing. Verified AlreadyRunning is valid; no duplicate launch is expected.");
    Console.WriteLine($"Gate 5.1 attempt {attempt}/{attemptLimit}. Press Enter when ready to record the initial utterance.");
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
                Console.WriteLine("DETECTOR ACTIVE: interruption is optional. If interrupting, continue speaking until prompted to press Enter.");
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
                await session.InterruptAsync("Gate 5.1 playback/handoff fault");
                try { await playing; } catch { }
                throw;
            }
            finally { diagnostics.PlaybackStarted -= OnStarted; }
        }
        var normalCompleted = await gate.RunTurnAsync(turn, initial, stt, reasoner, Speak, PlayInitial);
        if ((!replaced && !normalCompleted) || session.State != VoiceSessionState.Listening)
        {
            Console.Error.WriteLine("ATTEMPT_INCOMPLETE: no completed turn. Stop and inspect the log."); return 1;
        }
        if (acceptance is not null)
        {
            var step = FormalAcceptance.Steps[attempt - 1];
            var verified = acceptance.Verify(step, reasoner.LastResult, !replaced && normalCompleted && session.State == VoiceSessionState.Listening, out var explanation);
            diagnostics.Write("acceptance_machine_result", new Dictionary<string, object?> { ["turnId"] = turn.Id, ["step"] = attempt, ["verified"] = verified, ["reason"] = explanation });
            Console.WriteLine(explanation);
            if (!verified) { Console.Error.WriteLine("ACCEPTANCE_STOPPED: first failure; evidence retained."); return 1; }
            Console.WriteLine("Type PASS only if the application/result and spoken response were correct, without stale response or clipped speech. For rejection, confirm nothing launched. Anything else stops the run.");
            var confirmed = string.Equals(Console.ReadLine()?.Trim(), "PASS", StringComparison.OrdinalIgnoreCase);
            diagnostics.Write("acceptance_human_result", new Dictionary<string, object?> { ["turnId"] = turn.Id, ["step"] = attempt, ["confirmed"] = confirmed });
            if (!confirmed) { Console.Error.WriteLine("ACCEPTANCE_STOPPED: physical failure/unconfirmed outcome; evidence retained."); return 1; }
            if (step.Application is not null) replacements++;
            Console.WriteLine($"Valid application attempts confirmed: {replacements}/10; safety rejection tracked separately.");
            continue;
        }
        if (reasoner.LastResult?.Status is not (LaunchStatus.Succeeded or LaunchStatus.AlreadyRunning))
        {
            Console.Error.WriteLine("GATE5_CAPABILITY_NOT_PASSED: no verified capability result. Stopping; conversation completion is not capability acceptance.");
            return 1;
        }
        replacements++;
        Console.WriteLine($"Verified capability result received {replacements}/{attemptLimit}. Physically verify no old-response resumption and no clipped interruption words.");
    }
    catch (Exception ex)
    {
        diagnostics.Write("gate4_fault", new Dictionary<string, object?> { ["turnId"] = turn.Id, ["exceptionType"] = ex.GetType().Name });
        Console.Error.WriteLine($"GATE5_FAULT: {ex.GetType().Name}. Inspect voice-gate5.jsonl; no gate classification has been assigned."); return 1;
    }
    finally { if (initial is not null) File.Delete(initial.WavePath); }
}
Console.WriteLine($"verifiedCapabilityResultCount={replacements}; physical classification remains Leon's decision.");
return 0;


} }
sealed class Gate5HandoffSink(string path, ProbeEvidence? evidence = null) : IVoiceDiagnosticSink
{
    private readonly JsonLineVoiceDiagnosticSink inner = new(path);
    private readonly object sync = new();
    private readonly Dictionary<Guid, long> detections = new();
    private readonly Dictionary<Guid, long> replacementStarts = new();
    public event Action<Guid>? PlaybackStarted;
    public void Write(string name, IReadOnlyDictionary<string, object?> fields)
    {
        evidence?.Observe(name, fields);
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



