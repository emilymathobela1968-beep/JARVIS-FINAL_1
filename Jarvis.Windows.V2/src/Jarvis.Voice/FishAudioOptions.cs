namespace Jarvis.Voice;

/// <summary>Secrets are loaded from the process environment; they are never persisted or logged.</summary>
public sealed class FishAudioOptions
{
    public const string ApiKeyEnvironmentVariable = "FISH_AUDIO_API_KEY";
    public const string ReferenceIdEnvironmentVariable = "FISH_AUDIO_REFERENCE_ID";
    public const string ModelEnvironmentVariable = "FISH_AUDIO_MODEL";
    public string? ApiKey { get; init; }
    public string? ReferenceId { get; init; }
    public string? ModelId { get; init; }
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ReferenceId);

    public static FishAudioOptions FromEnvironment() => new()
    {
        ApiKey = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable),
        ReferenceId = Environment.GetEnvironmentVariable(ReferenceIdEnvironmentVariable),
        ModelId = Environment.GetEnvironmentVariable(ModelEnvironmentVariable) ?? "s2.1-pro-free",
    };
}
