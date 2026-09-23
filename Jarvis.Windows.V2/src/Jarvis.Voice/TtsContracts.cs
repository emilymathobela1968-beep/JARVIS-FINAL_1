namespace Jarvis.Voice;

public sealed record TtsAudio(byte[] Bytes, string ContentType, string FileExtension, string Model);
public sealed record PlaybackResult(bool Completed, bool Cancelled, string? CancellationReason, string OutputDevice);

public interface ITtsProvider
{
    Task<TtsAudio> SynthesizeAsync(string text, CancellationToken cancellationToken = default);
}

public interface IAudioPlayback
{
    Task<PlaybackResult> PlayAsync(TtsAudio audio, CancellationToken cancellationToken = default);
}

public interface IVoiceDiagnosticSink
{
    void Write(string eventName, IReadOnlyDictionary<string, object?> fields);
}
