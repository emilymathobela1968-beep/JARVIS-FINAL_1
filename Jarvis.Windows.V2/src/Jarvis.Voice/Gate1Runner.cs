namespace Jarvis.Voice;

public sealed record Gate1RunResult(int Attempt, bool HttpSucceeded, bool PlaybackCompleted, string? Error);

public sealed class Gate1Runner
{
    public const string TestText = "Hello Leon. This is Jarvis. I am testing my new voice system. This entire sentence must play from beginning to end without interruption.";
    private readonly ITtsProvider tts;
    private readonly IAudioPlayback playback;

    public Gate1Runner(ITtsProvider tts, IAudioPlayback playback) { this.tts = tts; this.playback = playback; }

    public async Task<IReadOnlyList<Gate1RunResult>> RunAsync(int attempts = 10, CancellationToken cancellationToken = default)
    {
        if (attempts != 10) throw new ArgumentOutOfRangeException(nameof(attempts), "Gate 1 requires exactly ten attempts.");
        var results = new List<Gate1RunResult>(attempts);
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                var audio = await tts.SynthesizeAsync(TestText, cancellationToken).ConfigureAwait(false);
                var outcome = await playback.PlayAsync(audio, cancellationToken).ConfigureAwait(false);
                results.Add(new Gate1RunResult(attempt, true, outcome.Completed, outcome.CancellationReason));
                if (!outcome.Completed) break;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                results.Add(new Gate1RunResult(attempt, false, false, ex.Message));
                break;
            }
        }
        return results;
    }
}
