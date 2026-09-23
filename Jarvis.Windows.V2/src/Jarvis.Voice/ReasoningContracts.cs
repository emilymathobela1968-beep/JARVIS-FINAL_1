namespace Jarvis.Voice;
public interface IJarvisReasoner { Task<string> RespondAsync(string transcript, Guid turnId, CancellationToken cancellationToken = default); }
public sealed class DeterministicGate3Reasoner : IJarvisReasoner
{
    public Task<string> RespondAsync(string transcript, Guid turnId, CancellationToken cancellationToken = default) =>
        Task.FromResult(string.IsNullOrWhiteSpace(transcript) ? "I did not receive a clear utterance, Leon." : $"Understood, Leon. You said: {transcript}");
}
