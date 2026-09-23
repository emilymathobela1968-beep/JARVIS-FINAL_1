using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jarvis.Contracts;

public enum VoiceServiceCommandType
{
    StartEngine,
    StartTts,
    StartStt,
    StartRealtimeVoice,
    StopRealtimeVoice,
    StopEngine,
    SendText,
    PttBegin,
    PttEnd,
    Interrupt,
    Shutdown,
    GetDevices,
    GetCapabilities,
}

public sealed record VoiceServiceCommand(
    VoiceServiceCommandType Type,
    string? Text = null,
    string? TurnId = null,
    bool? SpeakReply = null);

public sealed record VoiceServiceEvent(
    string Event,
    [property: JsonPropertyName("event_id")] string? EventId = null,
    double? Timestamp = null,
    string? Detail = null,
    string? State = null,
    string? Error = null,
    string? Component = null,
    string? Provider = null,
    string? Model = null,
    string? Voice = null,
    string? Source = null,
    string? Text = null,
    string? Capability = null,
    bool? Ok = null,
    JsonElement? Result = null,
    [property: JsonPropertyName("turn_id")] string? TurnId = null,
    [property: JsonPropertyName("playback_active")] bool? PlaybackActive = null,
    bool? Final = null);

public sealed record VoiceBridgeResult(
    bool Success,
    string Message,
    string? Error = null);
