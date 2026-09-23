using NAudio.Wave;

namespace Jarvis.Voice;

public sealed class WindowsMicrophoneCapture
{
    private readonly IVoiceDiagnosticSink diagnostics;
    public WindowsMicrophoneCapture(IVoiceDiagnosticSink diagnostics) => this.diagnostics = diagnostics;

    public async Task<SttAudio> CaptureUntilEnterAsync(Guid turnId, CancellationToken cancellationToken = default)
    {
        if (WaveIn.DeviceCount < 1) throw new InvalidOperationException("No Windows microphone input device is available.");
        var device = WaveIn.GetCapabilities(0).ProductName;
        var path = Path.Combine(Path.GetTempPath(), $"jarvis-gate2-{turnId:N}.wav");
        using var capture = new WaveInEvent { DeviceNumber = 0, WaveFormat = new WaveFormat(16000, 16, 1), BufferMilliseconds = 50 };
        using var writer = new WaveFileWriter(path, capture.WaveFormat);
        capture.DataAvailable += (_, e) => writer.Write(e.Buffer, 0, e.BytesRecorded);
        diagnostics.Write("microphone_initialized", new Dictionary<string, object?> { ["turnId"] = turnId, ["inputDevice"] = device, ["sampleRate"] = 16000 });
        diagnostics.Write("capture_started", new Dictionary<string, object?> { ["turnId"] = turnId, ["inputDevice"] = device });
        capture.StartRecording();
        Console.WriteLine("Recording. Speak one test utterance, then press Enter.");
        await Task.Run(Console.ReadLine, cancellationToken).ConfigureAwait(false);
        capture.StopRecording();
        diagnostics.Write("capture_ended", new Dictionary<string, object?> { ["turnId"] = turnId, ["wavePath"] = path, ["byteCount"] = new FileInfo(path).Length });
        return new SttAudio(path, device, turnId);
    }
}
