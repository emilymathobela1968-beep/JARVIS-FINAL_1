namespace Jarvis.Voice;

public sealed record SttAudio(string WavePath, string InputDevice, Guid TurnId);
public sealed record SttResult(string Transcript, TimeSpan Elapsed);
public interface ISttProvider { Task<SttResult> TranscribeAsync(SttAudio audio, CancellationToken cancellationToken = default); }
