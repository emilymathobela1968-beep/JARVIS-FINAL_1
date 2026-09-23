namespace Jarvis.Computer;

public sealed class FileSystemOperator(IEnumerable<string> allowedRoots, ComputerPolicy policy, IComputerAudit audit)
{
    private readonly string[] roots = allowedRoots.Select(x => Path.GetFullPath(x).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar).ToArray();

    public Task<ComputerOperationResult> ExecuteAsync(ComputerOperation operation, CancellationToken cancellationToken = default)
    {
        audit.Write(operation.OperationId, "filesystem_request", new { operation.Action, arguments = ComputerText.RedactArguments(operation.Arguments) });
        if (policy.Validate(operation) is { } rejected)
        {
            audit.Write(operation.OperationId, "filesystem_rejected", new { rejected.FailureReason });
            return Task.FromResult(rejected);
        }

        if (!operation.Arguments.TryGetValue("path", out var path))
        {
            if (operation.Arguments.TryGetValue("known_folder", out var knownFolder))
                path = ResolveKnownFolder(knownFolder);
        }

        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult(ComputerPolicy.Failure(operation, policy.Classify(operation), ComputerOperationStatus.Rejected, "Missing path.", "missing_path"));

        var canonical = CanonicalizeInsideRoot(path);
        if (canonical is null)
            return Task.FromResult(ComputerPolicy.Failure(operation, policy.Classify(operation), ComputerOperationStatus.Rejected, "Path is outside the approved roots.", "path_outside_approved_roots"));

        cancellationToken.ThrowIfCancellationRequested();
        var before = Observe(canonical);
        ComputerOperationResult result = operation.Action.ToLowerInvariant() switch
        {
            "list" => List(operation, canonical, before),
            "find" => Find(operation, canonical, before),
            "read" => Read(operation, canonical, before),
            "write" => Write(operation, canonical, operation.Arguments.GetValueOrDefault("content", string.Empty), before),
            "create_directory" => CreateDirectory(operation, canonical, before),
            "copy" => Copy(operation, canonical, before),
            "move" or "rename" => Move(operation, canonical, before),
            "delete" => Delete(operation, canonical, before),
            "metadata" => Metadata(operation, canonical, before),
            _ => ComputerPolicy.Failure(operation, policy.Classify(operation), ComputerOperationStatus.Unsupported, "Unsupported file-system action.", "unsupported_file_action")
        };

        audit.Write(operation.OperationId, "filesystem_result", new { result.Status, result.FailureReason, result.Evidence });
        return Task.FromResult(result);
    }

    public string? CanonicalizeInsideRoot(string path)
    {
        path = ResolveFriendlyPath(path);
        var full = Path.GetFullPath(path);
        return roots.Any(root => full.Equals(root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) || full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            ? full
            : null;
    }

    private static string ResolveFriendlyPath(string path) =>
        path.Equals("Documents", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("My Documents", StringComparison.OrdinalIgnoreCase)
            ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            : path;

    private static string ResolveKnownFolder(string knownFolder) => knownFolder.ToLowerInvariant() switch
    {
        "documents" or "mydocuments" => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "desktop" => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        "downloads" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
        _ => knownFolder
    };

    private ComputerObservation Observe(string path) =>
        new(File.Exists(path) || Directory.Exists(path), File.Exists(path) ? "file" : Directory.Exists(path) ? "directory" : "missing", new Dictionary<string, string> { ["path"] = path });

    private ComputerOperationResult Read(ComputerOperation op, string path, ComputerObservation before)
    {
        if (!File.Exists(path)) return Fail(op, "File does not exist.", "file_missing", before);
        if (Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return new(op.OperationId, op.Kind, op.Action, ComputerOperationStatus.ProviderUnavailable, "PDF text extraction is not implemented in this governed build.", policy.Classify(op), before, Observe(path), "document_reader_unavailable", new Dictionary<string, string> { ["path"] = path, ["extension"] = ".pdf" });
        var text = File.ReadAllText(path);
        return Success(op, before, Observe(path), new Dictionary<string, string> { ["path"] = path, ["length"] = text.Length.ToString() });
    }

    private ComputerOperationResult List(ComputerOperation op, string path, ComputerObservation before)
    {
        if (!Directory.Exists(path))
            return Fail(op, "Directory does not exist.", "directory_missing", before);

        var entries = Directory.EnumerateFileSystemEntries(path)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();
        var evidence = new Dictionary<string, string>
        {
            ["path"] = path,
            ["entries"] = entries.Length.ToString()
        };
        for (var i = 0; i < entries.Length; i++)
            evidence[$"entry{i + 1}"] = entries[i];
        return Success(op, before, before, evidence);
    }

    private ComputerOperationResult Find(ComputerOperation op, string path, ComputerObservation before)
    {
        if (!Directory.Exists(path))
            return Fail(op, "Directory does not exist.", "directory_missing", before);

        var extension = op.Arguments.GetValueOrDefault("extension", string.Empty);
        var pattern = string.IsNullOrWhiteSpace(extension)
            ? op.Arguments.GetValueOrDefault("pattern", "*")
            : "*" + (extension.StartsWith('.') ? extension : "." + extension);
        var files = Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();
        var evidence = new Dictionary<string, string>
        {
            ["path"] = path,
            ["pattern"] = pattern,
            ["matches"] = files.Length.ToString()
        };
        for (var i = 0; i < files.Length; i++)
            evidence[$"match{i + 1}"] = files[i];
        return Success(op, before, before, evidence);
    }

    private ComputerOperationResult Write(ComputerOperation op, string path, string content, ComputerObservation before)
    {
        if (ComputerText.ContainsSecretMarker(content)) return Fail(op, "Secret-like content rejected.", "secret_material_blocked", before);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return Verified(op, before, Observe(path), File.Exists(path), "write_verification_failed");
    }

    private ComputerOperationResult CreateDirectory(ComputerOperation op, string path, ComputerObservation before)
    {
        Directory.CreateDirectory(path);
        return Verified(op, before, Observe(path), Directory.Exists(path), "directory_verification_failed");
    }

    private ComputerOperationResult Copy(ComputerOperation op, string source, ComputerObservation before)
    {
        if (!op.Arguments.TryGetValue("destination", out var destination)) return Fail(op, "Missing destination.", "missing_destination", before);
        var target = CanonicalizeInsideRoot(destination);
        if (target is null) return Fail(op, "Destination is outside approved roots.", "destination_outside_approved_roots", before);
        File.Copy(source, target, overwrite: false);
        return Verified(op, before, Observe(target), File.Exists(target), "copy_verification_failed", target);
    }

    private ComputerOperationResult Move(ComputerOperation op, string source, ComputerObservation before)
    {
        if (!op.Arguments.TryGetValue("destination", out var destination)) return Fail(op, "Missing destination.", "missing_destination", before);
        var target = CanonicalizeInsideRoot(destination);
        if (target is null) return Fail(op, "Destination is outside approved roots.", "destination_outside_approved_roots", before);
        File.Move(source, target);
        return Verified(op, before, Observe(target), File.Exists(target) && !File.Exists(source), "move_verification_failed", target);
    }

    private ComputerOperationResult Delete(ComputerOperation op, string path, ComputerObservation before)
    {
        if (File.Exists(path)) File.Delete(path);
        else if (Directory.Exists(path)) Directory.Delete(path, recursive: false);
        return Verified(op, before, Observe(path), !File.Exists(path) && !Directory.Exists(path), "delete_verification_failed");
    }

    private ComputerOperationResult Metadata(ComputerOperation op, string path, ComputerObservation before)
    {
        var evidence = new Dictionary<string, string> { ["path"] = path, ["exists"] = before.Available.ToString() };
        if (File.Exists(path)) evidence["length"] = new FileInfo(path).Length.ToString();
        return Success(op, before, before, evidence);
    }

    private ComputerOperationResult Verified(ComputerOperation op, ComputerObservation before, ComputerObservation after, bool verified, string failureReason, string? target = null) =>
        verified
            ? Success(op, before, after, new Dictionary<string, string> { ["path"] = target ?? op.Arguments.GetValueOrDefault("path", string.Empty), ["verified"] = "true" })
            : new(op.OperationId, op.Kind, op.Action, ComputerOperationStatus.VerificationFailed, "Action ran but verification failed.", policy.Classify(op), before, after, failureReason);

    private ComputerOperationResult Success(ComputerOperation op, ComputerObservation before, ComputerObservation after, IReadOnlyDictionary<string, string>? evidence = null) =>
        new(op.OperationId, op.Kind, op.Action, ComputerOperationStatus.Succeeded, "Succeeded.", policy.Classify(op), before, after, Evidence: evidence);

    private ComputerOperationResult Fail(ComputerOperation op, string message, string reason, ComputerObservation? before = null) =>
        new(op.OperationId, op.Kind, op.Action, ComputerOperationStatus.Failed, message, policy.Classify(op), before, FailureReason: reason, Evidence: ComputerText.RedactArguments(op.Arguments));
}
