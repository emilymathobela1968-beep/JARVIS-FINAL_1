using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Jarvis.Voice;

public sealed class FasterWhisperSttProvider : ISttProvider
{
    private const string DefaultPython = @"C:\Jarvis_Envs\Jarvis_Grok_Voice\Scripts\python.exe";
    private readonly IVoiceDiagnosticSink diagnostics;
    public FasterWhisperSttProvider(IVoiceDiagnosticSink diagnostics) => this.diagnostics = diagnostics;

    public async Task<SttResult> TranscribeAsync(SttAudio audio, CancellationToken cancellationToken = default)
    {
        var python = Environment.GetEnvironmentVariable("JARVIS_GATE2_PYTHON") ?? DefaultPython;
        var helper = Path.Combine(AppContext.BaseDirectory, "gate2_faster_whisper.py");
        if (!File.Exists(python)) throw new FileNotFoundException("Validated local faster-whisper Python environment is unavailable.", python);
        if (!File.Exists(helper)) throw new FileNotFoundException("Gate 2 faster-whisper helper is unavailable.", helper);
        diagnostics.Write("transcription_started", new Dictionary<string, object?> { ["turnId"] = audio.TurnId, ["provider"] = "faster-whisper", ["model"] = "small.en" });
        var start = Stopwatch.StartNew();
        using var process = Process.Start(new ProcessStartInfo(python) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true, ArgumentList = { helper, audio.WavePath } }) ?? throw new InvalidOperationException("Could not start local faster-whisper.");
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            SttFailureDiagnostics.Preserve(audio, stderr, AppContext.BaseDirectory);
            throw new InvalidOperationException($"Local faster-whisper failed: {Sanitize(stderr)}");
        }
        var transcript = JsonDocument.Parse(stdout).RootElement.GetProperty("transcript").GetString()?.Trim() ?? string.Empty;
        start.Stop();
        diagnostics.Write("transcription_completed", new Dictionary<string, object?> { ["turnId"] = audio.TurnId, ["elapsedMs"] = start.ElapsedMilliseconds, ["finalTranscript"] = transcript });
        return new SttResult(transcript, start.Elapsed);
    }
    private static string Sanitize(string text) => string.IsNullOrWhiteSpace(text) ? "unknown local STT error" : text.Trim()[..Math.Min(512, text.Trim().Length)];
}

public sealed record SttFailureDiagnosticRecord(
    Guid TurnId,
    string FailureDirectory,
    string? PreservedWavePath,
    string FullStderrPath,
    string MetadataPath);

public static class SttFailureDiagnostics
{
    private static readonly Regex SecretAssignment = new(
        @"(?im)(?<name>FISH_AUDIO_API_KEY|FISH_AUDIO_REFERENCE_ID)\s*=\s*(?<value>[^\r\n;]+)",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    public static SttFailureDiagnosticRecord Preserve(SttAudio audio, string stderr, string baseDirectory)
    {
        var safeTurnId = audio.TurnId.ToString("N");
        var root = Path.Combine(baseDirectory, "logs", "stt-failures", safeTurnId);
        Directory.CreateDirectory(root);

        string? preservedWavePath = null;
        if (File.Exists(audio.WavePath))
        {
            preservedWavePath = Path.Combine(root, $"failure-{safeTurnId}.wav");
            File.Copy(audio.WavePath, preservedWavePath, overwrite: true);
        }

        var fullStderrPath = Path.Combine(root, "faster-whisper-stderr.txt");
        File.WriteAllText(fullStderrPath, RedactSecrets(stderr));

        var metadataPath = Path.Combine(root, "failure.json");
        var metadata = new
        {
            timestampUtc = DateTimeOffset.UtcNow,
            audio.TurnId,
            audio.InputDevice,
            originalWavePath = audio.WavePath,
            preservedWavePath,
            fullStderrPath,
            originalWaveExisted = preservedWavePath is not null
        };
        File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));

        return new SttFailureDiagnosticRecord(audio.TurnId, root, preservedWavePath, fullStderrPath, metadataPath);
    }

    public static string RedactSecrets(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return SecretAssignment.Replace(text, match => $"{match.Groups["name"].Value}=<REDACTED>");
    }
}
