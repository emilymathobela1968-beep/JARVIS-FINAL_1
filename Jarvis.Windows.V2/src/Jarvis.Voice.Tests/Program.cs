using Jarvis.Voice;
using NAudio.Wave;

var passed = 0; var failed = 0;
void Check(bool ok, string name) { if (ok) { passed++; Console.WriteLine($"PASS {name}"); } else { failed++; Console.WriteLine($"FAIL {name}"); } }
var sink = new TestSink();
await using var session = new VoiceSessionController();
await session.StartAsync();
var first = await session.BeginTurnAsync(); await session.MarkThinkingAsync(first.Id); await session.InterruptAsync("regression");
Check(first.CancellationToken.IsCancellationRequested, "VoiceSession cancellation");
Check(!await session.CompleteTurnAsync(first.Id), "VoiceSession rejects cancelled completion");
var second = await session.BeginTurnAsync(); await session.MarkSpeakingAsync(second.Id);
Check(await session.CompleteTurnAsync(second.Id) && session.State == VoiceSessionState.Listening, "VoiceSession returns to Listening");
var regressionFailed = failed;
Console.WriteLine(regressionFailed == 0 ? "VOICE_SESSION_TESTS_PASSED" : "VOICE_SESSION_TESTS_FAILED");
var gate = new Gate4Orchestrator(session, sink);
var old = await session.BeginTurnAsync(); await session.MarkSpeakingAsync(old.Id);
var detection = new WindowsBargeInDetector.Detection(old.Id, [1, 2, 3, 4]);
var playbackStopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var accepting = gate.AcceptBargeInAsync(detection, oldPlayback: playbackStopped.Task);
Check(old.CancellationToken.IsCancellationRequested && !accepting.IsCompleted, "handoff waits for cancelled old playback to stop");
Check(await gate.AcceptBargeInAsync(detection) is null, "concurrent duplicate rejected");
playbackStopped.SetResult();
var replacement = (await accepting)!;
Check(replacement.Id != old.Id, "replacement has new Turn ID");
Check(await gate.AcceptBargeInAsync(detection) is null, "late duplicate rejected");
var rig = new Fakes();
Check(await gate.RunReplacementAsync(replacement, detection.PreRollPcm16, rig, rig, rig, rig.Tts, rig.Play), "replacement orchestration completes");
foreach (var stage in new[] { "capture", "stt", "reasoner", "tts", "playback" })
{
    var calls = rig.Calls.Where(x => x.Stage == stage).ToArray();
    Check(calls.Length == 1, $"{stage} invocation count = 1");
    Check(calls.All(x => x.TurnId == replacement.Id && x.Completed && !x.Cancelled && x.Input is not null && x.Output is not null), $"{stage} records ID input output completion cancellation");
}
Check(((byte[])rig.Calls.Single(x => x.Stage == "capture").Input).SequenceEqual(detection.PreRollPcm16), "detector pre-roll passed exactly once");
Check(rig.Seeds == 1 && rig.Appends == 1, "one pre-roll seed and one continuation append");
Check(rig.FinalPcm.SequenceEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }), "PCM order PreRoll + Continuation");
Check(((byte[])rig.Calls.Single(x => x.Stage == "stt").Input).SequenceEqual(rig.FinalPcm), "STT receives exact assembled PCM from WAV");
Check((string)rig.Calls.Single(x => x.Stage == "reasoner").Input == ((SttResult)rig.Calls.Single(x => x.Stage == "stt").Output!).Transcript, "reasoner receives STT output");
Check((string)rig.Calls.Single(x => x.Stage == "tts").Input == (string)rig.Calls.Single(x => x.Stage == "reasoner").Output!, "TTS receives reasoner output");
Check(ReferenceEquals(rig.Calls.Single(x => x.Stage == "playback").Input, rig.Calls.Single(x => x.Stage == "tts").Output), "playback receives exact TTS output");
Check(session.State == VoiceSessionState.Listening, "replacement returns to Listening");
var future = await session.BeginTurnAsync();
Check(await gate.AcceptBargeInAsync(new(future.Id, [])) is not null, "stale event does not poison future interruption guard");

// Each held adapter deliberately completes after cancellation, like a non-cooperative provider.
foreach (var held in new[] { "capture", "stt", "reasoner", "tts", "playback" })
{
    var prior = await session.BeginTurnAsync();
    var fake = new Fakes { Hold = held };
    var run = gate.RunReplacementAsync(prior, [1, 2, 3, 4], fake, fake, fake, fake.Tts, fake.Play);
    await fake.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
    var next = (await gate.AcceptBargeInAsync(new(prior.Id, [9, 10])))!;
    await session.MarkThinkingAsync(next.Id);
    Check(await gate.AcceptBargeInAsync(new(prior.Id, [])) is null && session.IsCurrent(next.Id), $"{held}: stale detector cannot interrupt new turn");
    await session.MarkSpeakingAsync(prior.Id);
    Check(session.State == VoiceSessionState.Thinking && session.IsCurrent(next.Id), $"{held}: old state callback rejected");
    fake.Release.SetResult();
    Check(!await run.WaitAsync(TimeSpan.FromSeconds(5)), $"{held}: late old completion rejected");
    Check(fake.Calls.Single(x => x.Stage == held).Cancelled, $"{held}: records cancellation");
    Check(session.IsCurrent(next.Id) && !next.CancellationToken.IsCancellationRequested && session.State == VoiceSessionState.Thinking, $"{held}: late callback leaves new turn unchanged");
    var index = Array.IndexOf(new[] { "capture", "stt", "reasoner", "tts", "playback" }, held);
    Check(fake.Calls.Count == index + 1, $"{held}: old response cannot advance or resume");
    if (held == "playback") Check(fake.SuccessfulPlaybacks == 0, "cancelled old playback is not successful completion");
    Check(!await session.CompleteTurnAsync(prior.Id), $"{held}: old final completion rejected");
    var fresh = new Fakes();
    Check(await gate.RunReplacementAsync(next, [9, 10], fresh, fresh, fresh, fresh.Tts, fresh.Play), $"{held}: new turn still completes");
}
Check(sink.Events.Contains("stale_turn_rejected"), "stale rejection diagnostics emitted");
var diagnosticRoot = Path.Combine(Path.GetTempPath(), "jarvis-stt-diagnostics-tests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(diagnosticRoot);
try
{
    var diagnosticTurnId = Guid.NewGuid();
    var diagnosticWave = Path.Combine(diagnosticRoot, "input.wav");
    await File.WriteAllBytesAsync(diagnosticWave, [82, 73, 70, 70, 1, 2, 3, 4]);
    var fullError = "Traceback first line" + Environment.NewLine
        + new string('x', 900) + Environment.NewLine
        + "FISH_AUDIO_API_KEY=do-not-write-this" + Environment.NewLine
        + "FISH_AUDIO_REFERENCE_ID=do-not-write-this-either" + Environment.NewLine
        + "terminal decode_audio exception";
    var record = SttFailureDiagnostics.Preserve(new SttAudio(diagnosticWave, "test mic", diagnosticTurnId), fullError, diagnosticRoot);
    Check(File.Exists(diagnosticWave), "failed STT leaves original WAV for normal owner cleanup");
    Check(File.Exists(record.PreservedWavePath), "failed STT preserves WAV copy");
    Check(record.PreservedWavePath!.Contains(diagnosticTurnId.ToString("N")) && record.FullStderrPath.Contains(diagnosticTurnId.ToString("N")), "failed STT artifacts share turn correlation ID");
    var retainedStderr = await File.ReadAllTextAsync(record.FullStderrPath);
    Check(retainedStderr.Contains("terminal decode_audio exception") && retainedStderr.Length > 512, "full failure stderr retained without 512 byte truncation");
    Check(!retainedStderr.Contains("do-not-write-this") && retainedStderr.Contains("FISH_AUDIO_API_KEY=<REDACTED>") && retainedStderr.Contains("FISH_AUDIO_REFERENCE_ID=<REDACTED>"), "secret-like Fish values are redacted from STT diagnostics");
    File.Delete(diagnosticWave);
    Check(!File.Exists(diagnosticWave) && File.Exists(record.PreservedWavePath), "successful-turn cleanup owner behavior remains unchanged");
}
finally
{
    try { Directory.Delete(diagnosticRoot, recursive: true); } catch { }
}
Console.WriteLine($"DETERMINISTIC_TOTAL passed={passed} failed={failed}; VoiceSession passed={3-regressionFailed} failed={regressionFailed}; Gate4 passed={passed-(3-regressionFailed)} failed={failed-regressionFailed}");
Console.WriteLine(failed == 0 ? "GATE4_DETERMINISTIC_TESTS_PASSED" : "GATE4_DETERMINISTIC_TESTS_FAILED");
return failed == 0 ? 0 : 1;

sealed class Call(string stage, Guid turnId, object input)
{
    public string Stage = stage; public Guid TurnId = turnId; public object Input = input;
    public object? Output; public bool Cancelled; public bool Completed;
}
sealed class Fakes : IReplacementCapture, ISttProvider, IJarvisReasoner
{
    public List<Call> Calls = new();
    public string? Hold;
    public TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int Seeds, Appends, SuccessfulPlaybacks;
    public byte[] FinalPcm = [];
    private async Task<T> Invoke<T>(string stage, Guid id, object input, CancellationToken token, Func<T> output)
    {
        var call = new Call(stage, id, input); Calls.Add(call);
        using var registration = token.Register(() => call.Cancelled = true);
        if (Hold == stage) { Entered.TrySetResult(); await Release.Task; }
        var result = output(); call.Output = result; call.Completed = true; return result;
    }
    public Task<ReplacementCaptureResult> CaptureAsync(Guid id, byte[] pre, CancellationToken token = default) =>
        Invoke("capture", id, pre.ToArray(), token, () => { Seeds++; Appends++; FinalPcm = Gate4Orchestrator.AssembleReplacementPcm(pre, [5, 6, 7, 8]); return new ReplacementCaptureResult(id, FinalPcm, "fake mic"); });
    public Task<SttResult> TranscribeAsync(SttAudio audio, CancellationToken token = default)
    {
        using var reader = new WaveFileReader(audio.WavePath); var pcm = new byte[checked((int)reader.Length)]; reader.ReadExactly(pcm);
        return Invoke("stt", audio.TurnId, pcm, token, () => new SttResult("replacement transcript", TimeSpan.Zero));
    }
    public Task<string> RespondAsync(string text, Guid id, CancellationToken token = default) => Invoke("reasoner", id, text, token, () => "replacement response");
    public Task<TtsAudio> Tts(Guid id, string text, CancellationToken token) => Invoke("tts", id, text, token, () => new TtsAudio([11, 12], "audio/wav", ".wav", "fake"));
    public Task<PlaybackResult> Play(Guid id, TtsAudio audio, CancellationToken token) => Invoke("playback", id, audio, token, () =>
    {
        if (!token.IsCancellationRequested) SuccessfulPlaybacks++;
        return new PlaybackResult(!token.IsCancellationRequested, token.IsCancellationRequested, token.IsCancellationRequested ? "cancelled" : null, "fake speaker");
    });
}
sealed class TestSink : IVoiceDiagnosticSink
{
    public List<string> Events = new();
    public void Write(string name, IReadOnlyDictionary<string, object?> fields) => Events.Add(name);
}

