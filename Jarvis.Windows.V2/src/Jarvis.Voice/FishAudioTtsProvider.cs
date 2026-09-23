using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Jarvis.Voice;

public sealed class FishAudioTtsProvider : ITtsProvider
{
    private static readonly Uri Endpoint = new("https://api.fish.audio/v1/tts");
    private readonly HttpClient client;
    private readonly FishAudioOptions options;
    private readonly IVoiceDiagnosticSink diagnostics;

    public FishAudioTtsProvider(HttpClient client, FishAudioOptions options, IVoiceDiagnosticSink diagnostics)
    {
        this.client = client; this.options = options; this.diagnostics = diagnostics;
    }

    public async Task<TtsAudio> SynthesizeAsync(string text, CancellationToken cancellationToken = default)
    {
        if (!options.IsConfigured) throw new InvalidOperationException("Fish Audio requires FISH_AUDIO_API_KEY and FISH_AUDIO_REFERENCE_ID.");
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text is required.", nameof(text));

        diagnostics.Write("tts_request_started", new Dictionary<string, object?> { ["provider"] = "fish_audio", ["model"] = options.ModelId, ["textLength"] = text.Length });
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(new { text, reference_id = options.ReferenceId, format = "mp3" }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Headers.TryAddWithoutValidation("model", options.ModelId);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var sanitized = body.Length > 512 ? body[..512] : body;
            diagnostics.Write("tts_http_failed", new Dictionary<string, object?> { ["statusCode"] = (int)response.StatusCode, ["response"] = sanitized });
            throw new HttpRequestException($"Fish Audio request failed with HTTP {(int)response.StatusCode}.", null, response.StatusCode);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (bytes.Length == 0) throw new InvalidDataException("Fish Audio returned an empty audio response.");
        diagnostics.Write("tts_audio_received", new Dictionary<string, object?> { ["statusCode"] = (int)response.StatusCode, ["contentType"] = contentType, ["byteCount"] = bytes.Length });
        return new TtsAudio(bytes, contentType, ".mp3", options.ModelId ?? "s2.1-pro-free");
    }
}
