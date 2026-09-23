using Jarvis.Contracts;

namespace Jarvis.Infrastructure;

/// <summary>
/// Temporary compatibility boundary for the copied presentation shell.
/// V2 deliberately does not start, wrap, or reuse the legacy Backtalk Python process.
/// A real V2 adapter must implement the V2 voice contracts and be composed by the session controller.
/// </summary>
public sealed class PythonVoiceBridge : IAsyncDisposable
{
    public event EventHandler<VoiceServiceEvent>? EventReceived;
    public bool IsRunning => false;

    public Task<VoiceBridgeResult> StartAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new VoiceBridgeResult(true, "V2 shell started without a legacy voice bridge."));

    public Task<VoiceBridgeResult> WaitForTtsReadyAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new VoiceBridgeResult(false, "No V2 TTS adapter is configured."));

    public Task<VoiceBridgeResult> SendAsync(VoiceServiceCommand command, CancellationToken cancellationToken = default)
    {
        EventReceived?.Invoke(this, new VoiceServiceEvent("voice_unavailable", Detail: "V2 voice adapters are not configured."));
        return Task.FromResult(new VoiceBridgeResult(false, "V2 voice adapter is unavailable."));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
