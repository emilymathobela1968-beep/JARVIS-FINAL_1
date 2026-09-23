using NAudio.Wave;
namespace Jarvis.Voice;

public sealed class WindowsBargeInDetector : IAsyncDisposable
{
    public sealed record Detection(Guid TurnId, byte[] PreRollPcm16);
    private readonly IVoiceDiagnosticSink diagnostics;
    private WaveInEvent? capture;
    private TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Queue<byte[]> preRoll = new();
    private int bytes, speechFrames, stopping;
    private Detection? pending;
    public event EventHandler<Detection>? SpeechDetected;
    public WindowsBargeInDetector(IVoiceDiagnosticSink diagnostics) { this.diagnostics = diagnostics; released.TrySetResult(); }
    public void Start(Guid turnId)
    {
        if (capture is not null) throw new InvalidOperationException("Barge-in detector is already running.");
        if (WaveIn.DeviceCount < 1) throw new InvalidOperationException("No Windows microphone input device is available.");
        preRoll.Clear(); bytes = speechFrames = stopping = 0; pending = null;
        released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var current = new WaveInEvent { DeviceNumber = 0, WaveFormat = new WaveFormat(16000, 16, 1), BufferMilliseconds = 30 };
        capture = current;
        current.DataAvailable += (_, e) =>
        {
            if (Volatile.Read(ref stopping) != 0) return;
            var frame = e.Buffer[..e.BytesRecorded]; preRoll.Enqueue(frame); bytes += frame.Length;
            while (bytes > 22400 && preRoll.TryDequeue(out var dropped)) bytes -= dropped.Length;
            long energy = 0;
            for (int i = 0; i + 1 < frame.Length; i += 2) energy += Math.Abs((int)BitConverter.ToInt16(frame, i));
            var average = frame.Length < 2 ? 0 : energy / (frame.Length / 2);
            speechFrames = average > 850 ? speechFrames + 1 : 0;
            if (speechFrames < 3 || Interlocked.Exchange(ref stopping, 1) != 0) return;
            pending = new Detection(turnId, preRoll.SelectMany(x => x).ToArray());
            diagnostics.Write("barge_in_detected", new Dictionary<string, object?> { ["turnId"] = turnId, ["preRollBytes"] = pending.PreRollPcm16.Length });
            current.StopRecording();
        };
        current.RecordingStopped += (_, e) =>
        {
            // Dispose off the capture callback; completion means the device handle is released.
            _ = Task.Run(() =>
            {
                try
                {
                    current.Dispose(); capture = null;
                    diagnostics.Write("detector_capture_released", new Dictionary<string, object?> { ["turnId"] = turnId });
                    if (e.Exception is not null) { released.TrySetException(e.Exception); return; }
                    var detection = pending;
                    if (detection is not null) SpeechDetected?.Invoke(this, detection);
                    released.TrySetResult();
                }
                catch (Exception ex) { released.TrySetException(ex); }
            });
        };
        try { current.StartRecording(); }
        catch { current.Dispose(); capture = null; released.TrySetResult(); throw; }
    }
    public async Task StopAsync()
    {
        var current = capture;
        if (current is not null && Interlocked.Exchange(ref stopping, 1) == 0) current.StopRecording();
        await released.Task.ConfigureAwait(false);
    }
    public void Stop() => StopAsync().GetAwaiter().GetResult();
    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);
}
