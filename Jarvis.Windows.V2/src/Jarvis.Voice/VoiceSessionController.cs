namespace Jarvis.Voice;

public enum VoiceSessionState { Stopped, Starting, Listening, UserSpeaking, Transcribing, Thinking, Speaking, Interrupting, Recovering, Faulted }

public sealed record VoiceTurn(Guid Id, CancellationToken CancellationToken);
public sealed record VoiceStateChanged(VoiceSessionState Previous, VoiceSessionState Current, Guid? TurnId, string Reason);
public sealed record StaleTurnRejected(Guid TurnId, string Callback);

/// <summary>Single owner of turn lifetime and voice-state transitions. Device, STT and TTS adapters never change state themselves.</summary>
public sealed class VoiceSessionController : IAsyncDisposable
{
    private readonly SemaphoreSlim transitions = new(1, 1);
    private CancellationTokenSource? turnCancellation;
    private Guid? currentTurnId;
    private VoiceSessionState state = VoiceSessionState.Stopped;

    public event EventHandler<VoiceStateChanged>? StateChanged;
    public event EventHandler<StaleTurnRejected>? StaleTurnRejected;
    public VoiceSessionState State => state;
    public Guid? CurrentTurnId => currentTurnId;
    public bool IsCurrent(Guid turnId) => currentTurnId == turnId && turnCancellation?.IsCancellationRequested == false;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await TransitionAsync(VoiceSessionState.Starting, "start requested", cancellationToken);
        await TransitionAsync(VoiceSessionState.Listening, "voice session ready", cancellationToken);
    }

    public async Task<VoiceTurn> BeginTurnAsync(CancellationToken cancellationToken = default)
    {
        await transitions.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            CancelCurrentTurnUnsafe();
            turnCancellation = new CancellationTokenSource();
            currentTurnId = Guid.NewGuid();
            ChangeStateUnsafe(VoiceSessionState.UserSpeaking, "new turn", currentTurnId);
            return new VoiceTurn(currentTurnId.Value, turnCancellation.Token);
        }
        finally { transitions.Release(); }
    }

    public Task MarkTranscribingAsync(Guid turnId, CancellationToken cancellationToken = default) => TransitionForCurrentTurnAsync(turnId, VoiceSessionState.Transcribing, "transcribing", cancellationToken);
    public Task MarkThinkingAsync(Guid turnId, CancellationToken cancellationToken = default) => TransitionForCurrentTurnAsync(turnId, VoiceSessionState.Thinking, "reasoning", cancellationToken);
    public Task MarkSpeakingAsync(Guid turnId, CancellationToken cancellationToken = default) => TransitionForCurrentTurnAsync(turnId, VoiceSessionState.Speaking, "playback started", cancellationToken);

    public async Task<bool> CompleteTurnAsync(Guid turnId, CancellationToken cancellationToken = default)
    {
        await transitions.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (currentTurnId != turnId || turnCancellation?.IsCancellationRequested != false) { StaleTurnRejected?.Invoke(this, new(turnId, "completion")); return false; }
            turnCancellation.Dispose(); turnCancellation = null; currentTurnId = null;
            ChangeStateUnsafe(VoiceSessionState.Listening, "turn completed", null);
            return true;
        }
        finally { transitions.Release(); }
    }

    public async Task<Guid?> InterruptAsync(string reason, CancellationToken cancellationToken = default)
    {
        await transitions.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var interrupted = currentTurnId;
            if (interrupted is null) return null;
            ChangeStateUnsafe(VoiceSessionState.Interrupting, reason, interrupted);
            CancelCurrentTurnUnsafe();
            ChangeStateUnsafe(VoiceSessionState.Listening, "interrupted; awaiting new utterance", null);
            return interrupted;
        }
        finally { transitions.Release(); }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await transitions.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { CancelCurrentTurnUnsafe(); ChangeStateUnsafe(VoiceSessionState.Stopped, "stop requested", null); }
        finally { transitions.Release(); }
    }

    private async Task TransitionForCurrentTurnAsync(Guid turnId, VoiceSessionState next, string reason, CancellationToken cancellationToken)
    {
        await transitions.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (currentTurnId != turnId || turnCancellation?.IsCancellationRequested != false) { StaleTurnRejected?.Invoke(this, new(turnId, next.ToString())); return; }
            ChangeStateUnsafe(next, reason, turnId);
        }
        finally { transitions.Release(); }
    }

    private async Task TransitionAsync(VoiceSessionState next, string reason, CancellationToken cancellationToken)
    {
        await transitions.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { ChangeStateUnsafe(next, reason, currentTurnId); }
        finally { transitions.Release(); }
    }

    private void CancelCurrentTurnUnsafe()
    {
        turnCancellation?.Cancel(); turnCancellation?.Dispose(); turnCancellation = null; currentTurnId = null;
    }

    private void ChangeStateUnsafe(VoiceSessionState next, string reason, Guid? turnId)
    {
        var previous = state; state = next;
        StateChanged?.Invoke(this, new VoiceStateChanged(previous, next, turnId, reason));
    }

    public async ValueTask DisposeAsync() { await StopAsync(); transitions.Dispose(); }
}
