using System.Security.Cryptography;
using System.Text.Json;
using System.Net.Http.Json;
using Jarvis.Governance;

namespace Jarvis.Media;

public enum MediaSceneState { Planned, Generating, Candidate, Approved, Rejected, Locked }
public enum MediaProviderState { Available, ProviderUnavailable, AuthorizationRequired }
public enum MediaJobState { Idle, Starting, Generating, CandidateReady, Failed, AuthorizationRequired, Rejected, Approved }
public enum AlexisReferenceState { ReferenceRequired, PendingApproval, Approved }

public sealed record AlexisCharacterAuthority(
    string? CanonicalReferenceImagePath,
    string? ReferenceSha256,
    AlexisReferenceState State,
    IReadOnlyList<string> IdentityLockRequirements,
    IReadOnlyList<string> AllowedVariation,
    IReadOnlyList<string> ProhibitedIdentityChanges)
{
    public static AlexisCharacterAuthority ReferenceRequired() => new(
        null,
        null,
        AlexisReferenceState.ReferenceRequired,
        ["same approved face", "same premium ALEXIS identity", "consistent age, features and presence"],
        ["wardrobe may vary within premium dark diagnostic aesthetic", "environment may vary by workshop scene"],
        ["no face replacement", "no unrelated face.png", "no random sci-fi avatar", "no ethnicity/age drift"]);

    public static AlexisCharacterAuthority FromApprovedReference(string path)
    {
        var canonical = Path.GetFullPath(path);
        if (!File.Exists(canonical)) throw new FileNotFoundException("Canonical ALEXIS reference image is missing.", canonical);
        return ReferenceRequired() with
        {
            CanonicalReferenceImagePath = canonical,
            ReferenceSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(canonical))).ToLowerInvariant(),
            State = AlexisReferenceState.Approved
        };
    }
}

public sealed record MediaScene(
    int Index,
    string Title,
    string StoryBeat,
    string Composition,
    bool AlexisAppears,
    MediaSceneState State,
    string? CurrentCandidateId = null,
    string? LockedAssetPath = null);

public sealed record MediaJob(
    string JobId,
    string Title,
    string VisualDirection,
    AlexisCharacterAuthority AlexisAuthority,
    IReadOnlyList<MediaScene> Scenes,
    string ApprovedInputRoot,
    string ApprovedOutputRoot)
{
    public MediaJobState State { get; init; } = MediaJobState.Idle;
    public int? ActiveSceneIndex { get; init; }
    public DateTimeOffset? OperationStartedUtc { get; init; }
    public string? LastFailureReason { get; init; }
    public DateTimeOffset LastUpdatedUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record ImageGenerationRequest(
    string JobId,
    int SceneIndex,
    string Prompt,
    string OutputDirectory,
    Guid CorrelationId);

public sealed record GeneratedImageCandidate(
    string CandidateId,
    string JobId,
    int SceneIndex,
    string Path,
    string Provider,
    string Prompt,
    string CostEvidence,
    Guid CorrelationId,
    DateTimeOffset CreatedUtc);

public sealed record ImageGenerationResult(
    MediaProviderState ProviderState,
    GeneratedImageCandidate? Candidate,
    string Message,
    string? FailureReason);

public interface IImageGenerationProvider
{
    string Name { get; }
    MediaProviderState GetAvailability();
    Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken = default);
}

public sealed class UnavailableImageGenerationProvider(string name, MediaProviderState state, string reason) : IImageGenerationProvider
{
    public string Name { get; } = name;
    public MediaProviderState GetAvailability() => state;

    public Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ImageGenerationResult(state, null, $"{Name} is not available: {reason}", reason));
}

public sealed record AzureImageGenerationOptions(string? Endpoint, string? Deployment, string? ApiKey, string ApiVersion)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(Deployment) && !string.IsNullOrWhiteSpace(ApiKey);
    public static AzureImageGenerationOptions FromEnvironment() => AzureImageProviderConfiguration.Load().Options;
}

public sealed record OpenAiImageGenerationOptions(string? ApiKey, string Model, string Endpoint)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Model) && !string.IsNullOrWhiteSpace(Endpoint);
}

public sealed class OpenAiImageGenerationProvider(HttpClient http, OpenAiImageGenerationOptions options) : IImageGenerationProvider
{
    public string Name => "OpenAI Images";
    public MediaProviderState GetAvailability() => options.IsConfigured ? MediaProviderState.Available : MediaProviderState.AuthorizationRequired;

    public async Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken = default)
    {
        if (!options.IsConfigured)
            return new(MediaProviderState.AuthorizationRequired, null, "OpenAI image generation is not configured.", "image_provider_configuration_required");
        using var message = new HttpRequestMessage(HttpMethod.Post, options.Endpoint.TrimEnd('/') + "/v1/images/generations");
        message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
        message.Content = JsonContent.Create(new { model = options.Model, prompt = request.Prompt, n = 1, size = "1024x1024" });
        try
        {
            using var response = await http.SendAsync(message, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return new(MediaProviderState.ProviderUnavailable, null, "OpenAI image provider returned a bounded failure.", $"http_{(int)response.StatusCode}");
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken).ConfigureAwait(false);
            var data = document.RootElement.GetProperty("data")[0];
            byte[]? bytes = null;
            if (data.TryGetProperty("b64_json", out var b64) && !string.IsNullOrWhiteSpace(b64.GetString())) bytes = Convert.FromBase64String(b64.GetString()!);
            if (bytes is null && data.TryGetProperty("url", out var url) && Uri.TryCreate(url.GetString(), UriKind.Absolute, out var imageUri)) bytes = await http.GetByteArrayAsync(imageUri, cancellationToken).ConfigureAwait(false);
            if (bytes is null || bytes.Length == 0) return new(MediaProviderState.ProviderUnavailable, null, "Image provider returned no candidate data.", "candidate_missing");
            Directory.CreateDirectory(request.OutputDirectory);
            var path = Path.Combine(request.OutputDirectory, $"scene-{request.SceneIndex:00}-{Guid.NewGuid():N}.png");
            await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
            return new(MediaProviderState.Available, new GeneratedImageCandidate(string.Empty, request.JobId, request.SceneIndex, path, Name, request.Prompt, $"model:{options.Model}", request.CorrelationId, DateTimeOffset.UtcNow), "Image candidate generated.", null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or FormatException)
        {
            return new(MediaProviderState.ProviderUnavailable, null, "Image provider request failed.", ex.GetType().Name);
        }
    }
}

public static class MediaImageProviderSelector
{
    public static IImageGenerationProvider Select(HttpClient http, AzureImageProviderConfiguration configuration) =>
        configuration.Provider == "openai"
            ? new OpenAiImageGenerationProvider(http, configuration.OpenAiOptions)
            : new AzureOpenAiImageGenerationProvider(http, configuration.Options);
}

public sealed class AzureOpenAiImageGenerationProvider(HttpClient http, AzureImageGenerationOptions options) : IImageGenerationProvider
{
    public string Name => "Azure OpenAI Images";
    public MediaProviderState GetAvailability() => options.IsConfigured ? MediaProviderState.Available : MediaProviderState.AuthorizationRequired;

    public async Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken = default)
    {
        if (!options.IsConfigured)
            return new(MediaProviderState.AuthorizationRequired, null, "Azure OpenAI image generation is not configured.", "image_provider_configuration_required");

        var endpoint = options.Endpoint!.TrimEnd('/') + $"/openai/deployments/{Uri.EscapeDataString(options.Deployment!)} /images/generations?api-version={Uri.EscapeDataString(options.ApiVersion)}".Replace("%20", string.Empty, StringComparison.Ordinal);
        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint);
        message.Headers.Add("api-key", options.ApiKey);
        message.Content = JsonContent.Create(new { prompt = request.Prompt, n = 1, size = "1024x1024", response_format = "b64_json" });
        try
        {
            using var response = await http.SendAsync(message, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new(MediaProviderState.ProviderUnavailable, null, "Azure OpenAI image provider returned a bounded failure.", $"http_{(int)response.StatusCode}");
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken).ConfigureAwait(false);
            var data = document.RootElement.GetProperty("data")[0];
            var encoded = data.TryGetProperty("b64_json", out var b64) ? b64.GetString() : null;
            if (string.IsNullOrWhiteSpace(encoded)) return new(MediaProviderState.ProviderUnavailable, null, "Image provider returned no candidate data.", "candidate_missing");
            Directory.CreateDirectory(request.OutputDirectory);
            var path = Path.Combine(request.OutputDirectory, $"scene-{request.SceneIndex:00}-{Guid.NewGuid():N}.png");
            await File.WriteAllBytesAsync(path, Convert.FromBase64String(encoded), cancellationToken).ConfigureAwait(false);
            return new(MediaProviderState.Available, new GeneratedImageCandidate(string.Empty, request.JobId, request.SceneIndex, path, Name, request.Prompt, "provider_reported", request.CorrelationId, DateTimeOffset.UtcNow), "Image candidate generated.", null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or FormatException)
        {
            return new(MediaProviderState.ProviderUnavailable, null, "Image provider request failed.", ex.GetType().Name);
        }
    }
}

public sealed record MediaStatusProjection(MediaJobState State, string Label, bool ActivityVisible, string? FailureReason, string? CandidatePath);

public static class MediaStatusProjectionRules
{
    public static MediaStatusProjection Project(MediaJob job, GeneratedImageCandidate? candidate = null) => job.State switch
    {
        MediaJobState.Starting => new(job.State, "Preparing image generation...", true, null, null),
        MediaJobState.Generating => new(job.State, "Generating image", true, null, null),
        MediaJobState.CandidateReady => new(job.State, "Candidate ready", false, null, candidate?.Path),
        MediaJobState.AuthorizationRequired => new(job.State, "Image generation authorization required", false, job.LastFailureReason, null),
        MediaJobState.Failed => new(job.State, "Generation failed", false, job.LastFailureReason, null),
        _ => new(job.State, job.State.ToString(), false, job.LastFailureReason, candidate?.Path)
    };
}

public sealed class AlexisPromoManifestFactory
{
    private static readonly (string Title, string Beat, string Composition, bool Alexis)[] SceneSpecs =
    [
        ("vehicle/problem arrival", "A troubled vehicle enters the premium diagnostic workshop.", "wide 16:9 establishing shot, vehicle three-quarter front, technician nearby, dark graphite workshop", false),
        ("complex diagnostic evidence", "The technician is confronted by dense, conflicting diagnostic evidence.", "medium-wide workshop bay, tablet and scan evidence visible, concerned technician", false),
        ("ALEXIS introduced", "ALEXIS appears as the diagnostic intelligence guiding the investigation.", "premium interface glow and approved ALEXIS identity presence, grounded not fantasy", true),
        ("vehicle identification", "ALEXIS identifies the exact vehicle and configuration.", "vehicle VIN/identity workflow, technician and vehicle continuity", true),
        ("full system/network scan", "ALEXIS performs a full system and network scan.", "dashboard-like scan evidence over realistic workshop context", true),
        ("DTC prioritization", "Primary faults are separated from secondary evidence.", "ranked DTC evidence, clear primary-versus-secondary visual hierarchy", true),
        ("live-data analysis", "ALEXIS interprets live data under operating conditions.", "oscilloscope/live graph feel, technician observing measured values", true),
        ("circuit/wiring evidence", "Circuit and wiring evidence narrows the suspected fault path.", "wiring diagram and loom context, restrained blue diagnostic illumination", true),
        ("guided physical measurement", "The technician performs a guided physical measurement.", "multimeter/probe action, clear instruction focus, vehicle bay", true),
        ("connector/pin identification", "The exact connector and pin are identified for confirmation.", "close-up connector/pin with AR-style restrained overlay", true),
        ("fault isolated", "The root cause is isolated with evidence.", "decisive diagnostic conclusion, technician confidence returns", true),
        ("repair procedure", "ALEXIS guides the correct repair procedure.", "hands-on repair moment, clean professional workshop process", true),
        ("post-repair verification", "Post-repair checks prove the fault is resolved.", "verification scan, measured evidence, no warning chaos", true),
        ("successful system validation", "All systems validate successfully after repair.", "calm validated dashboard, vehicle ready, technician relieved", true),
        ("broader workshop intelligence", "ALEXIS shows broader workshop intelligence across cases and systems.", "premium operations wall, multiple diagnostic streams, grounded corporate scale", true),
        ("final ALEXIS hero/platform reveal", "The film ends with a premium ALEXIS platform reveal.", "cinematic hero frame, approved ALEXIS identity and platform presence, dark premium reveal", true)
    ];

    public MediaJob CreateAlexisPromo001(string approvedInputRoot, string approvedOutputRoot, AlexisCharacterAuthority? authority = null)
    {
        var scenes = SceneSpecs.Select((scene, i) => new MediaScene(
            i + 1,
            scene.Title,
            scene.Beat,
            scene.Composition,
            scene.Alexis,
            MediaSceneState.Planned)).ToArray();

        return new(
            "ALEXIS_PROMO_001",
            "ALEXIS Diagnostic Intelligence Promo",
            "dark premium corporate workshop; deep black/graphite; restrained blue diagnostic illumination; realistic automotive environment; cinematic 16:9",
            authority ?? AlexisCharacterAuthority.ReferenceRequired(),
            scenes,
            Path.GetFullPath(approvedInputRoot),
            Path.GetFullPath(approvedOutputRoot));
    }
}

public sealed class MediaJobStore(JsonMediaAudit audit)
{
    private readonly Dictionary<string, MediaJob> jobs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GeneratedImageCandidate> candidates = new(StringComparer.OrdinalIgnoreCase);

    public void Save(MediaJob job)
    {
        ValidateRoots(job);
        jobs[job.JobId] = job;
        audit.Write(Guid.NewGuid(), "media_job_saved", new { job.JobId, scenes = job.Scenes.Count, job.AlexisAuthority.State });
    }

    public MediaJob? Get(string jobId) => jobs.TryGetValue(jobId, out var job) ? job : null;

    public MediaJob BeginGeneration(string jobId, int sceneIndex, Guid correlationId)
    {
        var job = GetRequired(jobId);
        var updated = job with { State = MediaJobState.Starting, ActiveSceneIndex = sceneIndex, OperationStartedUtc = DateTimeOffset.UtcNow, LastFailureReason = null, LastUpdatedUtc = DateTimeOffset.UtcNow };
        jobs[jobId] = updated;
        audit.Write(correlationId, "media_generation_starting", new { jobId, sceneIndex });
        return updated;
    }

    public MediaJob MarkGenerating(string jobId, int sceneIndex, Guid correlationId)
    {
        var job = GetRequired(jobId);
        var updated = job with { State = MediaJobState.Generating, ActiveSceneIndex = sceneIndex, LastUpdatedUtc = DateTimeOffset.UtcNow };
        jobs[jobId] = updated;
        audit.Write(correlationId, "media_generation_started", new { jobId, sceneIndex });
        return updated;
    }

    public MediaJob MarkFailed(string jobId, string reason, Guid correlationId, MediaJobState state = MediaJobState.Failed)
    {
        var job = GetRequired(jobId);
        var updated = job with { State = state, LastFailureReason = GovernanceText.RedactSecrets(reason), LastUpdatedUtc = DateTimeOffset.UtcNow };
        jobs[jobId] = updated;
        audit.Write(correlationId, "media_generation_failed", new { jobId, state, reason = updated.LastFailureReason });
        return updated;
    }

    public GeneratedImageCandidate RegisterCandidate(MediaJob job, int sceneIndex, string candidatePath, string provider, string prompt, Guid correlationId, string costEvidence = "none")
    {
        ValidateRoots(job);
        var path = Path.GetFullPath(candidatePath);
        if (!IsInside(job.ApprovedInputRoot, path) && !IsInside(job.ApprovedOutputRoot, path))
            throw new UnauthorizedAccessException("Candidate path is outside approved media roots.");
        if (!File.Exists(path)) throw new FileNotFoundException("Candidate image is missing.", path);
        if (!job.Scenes.Any(scene => scene.Index == sceneIndex)) throw new InvalidOperationException("Scene index is not part of this job.");

        var candidate = new GeneratedImageCandidate(Guid.NewGuid().ToString("N"), job.JobId, sceneIndex, path, provider, prompt, costEvidence, correlationId, DateTimeOffset.UtcNow);
        candidates[candidate.CandidateId] = candidate;
        var updated = UpdateScene(job.JobId, sceneIndex, scene => scene with { State = MediaSceneState.Candidate, CurrentCandidateId = candidate.CandidateId });
        jobs[job.JobId] = updated with { State = MediaJobState.CandidateReady, ActiveSceneIndex = sceneIndex, LastUpdatedUtc = DateTimeOffset.UtcNow };
        audit.Write(correlationId, "media_candidate_registered", new { job.JobId, sceneIndex, candidate.CandidateId, provider });
        return candidate;
    }

    public MediaJob ApproveCandidate(string jobId, int sceneIndex, string candidateId, Guid correlationId)
    {
        if (!candidates.TryGetValue(candidateId, out var candidate)) throw new InvalidOperationException("Candidate is unknown.");
        if (!string.Equals(candidate.JobId, jobId, StringComparison.OrdinalIgnoreCase) || candidate.SceneIndex != sceneIndex)
            throw new InvalidOperationException("Candidate does not belong to the requested scene.");
        var job = UpdateScene(jobId, sceneIndex, scene => scene with { State = MediaSceneState.Approved, CurrentCandidateId = candidateId }) with { State = MediaJobState.Approved, LastUpdatedUtc = DateTimeOffset.UtcNow };
        audit.Write(correlationId, "media_candidate_approved", new { jobId, sceneIndex, candidateId });
        return job;
    }

    public MediaJob RejectCandidate(string jobId, int sceneIndex, string candidateId, string reason, Guid correlationId)
    {
        if (!candidates.ContainsKey(candidateId)) throw new InvalidOperationException("Candidate is unknown.");
        var job = UpdateScene(jobId, sceneIndex, scene => scene with { State = MediaSceneState.Rejected, CurrentCandidateId = null }) with { State = MediaJobState.Rejected, LastUpdatedUtc = DateTimeOffset.UtcNow };
        audit.Write(correlationId, "media_candidate_rejected", new { jobId, sceneIndex, candidateId, reason = GovernanceText.RedactSecrets(reason) });
        return job;
    }

    public MediaJob LockScene(string jobId, int sceneIndex, Guid correlationId)
    {
        var job = GetRequired(jobId);
        var scene = job.Scenes.Single(x => x.Index == sceneIndex);
        if (scene.State != MediaSceneState.Approved || string.IsNullOrWhiteSpace(scene.CurrentCandidateId))
            throw new InvalidOperationException("Scene cannot be locked before Leon approves a candidate.");
        var candidate = candidates[scene.CurrentCandidateId];
        var locked = Path.Combine(job.ApprovedOutputRoot, $"{sceneIndex:00}.png");
        File.Copy(candidate.Path, locked, overwrite: true);
        var updated = UpdateScene(jobId, sceneIndex, s => s with { State = MediaSceneState.Locked, LockedAssetPath = locked });
        audit.Write(correlationId, "media_scene_locked", new { jobId, sceneIndex, locked });
        return updated;
    }

    private MediaJob UpdateScene(string jobId, int sceneIndex, Func<MediaScene, MediaScene> update)
    {
        var job = GetRequired(jobId);
        var scenes = job.Scenes.Select(scene => scene.Index == sceneIndex ? update(scene) : scene).ToArray();
        var updated = job with { Scenes = scenes };
        jobs[jobId] = updated;
        return updated;
    }

    private MediaJob GetRequired(string jobId) => jobs.TryGetValue(jobId, out var job) ? job : throw new InvalidOperationException("Media job is unknown.");

    private static void ValidateRoots(MediaJob job)
    {
        Directory.CreateDirectory(job.ApprovedInputRoot);
        Directory.CreateDirectory(job.ApprovedOutputRoot);
    }

    private static bool IsInside(string root, string candidate)
    {
        var canonicalRoot = EnsureTrailingSeparator(Path.GetFullPath(root));
        var canonical = Path.GetFullPath(candidate);
        return canonical.StartsWith(canonicalRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
}

public sealed class MediaGovernedTools(MediaJobStore store, IImageGenerationProvider provider) : IGovernedTool
{
    public GovernedToolDefinition Definition { get; } = new("generate_media_image", "Generate or request a media image candidate for a scene.", ["job_id", "scene_index", "prompt", "output_directory"]);

    public async Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var jobId = ReadString(request.Arguments, "job_id");
        var sceneIndex = ReadInt(request.Arguments, "scene_index");
        var prompt = ReadString(request.Arguments, "prompt");
        var outputDirectory = ReadString(request.Arguments, "output_directory");
        var job = store.Get(jobId);
        if (job is null)
            return new(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Rejected, "Media job is unknown.", null, "media_job_unknown");
        if (job.AlexisAuthority.State != AlexisReferenceState.Approved && job.Scenes.Single(s => s.Index == sceneIndex).AlexisAppears)
            return new(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.ReferenceRequired, "ALEXIS canonical reference is required before generating this scene.", null, "alexis_reference_required");
        if (provider.GetAvailability() == MediaProviderState.AuthorizationRequired)
        {
            store.MarkFailed(jobId, "Image provider authorization is required.", request.CorrelationId, MediaJobState.AuthorizationRequired);
            return new(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.AuthorizationRequired, "Image provider authorization is required. No generation job is running.", store.Get(jobId), "provider_authorization_required");
        }
        if (provider.GetAvailability() == MediaProviderState.ProviderUnavailable)
        {
            store.MarkFailed(jobId, "Image provider is unavailable.", request.CorrelationId);
            return new(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.ProviderUnavailable, "Image provider is unavailable. No generation job is running.", store.Get(jobId), "provider_unavailable");
        }

        store.BeginGeneration(jobId, sceneIndex, request.CorrelationId);
        store.MarkGenerating(jobId, sceneIndex, request.CorrelationId);
        var generated = await provider.GenerateAsync(new(jobId, sceneIndex, prompt, outputDirectory, request.CorrelationId), cancellationToken).ConfigureAwait(false);
        if (generated.Candidate is null)
        {
            var failureState = generated.ProviderState == MediaProviderState.AuthorizationRequired ? MediaJobState.AuthorizationRequired : MediaJobState.Failed;
            store.MarkFailed(jobId, generated.FailureReason ?? generated.Message, request.CorrelationId, failureState);
            return new(request.ConversationId, request.CorrelationId, request.Name, generated.ProviderState == MediaProviderState.AuthorizationRequired ? GovernedToolStatus.AuthorizationRequired : GovernedToolStatus.ProviderUnavailable, generated.Message, store.Get(jobId), generated.FailureReason);
        }
        var candidate = store.RegisterCandidate(job, sceneIndex, generated.Candidate.Path, generated.Candidate.Provider, prompt, request.CorrelationId, generated.Candidate.CostEvidence);
        return new(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Succeeded, "Media image candidate registered.", candidate);
    }

    private static string ReadString(JsonElement args, string name) => args.GetProperty(name).GetString() ?? string.Empty;
    private static int ReadInt(JsonElement args, string name) => args.GetProperty(name).GetInt32();
}

public sealed class GetMediaJobTool(MediaJobStore store) : IGovernedTool
{
    public GovernedToolDefinition Definition { get; } = new("get_media_job", "Read media job status and scene states.", ["job_id"]);

    public Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var jobId = request.Arguments.GetProperty("job_id").GetString() ?? string.Empty;
        var job = store.Get(jobId);
        return Task.FromResult(job is null
            ? new GovernedToolResult(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Rejected, "Media job is unknown.", null, "media_job_unknown")
            : new GovernedToolResult(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Succeeded, "Media job status returned.", job));
    }
}

public sealed class RegisterMediaCandidateTool(MediaJobStore store) : IGovernedTool
{
    public GovernedToolDefinition Definition { get; } = new("register_media_candidate", "Register an externally generated image as a scene candidate.", ["job_id", "scene_index", "candidate_path", "provider", "prompt"]);

    public Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var jobId = request.Arguments.GetProperty("job_id").GetString() ?? string.Empty;
        var job = store.Get(jobId);
        if (job is null)
            return Task.FromResult(new GovernedToolResult(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Rejected, "Media job is unknown.", null, "media_job_unknown"));
        var candidate = store.RegisterCandidate(
            job,
            request.Arguments.GetProperty("scene_index").GetInt32(),
            request.Arguments.GetProperty("candidate_path").GetString() ?? string.Empty,
            request.Arguments.GetProperty("provider").GetString() ?? "external",
            request.Arguments.GetProperty("prompt").GetString() ?? string.Empty,
            request.CorrelationId,
            "external_or_manual");
        return Task.FromResult(new GovernedToolResult(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Succeeded, "Media candidate registered.", candidate));
    }
}

public sealed class ApproveMediaCandidateTool(MediaJobStore store) : IGovernedTool
{
    public GovernedToolDefinition Definition { get; } = new("approve_media_candidate", "Record Leon approval for a media candidate.", ["job_id", "scene_index", "candidate_id"]);

    public Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var job = store.ApproveCandidate(
            request.Arguments.GetProperty("job_id").GetString() ?? string.Empty,
            request.Arguments.GetProperty("scene_index").GetInt32(),
            request.Arguments.GetProperty("candidate_id").GetString() ?? string.Empty,
            request.CorrelationId);
        return Task.FromResult(new GovernedToolResult(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Succeeded, "Media candidate approved by Leon.", job));
    }
}

public sealed class RejectMediaCandidateTool(MediaJobStore store) : IGovernedTool
{
    public GovernedToolDefinition Definition { get; } = new("reject_media_candidate", "Reject a media candidate and permit controlled regeneration.", ["job_id", "scene_index", "candidate_id", "reason"]);

    public Task<GovernedToolResult> InvokeAsync(GovernedToolRequest request, CancellationToken cancellationToken = default)
    {
        var job = store.RejectCandidate(
            request.Arguments.GetProperty("job_id").GetString() ?? string.Empty,
            request.Arguments.GetProperty("scene_index").GetInt32(),
            request.Arguments.GetProperty("candidate_id").GetString() ?? string.Empty,
            request.Arguments.GetProperty("reason").GetString() ?? string.Empty,
            request.CorrelationId);
        return Task.FromResult(new GovernedToolResult(request.ConversationId, request.CorrelationId, request.Name, GovernedToolStatus.Succeeded, "Media candidate rejected.", job));
    }
}
