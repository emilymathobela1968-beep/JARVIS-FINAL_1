using System.Text.Json;

namespace Jarvis.AppBuilder;

public sealed class AppBuilderStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string root;

    public AppBuilderStore(string root)
    {
        this.root = root;
        Directory.CreateDirectory(ProjectsRoot);
        Directory.CreateDirectory(ProfilesRoot);
        Directory.CreateDirectory(ArtifactsRoot);
        Directory.CreateDirectory(SpecsRoot);
        Directory.CreateDirectory(ContextRoot);
    }

    public string ProjectsRoot => Path.Combine(root, "projects");
    public string ProfilesRoot => Path.Combine(root, "profiles");
    public string ArtifactsRoot => Path.Combine(root, "artifacts");
    public string SpecsRoot => Path.Combine(root, "specs");
    public string ContextRoot => Path.Combine(root, "context");
    public string Root => root;
    public string ProjectPath(string projectId) => Path.Combine(ProjectsRoot, $"{projectId}.json");
    public string ContextPath(string projectId) => Path.Combine(ContextRoot, $"{projectId}.builder-context.json");
    public string LatestContextPath => Path.Combine(ContextRoot, "latest-active.builder-context.json");

    public async Task SaveProjectAsync(AppBuilderProject project, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(ProjectPath(project.Id));
        await JsonSerializer.SerializeAsync(stream, project, Options, cancellationToken);
    }

    public async Task<AppBuilderProject> LoadProjectAsync(string projectId, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(ProjectPath(projectId));
        return await JsonSerializer.DeserializeAsync<AppBuilderProject>(stream, Options, cancellationToken)
            ?? throw new InvalidDataException($"Project {projectId} could not be loaded.");
    }

    public async Task SaveContextAsync(BuilderProjectContext context, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(context.ActiveProjectId))
        {
            await using var stream = File.Create(ContextPath(context.ActiveProjectId));
            await JsonSerializer.SerializeAsync(stream, context, Options, cancellationToken);
        }

        await using var latest = File.Create(LatestContextPath);
        await JsonSerializer.SerializeAsync(latest, context, Options, cancellationToken);
    }

    public async Task<BuilderProjectContext?> LoadLatestContextAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(LatestContextPath)) return null;
        await using var stream = File.OpenRead(LatestContextPath);
        return await JsonSerializer.DeserializeAsync<BuilderProjectContext>(stream, Options, cancellationToken);
    }
}
