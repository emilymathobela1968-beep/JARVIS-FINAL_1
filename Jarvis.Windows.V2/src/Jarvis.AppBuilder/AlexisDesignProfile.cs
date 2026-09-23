namespace Jarvis.AppBuilder;

public static class AlexisDesignProfile
{
    public const string ProjectId = "alexis-design-project";

    public static AppBuilderProject CreateProject() => new()
    {
        Id = ProjectId,
        Name = "ALEXIS Design Project",
        Product = new ProductAuthority(
            "Automotive diagnostic workstation for structured technician workflows.",
            "Leon and advanced diagnostic technicians.",
            "Automotive diagnostics",
            "Windows desktop and web diagnostic UI"),
        CurrentPhase = "Phase 1 - visual mock authority",
        Requirements =
        [
            Req("REQ-ALEXIS-001", RequirementClassification.Must, "visual", "Use page background #050915.", "Known ALEXIS visual baseline."),
            Req("REQ-ALEXIS-002", RequirementClassification.Must, "visual", "Use elevated surface #0B1220.", "Known ALEXIS visual baseline."),
            Req("REQ-ALEXIS-003", RequirementClassification.Must, "visual", "Use accent blue #2F7CFF and accent green #18CC6F as restrained accents.", "Known ALEXIS visual baseline."),
            Req("REQ-ALEXIS-004", RequirementClassification.Must, "visual", "Use primary text #F5F8FF and secondary text #7D8CA5.", "Known ALEXIS visual baseline."),
            Req("REQ-ALEXIS-005", RequirementClassification.Prohibited, "visual", "Do not present as generic SaaS, generic card-grid dashboard, gaming UI, excessive neon, or random gradients.", "ALEXIS must remain a premium purpose-built diagnostic workstation."),
            Req("REQ-ALEXIS-006", RequirementClassification.Must, "domain", "Use clearly labelled mock or simulated UI content unless authoritative diagnostic data is provided.", "Phase 1 must not invent domain truth."),
            Req("REQ-ALEXIS-SYSTEMSCAN-001", RequirementClassification.Must, "screen", "System Scan mock must include navigation, diagnostic strategy, scan controls, module evidence and network topology.", "System Scan is the first visual authority target.")
        ],
        DesignAuthority = CreateAuthority(),
        AcceptedDecisions = [],
        RejectedDecisions = [],
        UnresolvedQuestions = [],
        References = ["ALEXIS System Scan prior visual authority; simulated content only."],
        GeneratedArtifacts = [],
        RevisionHistory = [],
        AcceptanceStatus = ArtifactAcceptanceState.Candidate
    };

    public static DesignAuthority CreateAuthority() => new(
        "Extremely premium dark corporate diagnostic workstation with deep black/navy surfaces, high information authority, restrained blue/green accents and purpose-built automotive topology.",
        new Dictionary<string, string>
        {
            ["pageBackground"] = "#050915",
            ["surfaceElevated"] = "#0B1220",
            ["accentBlue"] = "#2F7CFF",
            ["accentGreen"] = "#18CC6F",
            ["primaryText"] = "#F5F8FF",
            ["secondaryText"] = "#7D8CA5"
        },
        [Decision("DA-SURFACE-001", "surface", "Deep background with elevated navy work surfaces.", "Preserves ALEXIS authority.", true)],
        [Decision("DA-TYPE-001", "typography", "Segoe UI/Inter-like technical hierarchy with compact uppercase labels.", "Readable workstation density.", true)],
        [Decision("DA-SPACING-001", "spacing", "Tight but breathable 16-24px rhythm.", "Supports repeated technician use.", true)],
        [Decision("DA-GEOMETRY-001", "geometry", "Structured panels, thin separators, no decorative blobs.", "Avoids generic dashboard styling.", true)],
        [Decision("DA-DENSITY-001", "density", "High information density without permanent overpacked lists.", "Scales beyond small module counts.", true)],
        [Decision("DA-NAV-001", "navigation", "Left workflow navigation remains visible.", "Supports diagnostic sequence orientation.", true)],
        [Decision("DA-INFO-001", "information", "Topology is primary; controls and module evidence support it.", "System Scan should communicate network state first.", true)],
        [Decision("DA-INTERACTION-001", "interaction", "Explicit accept/reject/revise state, no auto-accept.", "Human acceptance boundary.", true)],
        [Decision("DA-STATE-001", "state", "Candidate artifacts remain candidate until Leon accepts.", "Prevents false acceptance.", true)],
        [Decision("DA-ANIM-001", "animation", "Subtle scanning motion only; deterministic mock may be static.", "Phase 1 visual evidence should be stable.", true)],
        [Decision("DA-ICON-001", "iconography", "Technical line icons and compact status chips.", "Diagnostic workstation tone.", true)],
        ["System Scan reference"],
        ["Deep ALEXIS System Scan topology treatment"],
        ["Generic SaaS card-grid dashboard", "Gaming neon dashboard"],
        new Dictionary<string, IReadOnlyList<DesignDecision>>
        {
            ["System Scan"] = [Decision("DA-PAGE-SCAN-001", "system-scan", "Network topology receives the largest central surface.", "System Scan is topology-first.", true)]
        });

    private static Requirement Req(string id, RequirementClassification c, string area, string text, string reason) =>
        new(id, c, area, text, reason, DateTimeOffset.UtcNow);

    private static DesignDecision Decision(string id, string area, string value, string reason, bool accepted) =>
        new(id, area, value, reason, accepted);
}
