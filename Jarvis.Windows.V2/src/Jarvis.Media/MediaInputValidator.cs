namespace Jarvis.Media;

public sealed class MediaInputValidator
{
    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp"
    };

    public void Validate(MediaStudioRenderRequest request)
    {
        request.Authority.Validate();
        if (request.MissionId == Guid.Empty) throw new InvalidOperationException("Mission ID is required.");
        if (request.Panels.Count != 16) throw new InvalidOperationException("Exactly 16 source panels are required.");
        if (request.Panels.Select(panel => panel.Index).Distinct().Count() != 16) throw new InvalidOperationException("Panel indexes must be unique.");
        if (!request.Panels.OrderBy(panel => panel.Index).Select(panel => panel.Index).SequenceEqual(Enumerable.Range(1, 16)))
            throw new InvalidOperationException("Panel indexes must be ordered 1 through 16.");
        if (request.TargetDuration <= TimeSpan.Zero) throw new InvalidOperationException("Target duration must be positive.");
        if (request.TargetDuration < TimeSpan.FromSeconds(16)) throw new InvalidOperationException("Target duration is too short for 16 scenes.");
        if (string.IsNullOrWhiteSpace(request.OutputPath)) throw new InvalidOperationException("Output path is required.");
        if (string.IsNullOrWhiteSpace(request.Metadata.Title)) throw new InvalidOperationException("Project title is required.");

        var inputRoot = CanonicalDirectory(request.ApprovedInputRoot, mustExist: true);
        var outputRoot = CanonicalDirectory(request.ApprovedOutputRoot, mustExist: true);
        var output = Path.GetFullPath(request.OutputPath);
        if (!IsInside(outputRoot, output)) throw new UnauthorizedAccessException("Output path is outside the approved output root.");
        if (!string.Equals(Path.GetExtension(output), ".mp4", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Output path must be an .mp4 file.");

        foreach (var panel in request.Panels)
        {
            var panelPath = Path.GetFullPath(panel.Path);
            if (!IsInside(inputRoot, panelPath)) throw new UnauthorizedAccessException($"Panel {panel.Index} is outside the approved input root.");
            if (!File.Exists(panelPath)) throw new FileNotFoundException($"Panel {panel.Index} is missing.", panelPath);
            if (!SupportedImageExtensions.Contains(Path.GetExtension(panelPath))) throw new InvalidOperationException($"Panel {panel.Index} has an unsupported image extension.");
            if (string.IsNullOrWhiteSpace(panel.Title)) throw new InvalidOperationException($"Panel {panel.Index} title is required.");
        }

        ValidateOptionalAudio(request.NarrationAudioPath, inputRoot, "Narration");
        ValidateOptionalAudio(request.MusicAudioPath, inputRoot, "Music");
    }

    private static void ValidateOptionalAudio(string? path, string inputRoot, string label)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var audio = Path.GetFullPath(path);
        if (!IsInside(inputRoot, audio)) throw new UnauthorizedAccessException($"{label} audio is outside the approved input root.");
        if (!File.Exists(audio)) throw new FileNotFoundException($"{label} audio is missing.", audio);
        var extension = Path.GetExtension(audio);
        if (!extension.Equals(".wav", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".m4a", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{label} audio extension is unsupported.");
    }

    private static string CanonicalDirectory(string path, bool mustExist)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("Approved root path is required.");
        var canonical = Path.GetFullPath(path);
        if (mustExist && !Directory.Exists(canonical)) throw new DirectoryNotFoundException(canonical);
        return EnsureTrailingSeparator(canonical);
    }

    private static bool IsInside(string root, string candidate)
    {
        var path = Path.GetFullPath(candidate);
        return path.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
}
