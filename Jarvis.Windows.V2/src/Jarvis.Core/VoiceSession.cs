using Jarvis.Contracts;

namespace Jarvis.Core;

public sealed record VoiceSessionStatus(
    bool IsRunning,
    string? State,
    string? Detail,
    string? LastEvent,
    string? LastError);
