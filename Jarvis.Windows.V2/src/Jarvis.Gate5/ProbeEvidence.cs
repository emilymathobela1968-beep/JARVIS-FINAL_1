using System.Security.Cryptography;
using System.Text.Json;
using Jarvis.Voice;
using NAudio.Wave;
namespace Jarvis.Gate5;

/// <summary>Opt-in Gate 5 evidence only. Copies, but never changes, the WAV passed to the existing STT adapter.</summary>
public sealed class ProbeEvidence(string directory)
{
    private readonly object sync = new();
    private readonly Dictionary<Guid, DateTimeOffset> starts = new(), stops = new();
    public string DirectoryPath { get; } = Path.GetFullPath(directory);
    public void Observe(string name, IReadOnlyDictionary<string, object?> fields)
    {
        if (!fields.TryGetValue("turnId", out var value) || value is not Guid turn) return;
        lock (sync)
        {
            var now = DateTimeOffset.UtcNow;
            if (name is "capture_started" or "replacement_capture_requested") starts[turn] = now;
            if (name is "capture_ended" or "replacement_capture_completed") stops[turn] = now;
        }
    }
    public string Preserve(SttAudio audio)
    {
        Directory.CreateDirectory(DirectoryPath);
        var retained = Path.Combine(DirectoryPath, $"{audio.TurnId:D}.wav");
        File.Copy(audio.WavePath, retained, overwrite: false);
        using var reader = new WaveFileReader(retained);
        var format = reader.WaveFormat;
        DateTimeOffset? start, stop;
        lock (sync) { start = starts.TryGetValue(audio.TurnId, out var a) ? a : null; stop = stops.TryGetValue(audio.TurnId, out var b) ? b : null; }
        Write("audio_preserved", audio.TurnId, new {
            captureStartUtc = start, captureStopUtc = stop,
            captureDurationMs = start.HasValue && stop.HasValue ? (stop.Value - start.Value).TotalMilliseconds : (double?)null,
            pcmDurationMs = reader.TotalTime.TotalMilliseconds, pcmByteCount = reader.Length,
            sampleRate = format.SampleRate, channels = format.Channels, bitsPerSample = format.BitsPerSample,
            wavByteCount = new FileInfo(retained).Length, wavPath = retained, suppliedWavPath = audio.WavePath,
            sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(retained)))
        });
        Console.WriteLine($"DIAGNOSTIC_WAV [{audio.TurnId}]: {retained}");
        return retained;
    }
    public void Write(string stage, Guid turnId, object data)
    {
        lock (sync)
        {
            Directory.CreateDirectory(DirectoryPath);
            File.AppendAllText(Path.Combine(DirectoryPath, "probe.jsonl"), JsonSerializer.Serialize(new { timestampUtc = DateTimeOffset.UtcNow, stage, turnId, data }) + Environment.NewLine);
        }
    }
}
public sealed class EvidenceRetainingSttProvider(ISttProvider inner, ProbeEvidence evidence) : ISttProvider
{
    public async Task<SttResult> TranscribeAsync(SttAudio audio, CancellationToken cancellationToken = default)
    {
        evidence.Preserve(audio); // Runs before the original provider opens the original, unchanged file.
        var started = DateTimeOffset.UtcNow;
        evidence.Write("stt_started", audio.TurnId, new { sttStartUtc = started, suppliedWavPath = audio.WavePath });
        try
        {
            var result = await inner.TranscribeAsync(audio, cancellationToken).ConfigureAwait(false);
            evidence.Write("stt_completed", audio.TurnId, new { sttStartUtc = started, sttEndUtc = DateTimeOffset.UtcNow, exactFinalTranscript = result.Transcript, providerElapsedMs = result.Elapsed.TotalMilliseconds });
            Console.WriteLine($"FINAL_TRANSCRIPT [{audio.TurnId}]: {JsonSerializer.Serialize(result.Transcript)}");
            return result;
        }
        catch (Exception ex)
        {
            evidence.Write("stt_failed", audio.TurnId, new { sttStartUtc = started, sttEndUtc = DateTimeOffset.UtcNow, exceptionType = ex.GetType().Name });
            throw;
        }
    }
}
