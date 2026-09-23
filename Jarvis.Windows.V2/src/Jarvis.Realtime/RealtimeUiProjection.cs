using System.Text;
using System.Text.Json;

namespace Jarvis.Realtime;

public enum RealtimeUiProjectionKind
{
    None,
    Connected,
    Disconnected,
    UserTranscriptStarted,
    UserTranscriptUpdated,
    UserTranscriptCompleted,
    AssistantResponseStarted,
    AssistantResponseUpdated,
    AssistantResponseCompleted,
    AssistantInterrupted
}

public sealed record RealtimeUiProjection(RealtimeUiProjectionKind Kind, string Text = "", string Id = "");

public sealed class RealtimeUiProjector
{
    private readonly TranscriptAccumulator user = new();
    private readonly TranscriptAccumulator assistant = new();

    public RealtimeUiProjection ProjectTransport(string type) => type switch
    {
        "data.channel.open" => new(RealtimeUiProjectionKind.Connected),
        "closed" => new(RealtimeUiProjectionKind.Disconnected),
        "fault" => new(RealtimeUiProjectionKind.Disconnected),
        _ => new(RealtimeUiProjectionKind.None)
    };

    public RealtimeUiProjection ProjectServerEvent(string json)
    {
        using var document = ParseSafely(json);
        if (document is null)
            return new(RealtimeUiProjectionKind.None);

        var root = document.RootElement;
        var type = GetString(root, "type");

        if (type is "input_audio_buffer.speech_started")
        {
            var id = FirstNonEmpty(root, "item_id", "event_id");
            user.Begin(string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id);
            return new(RealtimeUiProjectionKind.UserTranscriptStarted, string.Empty, user.ActiveId);
        }

        if (type is "conversation.item.input_audio_transcription.delta")
        {
            var id = FirstNonEmpty(root, "item_id", "event_id");
            var delta = FirstNonEmpty(root, "delta", "text", "transcript");
            var text = user.Append(id, delta);
            return string.IsNullOrEmpty(delta)
                ? new(RealtimeUiProjectionKind.None)
                : new(RealtimeUiProjectionKind.UserTranscriptUpdated, text, user.ActiveId);
        }

        if (type is "conversation.item.input_audio_transcription.completed")
        {
            var id = FirstNonEmpty(root, "item_id", "event_id");
            var transcript = FirstNonEmpty(root, "transcript", "text");
            var text = user.Complete(id, transcript);
            return string.IsNullOrWhiteSpace(text)
                ? new(RealtimeUiProjectionKind.None)
                : new(RealtimeUiProjectionKind.UserTranscriptCompleted, text, user.ActiveId);
        }

        if (type is "conversation.item.created" or "conversation.item.done" or "response.output_item.done")
        {
            var role = FindFirstString(root, "role");
            var transcript = FindFirstString(root, "transcript", "text");
            if (string.IsNullOrWhiteSpace(transcript))
                return new(RealtimeUiProjectionKind.None);

            var id = FirstNonEmpty(root, "item_id", "id", "event_id");
            if (string.Equals(role, "user", StringComparison.OrdinalIgnoreCase))
                return new(RealtimeUiProjectionKind.UserTranscriptCompleted, user.Complete(id, transcript), user.ActiveId);

            if (string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase))
                return new(RealtimeUiProjectionKind.AssistantResponseCompleted, assistant.Complete(id, transcript), assistant.ActiveId);
        }

        if (type is "response.created")
        {
            var id = FirstNonEmpty(root, "response_id") is { Length: > 0 } directId
                ? directId
                : ReadNestedString(root, "response", "id");
            if (string.IsNullOrWhiteSpace(id))
                id = FirstNonEmpty(root, "event_id");
            assistant.Begin(string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id);
            return new(RealtimeUiProjectionKind.AssistantResponseStarted, string.Empty, assistant.ActiveId);
        }

        if (type is "response.output_audio_transcript.delta" or "response.audio_transcript.delta" or "response.output_text.delta" or "response.text.delta")
        {
            var id = FirstNonEmpty(root, "response_id", "item_id", "event_id");
            var delta = FirstNonEmpty(root, "delta", "text");
            var text = assistant.Append(id, delta);
            return string.IsNullOrEmpty(delta)
                ? new(RealtimeUiProjectionKind.None)
                : new(RealtimeUiProjectionKind.AssistantResponseUpdated, text, assistant.ActiveId);
        }

        if (type is "response.output_audio_transcript.done" or "response.audio_transcript.done" or "response.output_text.done" or "response.text.done")
        {
            var id = FirstNonEmpty(root, "response_id", "item_id", "event_id");
            var transcript = FirstNonEmpty(root, "transcript", "text");
            var text = assistant.Complete(id, transcript);
            return string.IsNullOrWhiteSpace(text)
                ? new(RealtimeUiProjectionKind.None)
                : new(RealtimeUiProjectionKind.AssistantResponseCompleted, text, assistant.ActiveId);
        }

        if (type is "response.done")
        {
            var id = FirstNonEmpty(root, "response_id", "id") is { Length: > 0 } directId
                ? directId
                : ReadNestedString(root, "response", "id");
            if (string.IsNullOrWhiteSpace(id))
                id = FirstNonEmpty(root, "event_id");
            var text = FindFirstString(root, "transcript", "text");
            if (!string.IsNullOrWhiteSpace(text))
                return new(RealtimeUiProjectionKind.AssistantResponseCompleted, assistant.Complete(id, text), assistant.ActiveId);

            var status = ReadNestedString(root, "response", "status");
            if (status is "cancelled" or "incomplete")
                return new(RealtimeUiProjectionKind.AssistantInterrupted, assistant.CurrentText, assistant.ActiveId);
        }

        if (type is "response.cancelled")
            return new(RealtimeUiProjectionKind.AssistantInterrupted, assistant.CurrentText, assistant.ActiveId);

        if (type is "session.created" or "session.updated")
            return new(RealtimeUiProjectionKind.Connected);

        return new(RealtimeUiProjectionKind.None);
    }

    private static JsonDocument? ParseSafely(string json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string FirstNonEmpty(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            var value = GetString(root, name);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return string.Empty;
    }

    private static string GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string ReadNestedString(JsonElement root, string parent, string name) =>
        root.ValueKind == JsonValueKind.Object &&
        root.TryGetProperty(parent, out var parentElement) &&
        parentElement.ValueKind == JsonValueKind.Object &&
        parentElement.TryGetProperty(name, out var element) &&
        element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? string.Empty
            : string.Empty;

    private static string FindFirstString(JsonElement root, params string[] names)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (names.Contains(property.Name, StringComparer.Ordinal) && property.Value.ValueKind == JsonValueKind.String)
                    return property.Value.GetString() ?? string.Empty;

                var nested = FindFirstString(property.Value, names);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                var nested = FindFirstString(item, names);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }

        return string.Empty;
    }

    private sealed class TranscriptAccumulator
    {
        private readonly StringBuilder text = new();
        public string ActiveId { get; private set; } = string.Empty;
        public string CurrentText => text.ToString();

        public void Begin(string id)
        {
            ActiveId = id;
            text.Clear();
        }

        public string Append(string id, string delta)
        {
            if (string.IsNullOrEmpty(delta))
                return CurrentText;

            if (string.IsNullOrWhiteSpace(ActiveId))
                Begin(string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id);
            else if (!string.IsNullOrWhiteSpace(id) && !string.Equals(id, ActiveId, StringComparison.Ordinal))
                return CurrentText;

            text.Append(delta);
            return CurrentText;
        }

        public string Complete(string id, string finalText)
        {
            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(ActiveId) && !string.Equals(id, ActiveId, StringComparison.Ordinal))
                Begin(id);
            else if (string.IsNullOrWhiteSpace(ActiveId))
                Begin(string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id);

            if (!string.IsNullOrWhiteSpace(finalText))
            {
                text.Clear();
                text.Append(finalText);
            }

            return CurrentText;
        }
    }
}
