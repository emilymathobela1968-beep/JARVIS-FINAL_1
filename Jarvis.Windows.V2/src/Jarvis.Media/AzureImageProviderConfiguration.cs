using Jarvis.Realtime;
using System.Text.Json;

namespace Jarvis.Media;

public sealed record MediaImageLocalConfiguration(string? Endpoint = null, string? Deployment = null, string? ApiVersion = null)
{
    public string? Provider { get; init; }
    public string? Model { get; init; }
}

public sealed record MediaImageConfigurationStatus(bool EndpointPresent, bool DeploymentPresent, bool CredentialPresent, string Source, IReadOnlyList<string> MissingConfiguration)
{
    public bool Configured => EndpointPresent && DeploymentPresent && CredentialPresent;
}

public sealed record AzureImageProviderConfiguration(AzureImageGenerationOptions Options, MediaImageConfigurationStatus Status)
{
    public const string CredentialIdentifier = JarvisRealtimeConfigurationProvider.CredentialIdentifier;
    public const string OpenAiCredentialIdentifier = "Jarvis.Windows.V2/OpenAIImageGeneration";
    public static readonly string DefaultLocalConfigurationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis.Windows.V2", "media-image.local.json");

    public static AzureImageProviderConfiguration Load() => Resolve(new JsonMediaImageLocalConfigStore(DefaultLocalConfigurationPath), new WindowsCredentialSecretStore(CredentialIdentifier), new WindowsCredentialSecretStore(OpenAiCredentialIdentifier));

    public string Provider { get; init; } = "azure";
    public OpenAiImageGenerationOptions OpenAiOptions { get; init; } = new(null, "gpt-image-2", "https://api.openai.com");

    public static AzureImageProviderConfiguration Resolve(IMediaImageLocalConfigStore localStore, IJarvisSecretStore secretStore, IJarvisSecretStore? openAiSecretStore = null)
    {
        var local = localStore.Read();
        var provider = (FirstNonEmpty(Environment.GetEnvironmentVariable("JARVIS_MEDIA_IMAGE_PROVIDER"), local.Provider) ?? "azure").Trim().ToLowerInvariant();
        var endpoint = FirstNonEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_IMAGE_ENDPOINT"), local.Endpoint);
        var deployment = FirstNonEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_IMAGE_DEPLOYMENT"), local.Deployment);
        var apiKey = FirstNonEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_IMAGE_API_KEY"), Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"), secretStore.ReadSecret());
        var apiVersion = FirstNonEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_IMAGE_API_VERSION"), local.ApiVersion) ?? "2024-02-15-preview";
        var openAiModel = FirstNonEmpty(Environment.GetEnvironmentVariable("OPENAI_IMAGE_MODEL"), local.Model) ?? "gpt-image-2";
        var openAiEndpoint = FirstNonEmpty(Environment.GetEnvironmentVariable("OPENAI_IMAGE_ENDPOINT"), "https://api.openai.com")!;
        var openAiKey = FirstNonEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY"), openAiSecretStore?.ReadSecret());
        if (provider == "openai") apiKey = openAiKey;
        var missing = new List<string>();
        if (provider == "openai")
        {
            if (string.IsNullOrWhiteSpace(openAiKey)) missing.Add("OPENAI_API_KEY or secure credential");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(endpoint)) missing.Add("AZURE_OPENAI_IMAGE_ENDPOINT");
            if (string.IsNullOrWhiteSpace(deployment)) missing.Add("AZURE_OPENAI_IMAGE_DEPLOYMENT");
            if (string.IsNullOrWhiteSpace(apiKey)) missing.Add("AZURE_OPENAI_IMAGE_API_KEY or secure credential");
        }
        var sources = new List<string>();
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_OPENAI_IMAGE_ENDPOINT")) || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_OPENAI_IMAGE_DEPLOYMENT")) || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_OPENAI_IMAGE_API_KEY")) || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"))) sources.Add("environment");
        if (!string.IsNullOrWhiteSpace(local.Endpoint) || !string.IsNullOrWhiteSpace(local.Deployment)) sources.Add("local_config");
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_OPENAI_IMAGE_API_KEY")) && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")) && !string.IsNullOrWhiteSpace(apiKey)) sources.Add("windows_credential");
        if (provider == "openai" && !string.IsNullOrWhiteSpace(openAiKey)) sources.Add("openai_credential");
        var status = new MediaImageConfigurationStatus(provider == "openai" ? true : !string.IsNullOrWhiteSpace(endpoint), provider == "openai" ? !string.IsNullOrWhiteSpace(openAiModel) : !string.IsNullOrWhiteSpace(deployment), provider == "openai" ? !string.IsNullOrWhiteSpace(openAiKey) : !string.IsNullOrWhiteSpace(apiKey), sources.Count == 0 ? "missing" : string.Join("+", sources.Distinct()), missing);
        return new(new AzureImageGenerationOptions(endpoint, deployment, apiKey, apiVersion), status)
        {
            Provider = provider is "openai" or "azure" ? provider : "azure",
            OpenAiOptions = new(openAiKey, openAiModel, openAiEndpoint)
        };
    }

    private static string? FirstNonEmpty(params string?[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}

public interface IMediaImageLocalConfigStore { MediaImageLocalConfiguration Read(); }

public sealed class JsonMediaImageLocalConfigStore(string path) : IMediaImageLocalConfigStore
{
    public MediaImageLocalConfiguration Read()
    {
        if (!File.Exists(path)) return new();
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            return new(Read(root, "endpoint"), Read(root, "deployment"), Read(root, "apiVersion")) { Provider = Read(root, "provider"), Model = Read(root, "model") };
        }
        catch (JsonException) { return new(); }
        catch (IOException) { return new(); }
        catch (UnauthorizedAccessException) { return new(); }
    }

    private static string? Read(JsonElement root, string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
