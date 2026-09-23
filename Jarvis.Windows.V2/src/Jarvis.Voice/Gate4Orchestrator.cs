using NAudio.Wave;
namespace Jarvis.Voice;

/// <summary>Serial Gate 4 coordinator. Controller remains the authority for every turn.</summary>
public sealed class Gate4Orchestrator
{
    private readonly VoiceSessionController session;
    private readonly IVoiceDiagnosticSink log;
    private int interrupting;
    public Gate4Orchestrator(VoiceSessionController session, IVoiceDiagnosticSink log) { this.session = session; this.log = log; }

    public async Task<VoiceTurn?> AcceptBargeInAsync(WindowsBargeInDetector.Detection detection, CancellationToken cancellationToken = default, Task? oldPlayback = null)
    {
        if (Interlocked.CompareExchange(ref interrupting, 1, 0) != 0) return null;
        try
        {
            if (!session.IsCurrent(detection.TurnId)) { LogRejected(detection.TurnId, "barge_in"); return null; }
            var old = await session.InterruptAsync("barge-in", cancellationToken).ConfigureAwait(false);
            if (old is null) return null;
            if (oldPlayback is not null) await oldPlayback.ConfigureAwait(false);
            var replacement = await session.BeginTurnAsync(cancellationToken).ConfigureAwait(false);
            log.Write("new_turn_created", new Dictionary<string, object?> { ["oldTurnId"] = old, ["turnId"] = replacement.Id, ["preRollBytes"] = detection.PreRollPcm16.Length });
            return replacement;
        }
        finally { Volatile.Write(ref interrupting, 0); }
    }

    private void LogRejected(Guid id, string callback) => log.Write("stale_turn_rejected", new Dictionary<string, object?> { ["turnId"] = id, ["callback"] = callback });
    private bool Current(VoiceTurn turn, string callback)
    {
        if (session.IsCurrent(turn.Id)) return true;
        LogRejected(turn.Id, callback); return false;
    }

    public async Task<bool> RunReplacementAsync(VoiceTurn turn, byte[] preRoll, IReplacementCapture capture, ISttProvider stt,
        IJarvisReasoner reasoner, Func<Guid, string, CancellationToken, Task<TtsAudio>> tts,
        Func<Guid, TtsAudio, CancellationToken, Task<PlaybackResult>> playback)
    {
        if (!Current(turn, "capture_requested")) return false;
        var captured = await capture.CaptureAsync(turn.Id, preRoll, turn.CancellationToken).ConfigureAwait(false);
        if (!Current(turn, "capture_completed") || captured.TurnId != turn.Id) return false;
        var path = Path.Combine(Path.GetTempPath(), $"jarvis-gate4-{turn.Id:N}.wav");
        try
        {
            using (var writer = new WaveFileWriter(path, new WaveFormat(16000, 16, 1))) writer.Write(captured.Pcm16, 0, captured.Pcm16.Length);
            return await RunTurnAsync(turn, new SttAudio(path, captured.InputDevice, turn.Id), stt, reasoner, tts, playback).ConfigureAwait(false);
        }
        finally { File.Delete(path); }
    }

    public async Task<bool> RunTurnAsync(VoiceTurn turn, SttAudio audio, ISttProvider stt, IJarvisReasoner reasoner,
        Func<Guid, string, CancellationToken, Task<TtsAudio>> tts,
        Func<Guid, TtsAudio, CancellationToken, Task<PlaybackResult>> playback)
    {
        if (!Current(turn, "stt_requested")) return false;
        await session.MarkTranscribingAsync(turn.Id);
        var transcript = await stt.TranscribeAsync(audio, turn.CancellationToken).ConfigureAwait(false);
        if (!Current(turn, "stt_completed")) return false;
        await session.MarkThinkingAsync(turn.Id);
        var response = await reasoner.RespondAsync(transcript.Transcript, turn.Id, turn.CancellationToken).ConfigureAwait(false);
        if (!Current(turn, "reasoner_completed")) return false;
        var speech = await tts(turn.Id, response, turn.CancellationToken).ConfigureAwait(false);
        if (!Current(turn, "tts_completed")) return false;
        await session.MarkSpeakingAsync(turn.Id);
        var result = await playback(turn.Id, speech, turn.CancellationToken).ConfigureAwait(false);
        if (!Current(turn, "playback_completed") || !result.Completed || result.Cancelled) return false;
        return await session.CompleteTurnAsync(turn.Id).ConfigureAwait(false);
    }

    public static byte[] AssembleReplacementPcm(byte[] preRoll, byte[] continuation)
    {
        var joined = new byte[preRoll.Length + continuation.Length]; Buffer.BlockCopy(preRoll, 0, joined, 0, preRoll.Length); Buffer.BlockCopy(continuation, 0, joined, preRoll.Length, continuation.Length); return joined;
    }
}
