namespace ZDeployPet.Core;

public sealed class DeploymentProfileValidator
{
    public ProfileValidationResult Validate(DeploymentProfile profile)
    {
        List<string> errors = [];

        if (profile.SchemaVersion != DeploymentProfile.CurrentSchemaVersion)
            errors.Add($"Unsupported profile schema {profile.SchemaVersion}.");
        if (string.IsNullOrWhiteSpace(profile.Name)) errors.Add("Profile name is required.");
        if (string.IsNullOrWhiteSpace(profile.WslDistribution)) errors.Add("A WSL distribution is required.");
        if (string.IsNullOrWhiteSpace(profile.WslProjectPath) || !profile.WslProjectPath.StartsWith('/'))
            errors.Add("The verified WSL project path is required.");

        ValidateProjectAndScript(profile, errors);
        ValidateReportRoot(profile.ReportRoot, errors);
        ValidateTargets(profile.Targets, errors);

        if (string.IsNullOrWhiteSpace(profile.LiveConfirmationPhrase))
            errors.Add("A live confirmation phrase is required.");

        return new ProfileValidationResult(errors);
    }

    private static void ValidateProjectAndScript(DeploymentProfile profile, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(profile.WindowsProjectPath) || !Directory.Exists(profile.WindowsProjectPath))
        {
            errors.Add("The Windows project folder does not exist.");
            return;
        }

        if (string.IsNullOrWhiteSpace(profile.ScriptRelativePath) || Path.IsPathRooted(profile.ScriptRelativePath))
        {
            errors.Add("Choose a deployment script using a path relative to the project folder.");
            return;
        }

        string root = Path.GetFullPath(profile.WindowsProjectPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string script = Path.GetFullPath(Path.Combine(root, profile.ScriptRelativePath));
        string prefix = root + Path.DirectorySeparatorChar;
        if (!script.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("The deployment script must remain inside the project folder.");
            return;
        }

        if (!File.Exists(script)) errors.Add("The selected deployment script does not exist.");
    }

    private static void ValidateReportRoot(string? reportRoot, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(reportRoot)) return;
        if (!reportRoot.StartsWith('/') || reportRoot == "/")
            errors.Add("The report root must be a safe absolute WSL path.");
    }

    private static void ValidateTargets(IReadOnlyList<DeploymentTarget> targets, List<string> errors)
    {
        if (targets.Count == 0)
        {
            errors.Add("Add at least one deployment target.");
            return;
        }

        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
        foreach (DeploymentTarget target in targets)
        {
            if (string.IsNullOrWhiteSpace(target.Id) || !ids.Add(target.Id))
                errors.Add("Every target requires a unique identifier.");
            if (string.IsNullOrWhiteSpace(target.Label)) errors.Add("Every target requires a label.");
            if (string.IsNullOrWhiteSpace(target.Host)) errors.Add($"Target '{target.Label}' requires a host.");
            if (target.Port is < 1 or > 65535) errors.Add($"Target '{target.Label}' has an invalid SSH port.");
            if (string.IsNullOrWhiteSpace(target.User)) errors.Add($"Target '{target.Label}' requires an SSH user.");
            if (!IsSafeRemoteDestination(target.RemoteDestination))
                errors.Add($"Target '{target.Label}' requires a safe absolute destination below the remote root/home.");
        }
    }

    public static bool IsSafeRemoteDestination(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/') || path.Contains('\0')) return false;
        string normalized = path.TrimEnd('/');
        if (normalized.Length == 0) return false;
        string[] segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(segment => segment is "." or "..")) return false;
        if (segments.Length == 1) return false;
        return !(segments.Length == 2 && string.Equals(segments[0], "home", StringComparison.Ordinal));
    }
}
