using Jarvis.Governance;

namespace Jarvis.Realtime;

public enum WebRtcTransportState { NotConfigured, AwaitingEphemeralCredential, WebViewReady, MicStreamReady, PeerConnectionEstablished, DataChannelOpen, RemoteAudioPlaying, Interrupted, Faulted, Closed }

public sealed record RealtimeEphemeralCredential(
    string ClientSecret,
    Uri CallsEndpoint,
    DateTimeOffset ExpiresAt);

public sealed record WebRtcTransportEvidence(
    Guid SessionId,
    WebRtcTransportState State,
    bool MicStreamStarted,
    bool PeerConnectionEstablished,
    bool DataChannelOpen,
    bool RemoteAudioTrackReceived,
    bool RemoteAudioPlaybackStarted,
    bool InterruptSupported,
    string Reason);

public sealed record WebRtcBridgeMessage(string Type, Guid SessionId, string? Payload = null);

public static class WebRtcCredentialPolicy
{
    private static readonly string[] LongLivedMarkers = ["api_key", "apikey", "authorization", "bearer ", "azure_openai_api_key", "openai_api_key"];

    public static bool IsSafeForBrowser(RealtimeEphemeralCredential credential)
    {
        if (string.IsNullOrWhiteSpace(credential.ClientSecret)) return false;
        if (credential.ExpiresAt <= DateTimeOffset.UtcNow) return false;
        return !LongLivedMarkers.Any(marker => credential.ClientSecret.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    public static string RedactForAudit(string text) => GovernanceText.RedactSecrets(text);
}

public sealed class WebRtcTransportEvidenceBuilder(Guid sessionId)
{
    private bool mic;
    private bool peer;
    private bool data;
    private bool remoteTrack;
    private bool playback;
    private bool interrupt;
    private WebRtcTransportState state = WebRtcTransportState.NotConfigured;
    private string reason = "not_started";

    public WebRtcTransportEvidenceBuilder Apply(WebRtcBridgeMessage message)
    {
        reason = message.Type;
        switch (message.Type)
        {
            case "webview.ready":
                state = WebRtcTransportState.WebViewReady;
                break;
            case "mic.stream.started":
                mic = true;
                state = WebRtcTransportState.MicStreamReady;
                break;
            case "peer.connection.established":
                peer = true;
                state = WebRtcTransportState.PeerConnectionEstablished;
                break;
            case "data.channel.open":
                data = true;
                state = WebRtcTransportState.DataChannelOpen;
                break;
            case "remote.audio.track":
                remoteTrack = true;
                break;
            case "remote.audio.playing":
                playback = true;
                state = WebRtcTransportState.RemoteAudioPlaying;
                break;
            case "interrupt.sent":
                interrupt = true;
                state = WebRtcTransportState.Interrupted;
                break;
            case "fault":
                state = WebRtcTransportState.Faulted;
                break;
            case "closed":
                state = WebRtcTransportState.Closed;
                break;
        }
        return this;
    }

    public WebRtcTransportEvidence Build() =>
        new(sessionId, state, mic, peer, data, remoteTrack, playback, interrupt, reason);
}

public sealed class WebRtcReadinessGate
{
    public bool IsPhysicalRealtimeReady(WebRtcTransportEvidence evidence) =>
        evidence.MicStreamStarted &&
        evidence.PeerConnectionEstablished &&
        evidence.DataChannelOpen &&
        evidence.RemoteAudioTrackReceived &&
        evidence.RemoteAudioPlaybackStarted;

    public RealtimeProviderStatus ToProviderStatus(WebRtcTransportEvidence evidence) =>
        IsPhysicalRealtimeReady(evidence)
            ? new(true, true, "WebRTC Realtime", "physical_webrtc_audio_ready", [])
            : new(true, false, "WebRTC Realtime", evidence.Reason, ["mic stream", "RTCPeerConnection", "data channel", "remote audio track", "remote audio playback"]);
}
