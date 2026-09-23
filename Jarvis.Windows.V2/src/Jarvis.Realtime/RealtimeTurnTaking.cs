namespace Jarvis.Realtime;

using System.Text.Json;
using System.Text.RegularExpressions;

public enum RealtimeTurnEvidenceKind
{
    None,
    UserSpeechStarted,
    UserSpeechStopped,
    ThinkingGraceEntered,
    SpeechResumedDuringGrace,
    GraceExpiredUserTurnCommitted,
    AssistantResponseStarted,
    AssistantResponseCompleted,
    AssistantInterrupted,
    AssistantCancellationRequested,
    AssistantAudioFlushRequested,
    NewUserUtteranceAccepted
}

public sealed record RealtimeTurnEvidence(RealtimeTurnEvidenceKind Kind, string Reason = "");
public sealed record RealtimeUserTurnDecision(bool Accepted, string Reason, string TurnId, string ItemId, string Transcript);

public sealed class RealtimeTurnTakingPolicy
{
    public const int ThinkingGraceMs = 1800;
    private bool inUserGrace;
    private bool assistantSpeaking;

    public IReadOnlyList<RealtimeTurnEvidence> Observe(string eventType)
    {
        var evidence = new List<RealtimeTurnEvidence>();
        switch (eventType)
        {
            case "input_audio_buffer.speech_started":
                if (assistantSpeaking)
                {
                    assistantSpeaking = false;
                    inUserGrace = false;
                    evidence.Add(new(RealtimeTurnEvidenceKind.AssistantInterrupted, "assistant_speaking_user_started"));
                    evidence.Add(new(RealtimeTurnEvidenceKind.AssistantCancellationRequested, "interrupt_response_true"));
                    evidence.Add(new(RealtimeTurnEvidenceKind.AssistantAudioFlushRequested, "provider_managed_webrtc_audio"));
                    evidence.Add(new(RealtimeTurnEvidenceKind.NewUserUtteranceAccepted, "barge_in_user_speech_started"));
                }
                else if (inUserGrace)
                {
                    inUserGrace = false;
                    evidence.Add(new(RealtimeTurnEvidenceKind.SpeechResumedDuringGrace, "same_user_turn"));
                }

                evidence.Add(new(RealtimeTurnEvidenceKind.UserSpeechStarted));
                break;

            case "input_audio_buffer.speech_stopped":
                inUserGrace = true;
                evidence.Add(new(RealtimeTurnEvidenceKind.UserSpeechStopped));
                evidence.Add(new(RealtimeTurnEvidenceKind.ThinkingGraceEntered, $"{ThinkingGraceMs}ms"));
                break;

            case "input_audio_buffer.committed":
                if (inUserGrace)
                {
                    inUserGrace = false;
                    evidence.Add(new(RealtimeTurnEvidenceKind.GraceExpiredUserTurnCommitted, "server_vad_commit"));
                }
                break;

            case "response.created":
                if (inUserGrace)
                {
                    inUserGrace = false;
                    evidence.Add(new(RealtimeTurnEvidenceKind.GraceExpiredUserTurnCommitted, "response_created"));
                }

                assistantSpeaking = true;
                evidence.Add(new(RealtimeTurnEvidenceKind.AssistantResponseStarted));
                break;

            case "response.done":
                assistantSpeaking = false;
                evidence.Add(new(RealtimeTurnEvidenceKind.AssistantResponseCompleted));
                break;
        }

        return evidence;
    }
}

public sealed class RealtimeUserTurnGate
{
    private readonly HashSet<string> completedItems = new(StringComparer.Ordinal);
    private readonly HashSet<string> respondedTurns = new(StringComparer.Ordinal);
    private string lastAcceptedTranscript = string.Empty;
    private string lastAssistantTranscript = string.Empty;

    public bool AssistantPlaybackActive { get; private set; }
    public bool AssistantResponseActive { get; private set; }

    public void SetAssistantPlaybackActive(bool active) => AssistantPlaybackActive = active;

    public void ObserveAssistantResponseStarted() => AssistantResponseActive = true;

    public void ObserveAssistantResponseCompleted() => AssistantResponseActive = false;

    public void ObserveAssistantTranscript(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
            lastAssistantTranscript = text;
    }

    public RealtimeUserTurnDecision EvaluateInputTranscriptionCompleted(string eventJson)
    {
        using var document = JsonDocument.Parse(eventJson);
        var root = document.RootElement;
        var itemId = ReadString(root, "item_id");
        var transcript = ReadString(root, "transcript").Trim();
        if (string.IsNullOrWhiteSpace(itemId))
            return Reject("invalid", itemId, transcript);
        if (string.IsNullOrWhiteSpace(transcript))
            return Reject("empty", itemId, transcript);
        if (!completedItems.Add(itemId))
            return Reject("duplicate", itemId, transcript);
        if ((AssistantPlaybackActive || AssistantResponseActive) && IsNearSame(transcript, lastAssistantTranscript))
            return Reject("playback_echo", itemId, transcript);
        if (IsNearSame(transcript, lastAcceptedTranscript))
            return Reject("duplicate", itemId, transcript);

        lastAcceptedTranscript = transcript;
        return new(true, "accepted", StableTurnId(itemId), itemId, transcript);
    }

    public bool TryMarkResponseStarted(string turnId)
    {
        if (string.IsNullOrWhiteSpace(turnId))
            return false;
        return respondedTurns.Add(turnId);
    }

    private static RealtimeUserTurnDecision Reject(string reason, string itemId, string transcript) =>
        new(false, reason, string.IsNullOrWhiteSpace(itemId) ? string.Empty : StableTurnId(itemId), itemId, transcript);

    private static string StableTurnId(string itemId) => "turn_" + itemId;

    private static string ReadString(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object &&
        root.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static bool IsNearSame(string left, string right)
    {
        var a = Normalize(left);
        var b = Normalize(right);
        if (a.Length < 12 || b.Length < 12)
            return false;
        if (string.Equals(a, b, StringComparison.Ordinal))
            return true;
        if (a.Contains(b, StringComparison.Ordinal) || b.Contains(a, StringComparison.Ordinal))
            return true;

        var aTokens = a.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var bTokens = b.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        if (aTokens.Count == 0 || bTokens.Count == 0)
            return false;
        var overlap = aTokens.Intersect(bTokens, StringComparer.Ordinal).Count();
        return overlap / (double)Math.Max(aTokens.Count, bTokens.Count) >= 0.82;
    }

    private static string Normalize(string value) =>
        Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9 ]", " ").Trim();
}
