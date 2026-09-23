using System.Net.Http.Json;
using System.Text.Json;

namespace Jarvis.Realtime;

public sealed record AzureRealtimeOptions(
    string? Endpoint,
    string? Deployment,
    string? ApiKey,
    bool PreferWebRtc,
    string TranscriptionModel = "whisper-1",
    string TranscriptionLanguage = "en",
    string Voice = "marin",
    string Instructions = RealtimeSessionContract.DefaultInstructions);

public static class AzureRealtimeOptionsProvider
{
    public static AzureRealtimeOptions FromEnvironment() => JarvisRealtimeConfigurationProvider.Load().Options;
}

public sealed class AzureRealtimeProviderBoundary(AzureRealtimeOptions options)
{
    public RealtimeProviderStatus GetStatus()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Endpoint)) missing.Add("AZURE_OPENAI_REALTIME_ENDPOINT");
        if (string.IsNullOrWhiteSpace(options.Deployment)) missing.Add("AZURE_OPENAI_REALTIME_DEPLOYMENT");
        if (string.IsNullOrWhiteSpace(options.ApiKey)) missing.Add("AZURE_OPENAI_API_KEY or Entra token provider");

        if (missing.Count > 0)
            return new(false, false, "Azure/OpenAI GA Realtime", "configuration_missing", missing);

        return new(true, false, "Azure/OpenAI GA Realtime", "awaiting_webrtc_physical_evidence", ["WebView2 transport evidence", "microphone stream", "remote audio playback"]);
    }
}

public sealed class AzureRealtimeEphemeralSecretBroker(HttpClient http, AzureRealtimeOptions options, IRealtimeAuditSink audit)
{
    public RealtimeProviderStatus GetStatus() => new AzureRealtimeProviderBoundary(options).GetStatus();

    public async Task<RealtimeEphemeralCredential> MintAsync(
        Guid sessionId,
        IReadOnlyList<RealtimeToolDefinition> tools,
        CancellationToken cancellationToken = default)
    {
        var status = GetStatus();
        if (!status.Configured)
            throw new InvalidOperationException($"Realtime configuration missing: {string.Join(", ", status.RequiredConfiguration)}");

        var clientSecretsEndpoint = BuildEndpoint("/openai/v1/realtime/client_secrets");
        // Azure's browser filter suppresses the function-call events consumed by the native governed router.
        var callsEndpoint = BuildEndpoint("/openai/v1/realtime/calls");
        using var request = new HttpRequestMessage(HttpMethod.Post, clientSecretsEndpoint)
        {
            Content = JsonContent.Create(RealtimeSessionContract.BuildClientSecretPayload(options, tools))
        };
        request.Headers.TryAddWithoutValidation("api-key", options.ApiKey);

        audit.Write(sessionId, Guid.NewGuid(), "realtime_client_secret_requested", new
        {
            endpoint = clientSecretsEndpoint.GetLeftPart(UriPartial.Path),
            deployment = options.Deployment,
            tools = tools.Select(tool => tool.Name).ToArray()
        });

        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            audit.Write(sessionId, Guid.NewGuid(), "realtime_client_secret_rejected", new { statusCode = (int)response.StatusCode, response.ReasonPhrase });
            throw new InvalidOperationException($"Realtime client secret broker failed: {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        var credential = ParseCredential(body, callsEndpoint);
        if (!WebRtcCredentialPolicy.IsSafeForBrowser(credential))
            throw new InvalidOperationException("Realtime client secret broker returned a credential that is not safe for WebView use.");

        audit.Write(sessionId, Guid.NewGuid(), "realtime_client_secret_minted", new
        {
            callsEndpoint = callsEndpoint.GetLeftPart(UriPartial.Path),
            credentialExpiresAt = credential.ExpiresAt
        });
        return credential;
    }

    private RealtimeEphemeralCredential ParseCredential(string body, Uri callsEndpoint)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var value = ReadString(root, "value")
            ?? ReadString(root, "token")
            ?? ReadNestedString(root, "client_secret", "value")
            ?? ReadNestedString(root, "client_secret", "token");
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Realtime client secret response did not contain an ephemeral value.");

        var expiresAt = ReadUnixTime(root, "expires_at")
            ?? ReadNestedUnixTime(root, "client_secret", "expires_at")
            ?? DateTimeOffset.UtcNow.AddMinutes(1);
        return new(value, callsEndpoint, expiresAt);
    }

    private Uri BuildEndpoint(string pathAndQuery)
    {
        var endpoint = options.Endpoint!.TrimEnd('/');
        return new Uri(endpoint + pathAndQuery, UriKind.Absolute);
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static string? ReadNestedString(JsonElement root, string parent, string name) =>
        root.ValueKind == JsonValueKind.Object &&
        root.TryGetProperty(parent, out var parentElement) &&
        parentElement.ValueKind == JsonValueKind.Object &&
        parentElement.TryGetProperty(name, out var element) &&
        element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static DateTimeOffset? ReadUnixTime(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(name, out var element))
            return null;
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var seconds))
            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        if (element.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(element.GetString(), out var parsed))
            return parsed;
        return null;
    }

    private static DateTimeOffset? ReadNestedUnixTime(JsonElement root, string parent, string name) =>
        root.ValueKind == JsonValueKind.Object &&
        root.TryGetProperty(parent, out var parentElement) &&
        parentElement.ValueKind == JsonValueKind.Object
            ? ReadUnixTime(parentElement, name)
            : null;
}
