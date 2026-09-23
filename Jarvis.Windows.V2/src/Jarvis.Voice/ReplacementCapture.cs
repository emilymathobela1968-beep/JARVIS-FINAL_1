using NAudio.Wave;
namespace Jarvis.Voice;
public sealed record ReplacementCaptureResult(Guid TurnId, byte[] Pcm16, string InputDevice);
public interface IReplacementCapture { Task<ReplacementCaptureResult> CaptureAsync(Guid turnId, byte[] preRollPcm16, CancellationToken cancellationToken = default); }

public sealed class WindowsReplacementCapture : IReplacementCapture
{
    private readonly IVoiceDiagnosticSink log;
    public WindowsReplacementCapture(IVoiceDiagnosticSink log) => this.log = log;
    public async Task<ReplacementCaptureResult> CaptureAsync(Guid turnId, byte[] preRollPcm16, CancellationToken cancellationToken = default)
    {
        void Log(string name, int? count = null) => log.Write(name, new Dictionary<string, object?> { ["turnId"] = turnId, ["byteCount"] = count });
        Log("replacement_capture_requested");
        cancellationToken.ThrowIfCancellationRequested();
        if (WaveIn.DeviceCount < 1) throw new InvalidOperationException("No Windows microphone input device is available.");
        var device = WaveIn.GetCapabilities(0).ProductName;
        using var capture = new WaveInEvent { DeviceNumber = 0, WaveFormat = new WaveFormat(16000, 16, 1), BufferMilliseconds = 50 };
        using var pcm = new MemoryStream(); pcm.Write(preRollPcm16);
        var stopped = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        int first = 0;
        capture.DataAvailable += (_, e) =>
        {
            if (e.BytesRecorded == 0) return;
            if (Interlocked.Exchange(ref first, 1) == 0) Log("replacement_first_continuation_pcm", e.BytesRecorded);
            pcm.Write(e.Buffer, 0, e.BytesRecorded);
        };
        capture.RecordingStopped += (_, e) => stopped.TrySetResult(e.Exception);
        capture.StartRecording(); Log("replacement_mic_acquired");
        using var registration = cancellationToken.Register(capture.StopRecording);
        Console.WriteLine("Barge-in accepted. Continue speaking, then press Enter.");
        try
        {
            var enter = Console.In.ReadLineAsync(cancellationToken).AsTask();
            await Task.WhenAny(enter, stopped.Task).ConfigureAwait(false);
            if (enter.IsCompleted) await enter.ConfigureAwait(false);
        }
        finally { capture.StopRecording(); await stopped.Task.ConfigureAwait(false); }
        cancellationToken.ThrowIfCancellationRequested();
        if (await stopped.Task.ConfigureAwait(false) is { } error) throw error;
        var result = pcm.ToArray(); Log("replacement_capture_completed", result.Length);
        return new ReplacementCaptureResult(turnId, result, device);
    }
}
