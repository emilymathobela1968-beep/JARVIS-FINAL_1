using Jarvis.Gate5;
using Jarvis.Voice;
using NAudio.Wave;
using System.Text.Json;

internal static class ProbeEvidenceTests
{
    public static async Task Run(Action<bool, string> check)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "test-evidence", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var input = Path.Combine(directory, "input.wav");
        var pcm = Enumerable.Range(0, 3200).Select(x => (byte)(x % 251)).ToArray();
        using (var writer = new WaveFileWriter(input, new WaveFormat(16000, 16, 1))) writer.Write(pcm, 0, pcm.Length);
        var original = File.ReadAllBytes(input);
        var id = Guid.NewGuid();
        var evidence = new ProbeEvidence(Path.Combine(directory, "retained"));
        evidence.Observe("capture_started", new Dictionary<string, object?> { ["turnId"] = id });
        evidence.Observe("capture_ended", new Dictionary<string, object?> { ["turnId"] = id });
        var retained = Path.Combine(evidence.DirectoryPath, $"{id:D}.wav");
        var fake = new ProbeStt((audio, token) =>
        {
            check(File.Exists(retained), "probe copy exists before STT invocation");
            check(File.ReadAllBytes(retained).SequenceEqual(File.ReadAllBytes(audio.WavePath)), "probe WAV is byte-identical to supplied WAV");
            return Task.FromResult(new SttResult("  Open Word.  ", TimeSpan.FromMilliseconds(7)));
        });
        using var cancellation = new CancellationTokenSource();
        var source = new SttAudio(input, "test microphone", id);
        var result = await new EvidenceRetainingSttProvider(fake, evidence).TranscribeAsync(source, cancellation.Token);
        check(fake.Calls == 1 && ReferenceEquals(fake.Audio, source) && fake.Token == cancellation.Token, "probe forwards exact STT request and cancellation unchanged once");
        check(result.Transcript == "  Open Word.  " && File.ReadAllBytes(input).SequenceEqual(original), "probe preserves exact transcript and input WAV");
        File.Delete(input); // Simulates the existing orchestrator/host temporary-file cleanup.
        check(File.Exists(retained) && File.ReadAllBytes(retained).SequenceEqual(original), "retained WAV survives normal turn cleanup");
        var events = File.ReadAllLines(Path.Combine(evidence.DirectoryPath, "probe.jsonl")).Select(x => JsonDocument.Parse(x).RootElement.Clone()).ToArray();
        var metadata = events.Single(x => x.GetProperty("stage").GetString() == "audio_preserved").GetProperty("data");
        check(metadata.GetProperty("pcmByteCount").GetInt64() == 3200 && metadata.GetProperty("sampleRate").GetInt32() == 16000 && metadata.GetProperty("channels").GetInt32() == 1 && metadata.GetProperty("bitsPerSample").GetInt32() == 16, "probe logs measured PCM count and format");
        check(metadata.GetProperty("pcmDurationMs").GetDouble() == 100 && metadata.GetProperty("captureStartUtc").ValueKind == JsonValueKind.String && metadata.GetProperty("captureStopUtc").ValueKind == JsonValueKind.String && metadata.GetProperty("captureDurationMs").GetDouble() >= 0, "probe logs audio duration and observed capture boundaries separately");
        var completed = events.Single(x => x.GetProperty("stage").GetString() == "stt_completed").GetProperty("data");
        check(completed.GetProperty("exactFinalTranscript").GetString() == result.Transcript && completed.GetProperty("sttStartUtc").ValueKind == JsonValueKind.String && completed.GetProperty("sttEndUtc").ValueKind == JsonValueKind.String, "probe logs exact transcript and STT start/end");
        check(events.All(x => x.GetProperty("turnId").GetGuid() == id) && metadata.GetProperty("wavPath").GetString() == retained, "probe filename and metadata preserve Turn ID");
        var failureId = Guid.NewGuid();
        var failureAudio = new SttAudio(retained, "test microphone", failureId);
        var failure = new ProbeStt((_, _) => Task.FromException<SttResult>(new InvalidOperationException("test failure")));
        bool threw = false;
        try { await new EvidenceRetainingSttProvider(failure, evidence).TranscribeAsync(failureAudio); } catch (InvalidOperationException) { threw = true; }
        check(threw && File.Exists(Path.Combine(evidence.DirectoryPath, $"{failureId:D}.wav")), "STT failure propagates with diagnostic WAV retained");
        check(File.ReadAllLines(Path.Combine(evidence.DirectoryPath, "probe.jsonl")).Any(x => x.Contains("stt_failed")), "STT failure records its end timestamp");
    }
    private sealed class ProbeStt(Func<SttAudio, CancellationToken, Task<SttResult>> callback) : ISttProvider
    {
        public int Calls; public SttAudio? Audio; public CancellationToken Token;
        public Task<SttResult> TranscribeAsync(SttAudio audio, CancellationToken cancellationToken = default)
        { Calls++; Audio = audio; Token = cancellationToken; return callback(audio, cancellationToken); }
    }
}
