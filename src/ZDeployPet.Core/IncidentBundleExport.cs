namespace ZDeployPet.Core;

public sealed record IncidentBundleExportValidationResult(
    bool Success,
    string? CanonicalPath,
    IReadOnlyList<string> Errors);

public static class IncidentBundleExportPolicy
{
    public const string RequiredExtension = ".txt";

    public static IncidentBundleExportValidationResult ValidateDestination(
        string? destinationPath,
        string? activeWindowsProjectPath)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(destinationPath))
            return Failed("An export destination is required.");

        string candidate = destinationPath.Trim();
        if (!Path.IsPathFullyQualified(candidate))
            errors.Add("Incident bundles must be exported to a fully qualified local Windows path.");

        if (candidate.StartsWith("\\\\", StringComparison.Ordinal) ||
            candidate.StartsWith("//", StringComparison.Ordinal))
            errors.Add("Network/UNC export destinations are not allowed.");

        string? canonical = null;
        try
        {
            canonical = Path.GetFullPath(candidate);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            errors.Add("The selected export destination is not a valid Windows file path.");
        }

        if (canonical is not null)
        {
            if (!string.Equals(Path.GetExtension(canonical), RequiredExtension, StringComparison.OrdinalIgnoreCase))
                errors.Add($"Incident bundles must use the '{RequiredExtension}' extension.");

            string fileName = Path.GetFileName(canonical);
            if (string.IsNullOrWhiteSpace(fileName) || fileName is "." or "..")
                errors.Add("The export destination must name a file.");

            if (HasAlternateDataStreamSyntax(canonical))
                errors.Add("Alternate data stream destinations are not allowed.");

            if (!string.IsNullOrWhiteSpace(activeWindowsProjectPath) &&
                IsInsideOrEqual(canonical, activeWindowsProjectPath))
                errors.Add("Incident bundles cannot be exported inside the active project repository.");
        }

        return new IncidentBundleExportValidationResult(errors.Count == 0, canonical, errors);
    }

    public static string BuildSuggestedFileName(string deploymentId)
    {
        string safeId = string.Concat((deploymentId ?? string.Empty)
            .Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
        safeId = safeId.Trim().Trim('.');
        if (string.IsNullOrWhiteSpace(safeId))
            safeId = "deployment";

        return $"zdeploypet-incident-{safeId}{RequiredExtension}";
    }

    private static bool HasAlternateDataStreamSyntax(string fullPath)
    {
        int firstColon = fullPath.IndexOf(':');
        if (firstColon < 0)
            return false;

        // A local drive-qualified Windows path may contain exactly one colon at index 1 (C:\...).
        return firstColon != 1 || fullPath.IndexOf(':', firstColon + 1) >= 0;
    }

    private static bool IsInsideOrEqual(string candidatePath, string rootPath)
    {
        string candidate;
        string root;
        try
        {
            candidate = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidatePath));
            root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
        }
        catch
        {
            return false;
        }

        if (string.Equals(candidate, root, StringComparison.OrdinalIgnoreCase))
            return true;

        string prefix = root + Path.DirectorySeparatorChar;
        return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static IncidentBundleExportValidationResult Failed(string error)
        => new(false, null, [error]);
}
