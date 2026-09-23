namespace Jarvis.MissionIntelligence;

public enum AuthoritySeverity { Required, Recommended }

public sealed record AuthorityRule(
    string Id,
    string Title,
    string Description,
    AuthoritySeverity Severity);

public sealed record ProjectAuthority(
    string ProductName,
    string Version,
    IReadOnlyList<AuthorityRule> Rules,
    IReadOnlyList<string> ApprovedExamples,
    IReadOnlyList<string> RejectedExamples)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ProductName)) throw new InvalidOperationException("Authority product name is required.");
        if (string.IsNullOrWhiteSpace(Version)) throw new InvalidOperationException("Authority version is required.");
        if (Rules.Count == 0) throw new InvalidOperationException("Authority requires at least one rule.");
        if (Rules.Select(rule => rule.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Rules.Count)
            throw new InvalidOperationException("Authority rule IDs must be unique.");
    }
}

public static class AlexisDesignAuthority
{
    public static ProjectAuthority CreateBaseline() => new(
        ProductName: "ALEXIS",
        Version: "platinum-authority-v1",
        Rules:
        [
            new("canvas.16x9", "16:9 operating canvas", "Primary dashboard screens must fit a controlled 16:9 composition without accidental page scroll.", AuthoritySeverity.Required),
            new("visual.system-scan", "System Scan authority", "System Scan is the reference for hierarchy, density, contrast, restraint, and premium diagnostic tone.", AuthoritySeverity.Required),
            new("functionality.locked", "Functionality preservation", "Visual Engineering must preserve existing workflow behavior unless Leon explicitly authorizes functional changes.", AuthoritySeverity.Required),
            new("typography.hierarchy", "Readable hierarchy", "Typography must support fast workshop scanning with clear primary, secondary, and supporting information levels.", AuthoritySeverity.Required),
            new("asset.fidelity", "Asset fidelity", "Vehicle and product assets must remain recognizable, correctly cropped, and consistent with ALEXIS identity.", AuthoritySeverity.Required),
            new("premium.density", "Premium density", "Screens should feel dense, composed, and operational rather than generic, sparse, or decorative.", AuthoritySeverity.Recommended)
        ],
        ApprovedExamples: ["System Scan"],
        RejectedExamples: ["generic card grid", "marketing hero layout", "scroll-heavy diagnostic workflow"]);
}
