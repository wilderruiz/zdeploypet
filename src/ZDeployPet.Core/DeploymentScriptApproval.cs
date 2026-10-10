namespace ZDeployPet.Core;

public sealed record DeploymentScriptApproval(
    int SchemaVersion,
    string ProfileId,
    string ScriptRelativePath,
    string Sha256,
    DateTimeOffset ApprovedAtUtc)
{
    public const int CurrentSchemaVersion = 1;
}

public sealed record DeploymentScriptApprovalMatchResult(
    bool IsMatch,
    IReadOnlyList<string> Errors);

public static class DeploymentScriptApprovalValidator
{
    public static DeploymentScriptApprovalMatchResult Validate(
        DeploymentScriptApproval? approval,
        string profileId,
        string scriptRelativePath,
        string currentSha256)
    {
        List<string> errors = [];

        if (approval is null)
            return new(false, ["No deployment-script approval exists for this profile."]);

        if (approval.SchemaVersion != DeploymentScriptApproval.CurrentSchemaVersion)
            errors.Add($"Unsupported deployment-script approval schema {approval.SchemaVersion}.");

        if (!string.Equals(approval.ProfileId, profileId, StringComparison.Ordinal))
            errors.Add("The deployment-script approval belongs to a different profile.");

        if (!string.Equals(
                NormalizeRelativePath(approval.ScriptRelativePath),
                NormalizeRelativePath(scriptRelativePath),
                StringComparison.Ordinal))
        {
            errors.Add("The approved deployment-script path does not match the configured profile script.");
        }

        if (!IsSha256(approval.Sha256))
            errors.Add("The approved deployment-script SHA-256 is missing or invalid.");

        if (!IsSha256(currentSha256))
            errors.Add("The current deployment-script SHA-256 is missing or invalid.");

        if (IsSha256(approval.Sha256) &&
            IsSha256(currentSha256) &&
            !string.Equals(approval.Sha256, currentSha256, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("The deployment script fingerprint has changed since approval.");
        }

        return new(errors.Count == 0, errors);
    }

    public static bool IsSha256(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64)
            return false;

        foreach (char character in value)
        {
            bool hex = character is >= '0' and <= '9'
                or >= 'a' and <= 'f'
                or >= 'A' and <= 'F';
            if (!hex) return false;
        }

        return true;
    }

    private static string NormalizeRelativePath(string? path)
        => (path ?? string.Empty).Replace('\\', '/').Trim();
}

public sealed record DryRunPromptPlan(
    int TargetChoice,
    int? ReleaseChoice)
{
    public IReadOnlyList<string> BuildStandardInputLines()
    {
        if (TargetChoice is < 1 or > 3)
            throw new InvalidDataException("Dry-run target prompt choice must be 1, 2 or 3.");

        if (ReleaseChoice is not null && ReleaseChoice is < 1 or > 3)
            throw new InvalidDataException("Dry-run release prompt choice must be 1, 2 or 3 when supplied.");

        List<string> lines = [TargetChoice.ToString(System.Globalization.CultureInfo.InvariantCulture)];
        if (ReleaseChoice is not null)
            lines.Add(ReleaseChoice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

        // PDA-5 is always dry-run. Live mode is intentionally impossible here.
        lines.Add("1");
        return lines;
    }
}
