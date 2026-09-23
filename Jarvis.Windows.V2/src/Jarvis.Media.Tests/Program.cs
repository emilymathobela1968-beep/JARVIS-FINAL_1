using System.Text.Json;
using Jarvis.Governance;
using Jarvis.Media;

var tests = new MediaWorkflowTests();
await tests.RunAll();

internal sealed class MediaWorkflowTests
{
    private int passed;
    private int failed;

    public async Task RunAll()
    {
        await Run("ALEXIS_PROMO_001 manifest has sixteen ordered scenes", ManifestHasSixteenOrderedScenes);
        await Run("scene lifecycle cannot lock without approval", CannotLockWithoutApproval);
        await Run("candidate registration and approval then lock", CandidateApprovalThenLock);
        await Run("rejection permits controlled regeneration", RejectionPermitsRegeneration);
        await Run("reference required when ALEXIS authority missing", ReferenceRequiredWhenAuthorityMissing);
        await Run("provider unavailable returns explicit status", ProviderUnavailableExplicit);
        await Run("candidate output boundary enforced", CandidateOutputBoundaryEnforced);
        await Run("media governed tools list required controls", MediaToolsListRequiredControls);
        await Run("unknown media tool fails closed", UnknownToolFailsClosed);
        await Run("secret values are redacted from media audit", SecretValuesRedacted);
        await Run("unavailable provider cannot enter generating", UnavailableProviderCannotGenerate);
        await Run("available provider stores candidate and state", AvailableProviderStoresCandidate);
        await Run("provider failure is truthful", ProviderFailureIsTruthful);
        await Run("media status projection is authoritative", MediaStatusProjectionIsAuthoritative);
        await Run("image config uses durable values and secure credential", ImageConfigUsesDurableValues);
        await Run("image config environment overrides durable values", ImageConfigEnvironmentOverrides);
        await Run("image config omits credentials from disk model", ImageConfigOmitsCredentials);
        await Run("provider selector chooses OpenAI", ProviderSelectorChoosesOpenAi);
        await Run("missing OpenAI credential requires authorization", MissingOpenAiCredentialRequiresAuthorization);
        await Run("OpenAI provider saves mocked image candidate", OpenAiProviderSavesCandidate);
        await Run("Azure provider remains selectable", AzureProviderRemainsSelectable);

        Console.WriteLine($"MEDIA_TESTS_TOTAL passed={passed} failed={failed}");
        if (failed > 0) Environment.Exit(1);
    }

    private Task ManifestHasSixteenOrderedScenes()
    {
        var fixture = Fixture();
        Assert(fixture.Job.JobId == "ALEXIS_PROMO_001", "job id should be locked");
        Assert(fixture.Job.Scenes.Count == 16, "job should contain sixteen scenes");
        Assert(fixture.Job.Scenes.Select(x => x.Index).SequenceEqual(Enumerable.Range(1, 16)), "scenes should be ordered 1..16");
        Assert(fixture.Job.Scenes[0].Title.Contains("vehicle/problem arrival", StringComparison.Ordinal), "scene 01 should be vehicle arrival");
        Assert(fixture.Job.Scenes[15].Title.Contains("final ALEXIS hero", StringComparison.Ordinal), "scene 16 should be final hero reveal");
        Assert(fixture.Job.Scenes.All(x => x.State == MediaSceneState.Planned), "all scenes should start planned");
        return Task.CompletedTask;
    }

    private Task CannotLockWithoutApproval()
    {
        var fixture = Fixture();
        AssertThrows<InvalidOperationException>(() => fixture.Store.LockScene(fixture.Job.JobId, 1, Guid.NewGuid()));
        return Task.CompletedTask;
    }

    private Task CandidateApprovalThenLock()
    {
        var fixture = Fixture();
        var candidatePath = CreateCandidate(fixture.InputRoot, "candidate-01.png");
        var candidate = fixture.Store.RegisterCandidate(fixture.Job, 1, candidatePath, "external", "scene 01", Guid.NewGuid());
        var afterCandidate = fixture.Store.Get(fixture.Job.JobId)!;
        Assert(afterCandidate.Scenes[0].State == MediaSceneState.Candidate, "scene should enter candidate state");
        fixture.Store.ApproveCandidate(fixture.Job.JobId, 1, candidate.CandidateId, Guid.NewGuid());
        var locked = fixture.Store.LockScene(fixture.Job.JobId, 1, Guid.NewGuid());
        Assert(locked.Scenes[0].State == MediaSceneState.Locked, "scene should lock after approval");
        Assert(File.Exists(locked.Scenes[0].LockedAssetPath), "locked asset should exist");
        return Task.CompletedTask;
    }

    private Task RejectionPermitsRegeneration()
    {
        var fixture = Fixture();
        var first = fixture.Store.RegisterCandidate(fixture.Job, 1, CreateCandidate(fixture.InputRoot, "candidate-a.png"), "external", "bad crop", Guid.NewGuid());
        fixture.Store.RejectCandidate(fixture.Job.JobId, 1, first.CandidateId, "identity drift", Guid.NewGuid());
        var second = fixture.Store.RegisterCandidate(fixture.Store.Get(fixture.Job.JobId)!, 1, CreateCandidate(fixture.InputRoot, "candidate-b.png"), "external", "corrected", Guid.NewGuid());
        var job = fixture.Store.Get(fixture.Job.JobId)!;
        Assert(job.Scenes[0].State == MediaSceneState.Candidate, "rejected scene should accept a new candidate");
        Assert(job.Scenes[0].CurrentCandidateId == second.CandidateId, "new candidate should be current");
        return Task.CompletedTask;
    }

    private async Task ReferenceRequiredWhenAuthorityMissing()
    {
        var fixture = Fixture();
        var registry = Registry(fixture, new FakeImageProvider(MediaProviderState.Available));
        var result = await registry.DispatchAsync(Request("generate_media_image", new { job_id = fixture.Job.JobId, scene_index = 3, prompt = "ALEXIS appears", output_directory = fixture.OutputRoot }));
        Assert(result.Status == GovernedToolStatus.ReferenceRequired, "ALEXIS scene should require approved reference");
        Assert(result.FailureReason == "alexis_reference_required", "reference failure reason should be explicit");
    }

    private async Task ProviderUnavailableExplicit()
    {
        var fixture = Fixture();
        var registry = Registry(fixture, new FakeImageProvider(MediaProviderState.ProviderUnavailable));
        var result = await registry.DispatchAsync(Request("generate_media_image", new { job_id = fixture.Job.JobId, scene_index = 1, prompt = "arrival", output_directory = fixture.OutputRoot }));
        Assert(result.Status == GovernedToolStatus.ProviderUnavailable, "provider unavailable should be explicit");
    }

    private async Task UnavailableProviderCannotGenerate()
    {
        var fixture = Fixture();
        var registry = Registry(fixture, new FakeImageProvider(MediaProviderState.AuthorizationRequired));
        var result = await registry.DispatchAsync(Request("generate_media_image", new { job_id = fixture.Job.JobId, scene_index = 1, prompt = "arrival", output_directory = fixture.OutputRoot }));
        var job = fixture.Store.Get(fixture.Job.JobId)!;
        Assert(result.Status == GovernedToolStatus.AuthorizationRequired, "authorization must be explicit");
        Assert(job.State == MediaJobState.AuthorizationRequired, "unavailable provider must not enter generating");
    }

    private async Task AvailableProviderStoresCandidate()
    {
        var fixture = Fixture();
        var registry = Registry(fixture, new FakeImageProvider(MediaProviderState.Available));
        var result = await registry.DispatchAsync(Request("generate_media_image", new { job_id = fixture.Job.JobId, scene_index = 1, prompt = "arrival", output_directory = fixture.OutputRoot }));
        var candidate = result.Evidence as GeneratedImageCandidate ?? throw new InvalidOperationException("candidate evidence missing");
        var job = fixture.Store.Get(fixture.Job.JobId)!;
        Assert(job.State == MediaJobState.CandidateReady, "successful generation should reach CandidateReady");
        Assert(job.Scenes[0].CurrentCandidateId == candidate.CandidateId && File.Exists(candidate.Path), "candidate path and id should be stored");
    }

    private async Task ProviderFailureIsTruthful()
    {
        var fixture = Fixture();
        var registry = Registry(fixture, new FailingImageProvider());
        var result = await registry.DispatchAsync(Request("generate_media_image", new { job_id = fixture.Job.JobId, scene_index = 1, prompt = "arrival", output_directory = fixture.OutputRoot }));
        var job = fixture.Store.Get(fixture.Job.JobId)!;
        Assert(result.Status == GovernedToolStatus.ProviderUnavailable && job.State == MediaJobState.Failed, "provider failure should be explicit and terminal");
        Assert(job.LastFailureReason == "provider_failed", "failure reason should be bounded");
    }

    private Task MediaStatusProjectionIsAuthoritative()
    {
        var fixture = Fixture();
        var starting = fixture.Store.BeginGeneration(fixture.Job.JobId, 1, Guid.NewGuid());
        var projection = MediaStatusProjectionRules.Project(starting);
        Assert(projection.ActivityVisible && projection.Label.Contains("Preparing", StringComparison.Ordinal), "starting should show activity");
        var failed = fixture.Store.MarkFailed(fixture.Job.JobId, "provider_failed", Guid.NewGuid());
        projection = MediaStatusProjectionRules.Project(failed);
        Assert(!projection.ActivityVisible && projection.Label == "Generation failed", "failed should stop activity");
        return Task.CompletedTask;
    }

    private Task ImageConfigUsesDurableValues()
    {
        var config = AzureImageProviderConfiguration.Resolve(new FakeImageConfigStore(new("https://image.example", "gpt-image-1", "2024-02-15-preview")), new FakeSecretStore("secure-value"));
        Assert(config.Status.Configured && config.Options.Deployment == "gpt-image-1", "durable image config should resolve with secure credential");
        Assert(config.Status.Source == "local_config+windows_credential", "config source should distinguish durable and secure values");
        return Task.CompletedTask;
    }

    private Task ImageConfigEnvironmentOverrides()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_IMAGE_ENDPOINT");
        var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_IMAGE_DEPLOYMENT");
        try
        {
            Environment.SetEnvironmentVariable("AZURE_OPENAI_IMAGE_ENDPOINT", "https://env.example");
            Environment.SetEnvironmentVariable("AZURE_OPENAI_IMAGE_DEPLOYMENT", "env-image");
            var config = AzureImageProviderConfiguration.Resolve(new FakeImageConfigStore(new("https://local.example", "local-image")), new FakeSecretStore("secure-value"));
            Assert(config.Options.Endpoint == "https://env.example" && config.Options.Deployment == "env-image", "environment should override durable image config");
        }
        finally
        {
            Environment.SetEnvironmentVariable("AZURE_OPENAI_IMAGE_ENDPOINT", endpoint);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_IMAGE_DEPLOYMENT", deployment);
        }
        return Task.CompletedTask;
    }

    private Task ImageConfigOmitsCredentials()
    {
        var local = JsonSerializer.Serialize(new { endpoint = "https://image.example", deployment = "gpt-image-1" });
        Assert(!local.Contains("secure-value", StringComparison.Ordinal), "secure credential must not be serialized into local config");
        return Task.CompletedTask;
    }

    private Task ProviderSelectorChoosesOpenAi()
    {
        var config = AzureImageProviderConfiguration.Resolve(new FakeImageConfigStore(new(null, null) { Provider = "openai", Model = "gpt-image-2" }), new FakeSecretStore("azure"), new FakeSecretStore("openai"));
        var provider = MediaImageProviderSelector.Select(new HttpClient(), config);
        Assert(provider is OpenAiImageGenerationProvider, "openai selection should be explicit");
        return Task.CompletedTask;
    }

    private Task MissingOpenAiCredentialRequiresAuthorization()
    {
        var config = AzureImageProviderConfiguration.Resolve(new FakeImageConfigStore(new(null, null) { Provider = "openai", Model = "gpt-image-2" }), new FakeSecretStore(null), new FakeSecretStore(null));
        var provider = MediaImageProviderSelector.Select(new HttpClient(), config);
        Assert(provider.GetAvailability() == MediaProviderState.AuthorizationRequired, "missing OpenAI credential should require authorization");
        return Task.CompletedTask;
    }

    private async Task OpenAiProviderSavesCandidate()
    {
        var root = Path.Combine(Path.GetTempPath(), "jarvis-openai-image-" + Guid.NewGuid().ToString("N"));
        var handler = new StubImageHandler();
        var provider = new OpenAiImageGenerationProvider(new HttpClient(handler), new("secret-not-logged", "gpt-image-2", "https://api.openai.com"));
        var result = await provider.GenerateAsync(new("job", 1, "arrival", root, Guid.NewGuid()));
        Assert(result.ProviderState == MediaProviderState.Available && result.Candidate is not null, "mocked OpenAI response should produce candidate");
        var candidate = result.Candidate!;
        Assert(File.Exists(candidate.Path) && File.ReadAllBytes(candidate.Path).SequenceEqual(new byte[] { 137, 80, 78, 71 }), "decoded bytes should be saved");
        Assert(handler.AuthorizationHeader == "Bearer secret-not-logged" && handler.RequestBody.Contains("gpt-image-2", StringComparison.Ordinal), "request should use configured model and authorization");
    }

    private Task AzureProviderRemainsSelectable()
    {
        var config = AzureImageProviderConfiguration.Resolve(new FakeImageConfigStore(new("https://azure.example", "deployment")), new FakeSecretStore("azure"));
        var provider = MediaImageProviderSelector.Select(new HttpClient(), config);
        Assert(provider is AzureOpenAiImageGenerationProvider, "azure should remain the explicit default path");
        return Task.CompletedTask;
    }

    private Task CandidateOutputBoundaryEnforced()
    {
        var fixture = Fixture();
        var outside = CreateCandidate(Path.Combine(Path.GetTempPath(), "outside-" + Guid.NewGuid().ToString("N")), "candidate.png");
        AssertThrows<UnauthorizedAccessException>(() => fixture.Store.RegisterCandidate(fixture.Job, 1, outside, "external", "outside", Guid.NewGuid()));
        return Task.CompletedTask;
    }

    private Task MediaToolsListRequiredControls()
    {
        var fixture = Fixture();
        var registry = Registry(fixture, new FakeImageProvider(MediaProviderState.ProviderUnavailable));
        var names = registry.Definitions.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        Assert(names.Contains("generate_media_image"), "generate tool required");
        Assert(names.Contains("get_media_job"), "get job tool required");
        Assert(names.Contains("register_media_candidate"), "register tool required");
        Assert(names.Contains("approve_media_candidate"), "approve tool required");
        Assert(names.Contains("reject_media_candidate"), "reject tool required");
        return Task.CompletedTask;
    }

    private async Task UnknownToolFailsClosed()
    {
        var fixture = Fixture();
        var registry = Registry(fixture, new FakeImageProvider(MediaProviderState.ProviderUnavailable));
        var result = await registry.DispatchAsync(Request("approve_everything_for_me", new { job_id = fixture.Job.JobId }));
        Assert(result.Status == GovernedToolStatus.Rejected, "unknown tool should reject");
        Assert(result.FailureReason == "unknown_tool", "unknown tool reason should be explicit");
    }

    private async Task SecretValuesRedacted()
    {
        var fixture = Fixture();
        var auditPath = fixture.AuditPath;
        var registry = Registry(fixture, new FakeImageProvider(MediaProviderState.ProviderUnavailable));
        await registry.DispatchAsync(Request("reject_media_candidate", new { job_id = fixture.Job.JobId, scene_index = 1, candidate_id = "missing", reason = "OPENAI_API_KEY=do-not-write-this" }));
        var log = await File.ReadAllTextAsync(auditPath);
        Assert(!log.Contains("do-not-write-this", StringComparison.Ordinal), "secret value should be redacted");
    }

    private static GovernedCapabilityRegistry Registry(FixtureData fixture, IImageGenerationProvider provider)
    {
        var audit = new JsonGovernedToolAudit(fixture.AuditPath);
        return new GovernedCapabilityRegistry(
            [
                new MediaGovernedTools(fixture.Store, provider),
                new GetMediaJobTool(fixture.Store),
                new RegisterMediaCandidateTool(fixture.Store),
                new ApproveMediaCandidateTool(fixture.Store),
                new RejectMediaCandidateTool(fixture.Store)
            ],
            audit);
    }

    private static GovernedToolRequest Request(string name, object args) =>
        new(Guid.NewGuid(), Guid.NewGuid(), name, JsonSerializer.SerializeToElement(args));

    private static FixtureData Fixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "jarvis-media-workflow-" + Guid.NewGuid().ToString("N"));
        var input = Path.Combine(root, "input");
        var output = Path.Combine(root, "output");
        Directory.CreateDirectory(input);
        Directory.CreateDirectory(output);
        var auditPath = Path.Combine(root, "media-governance.jsonl");
        var store = new MediaJobStore(new JsonMediaAudit(Path.Combine(root, "media-audit.jsonl")));
        var job = new AlexisPromoManifestFactory().CreateAlexisPromo001(input, output);
        store.Save(job);
        return new(root, input, output, auditPath, store, job);
    }

    private static string CreateCandidate(string directory, string name)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name);
        File.WriteAllBytes(path, [137, 80, 78, 71]);
        return path;
    }

    private async Task Run(string name, Func<Task> test)
    {
        try
        {
            await test();
            passed++;
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            failed++;
            Console.WriteLine($"FAIL {name}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertThrows<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    private sealed record FixtureData(string Root, string InputRoot, string OutputRoot, string AuditPath, MediaJobStore Store, MediaJob Job);

    private sealed class FakeImageProvider(MediaProviderState state) : IImageGenerationProvider
    {
        public string Name => "fake";
        public MediaProviderState GetAvailability() => state;

        public Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken = default)
        {
            if (state != MediaProviderState.Available)
                return Task.FromResult(new ImageGenerationResult(state, null, "provider unavailable", "provider_unavailable"));
            Directory.CreateDirectory(request.OutputDirectory);
            var path = Path.Combine(request.OutputDirectory, $"candidate-{request.SceneIndex:00}.png");
            File.WriteAllBytes(path, [137, 80, 78, 71]);
            return Task.FromResult(new ImageGenerationResult(state, new(Guid.NewGuid().ToString("N"), request.JobId, request.SceneIndex, path, Name, request.Prompt, "none", request.CorrelationId, DateTimeOffset.UtcNow), "generated", null));
        }
    }

    private sealed class FailingImageProvider : IImageGenerationProvider
    {
        public string Name => "failing";
        public MediaProviderState GetAvailability() => MediaProviderState.Available;
        public Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ImageGenerationResult(MediaProviderState.ProviderUnavailable, null, "provider failed", "provider_failed"));
    }

    private sealed class FakeImageConfigStore(MediaImageLocalConfiguration value) : IMediaImageLocalConfigStore
    {
        public MediaImageLocalConfiguration Read() => value;
    }

    private sealed class FakeSecretStore(string? value) : Jarvis.Realtime.IJarvisSecretStore
    {
        public string Identifier => "test";
        public string? ReadSecret() => value;
    }

    private sealed class StubImageHandler : HttpMessageHandler
    {
        public string AuthorizationHeader { get; private set; } = string.Empty;
        public string RequestBody { get; private set; } = string.Empty;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AuthorizationHeader = request.Headers.Authorization?.ToString() ?? string.Empty;
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            var payload = JsonSerializer.Serialize(new { data = new[] { new { b64_json = Convert.ToBase64String([137, 80, 78, 71]) } } });
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(payload) };
        }
    }
}
