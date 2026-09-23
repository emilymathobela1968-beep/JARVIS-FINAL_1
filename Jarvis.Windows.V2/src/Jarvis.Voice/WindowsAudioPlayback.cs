using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Jarvis.Voice;

public sealed class WindowsAudioPlayback : IAudioPlayback
{
    private readonly IVoiceDiagnosticSink diagnostics;
    public WindowsAudioPlayback(IVoiceDiagnosticSink diagnostics) => this.diagnostics = diagnostics;

    public async Task<PlaybackResult> PlayAsync(TtsAudio audio, CancellationToken cancellationToken = default)
        => await PlayAsync(audio, null, cancellationToken).ConfigureAwait(false);

    public async Task<PlaybackResult> PlayAsync(TtsAudio audio, Guid? turnId, CancellationToken cancellationToken = default)
    {
        var temporaryPath = Path.Combine(Path.GetTempPath(), $"jarvis-gate1-{Guid.NewGuid():N}{audio.FileExtension}");
        await File.WriteAllBytesAsync(temporaryPath, audio.Bytes, cancellationToken).ConfigureAwait(false);
        var device = GetDefaultOutputDevice();
        try
        {
            using var reader = new MediaFoundationReader(temporaryPath);
            using var output = new WaveOutEvent();
            var completed = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
            output.PlaybackStopped += (_, args) => completed.TrySetResult(args.Exception);
            output.Init(reader);
            diagnostics.Write("playback_queued", new Dictionary<string, object?> { ["contentType"] = audio.ContentType, ["byteCount"] = audio.Bytes.Length, ["outputDevice"] = device });
            output.Play();
            diagnostics.Write("playback_started", new Dictionary<string, object?> { ["turnId"] = turnId, ["outputDevice"] = device });
            using var registration = cancellationToken.Register(() => { diagnostics.Write("playback_cancel_requested", new Dictionary<string, object?> { ["turnId"] = turnId }); output.Stop(); });
            var failure = await completed.Task.ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested)
            {
                diagnostics.Write("playback_stopped", new Dictionary<string, object?> { ["turnId"] = turnId, ["reason"] = "caller cancellation", ["outputDevice"] = device });
                return new PlaybackResult(false, true, "caller cancellation", device);
            }
            if (failure is not null) throw failure;
            diagnostics.Write("playback_completed", new Dictionary<string, object?> { ["outputDevice"] = device });
            return new PlaybackResult(true, false, null, device);
        }
        finally { try { File.Delete(temporaryPath); } catch { /* Diagnostic file cleanup never changes playback outcome. */ } }
    }

    private static string GetDefaultOutputDevice()
    {
        using var enumerator = new MMDeviceEnumerator();
        using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        return device.FriendlyName;
    }
}
