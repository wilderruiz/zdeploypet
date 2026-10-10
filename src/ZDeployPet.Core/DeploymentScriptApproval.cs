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
    public void Validate()
    {
        if (TargetChoice is < 1 or > 3)
            throw new InvalidDataException("Dry-run target prompt choice must be 1, 2 or 3.");

        if (ReleaseChoice is not null && ReleaseChoice is < 1 or > 3)
            throw new InvalidDataException("Dry-run release prompt choice must be 1, 2 or 3 when supplied.");
    }
}

public enum DryRunPromptStage
{
    AwaitTarget,
    AwaitRelease,
    AwaitMode,
    AwaitDryMarker,
    RunningDry,
    Complete,
    SafetyViolation
}

/// <summary>
/// Fail-closed adapter for the approved interactive Millenova script.
/// Answers are emitted only after the exact expected prompt is observed; they are
/// never pre-buffered on stdin where ssh/validators could consume them. PDA-5 never
/// emits the live confirmation phrase and treats any live-mode marker as a safety violation.
/// </summary>
public sealed class DryRunPromptProtocol
{
    private const int MaximumRollingCharacters = 1024;
    private readonly DryRunPromptPlan _plan;
    private string _rolling = string.Empty;

    public DryRunPromptProtocol(DryRunPromptPlan plan)
    {
        plan.Validate();
        _plan = plan;
        Stage = DryRunPromptStage.AwaitTarget;
    }

    public DryRunPromptStage Stage { get; private set; }
    public bool DryModeConfirmed { get; private set; }
    public bool Completed => Stage == DryRunPromptStage.Complete;
    public bool SafetyViolation => Stage == DryRunPromptStage.SafetyViolation;
    public string? ViolationReason { get; private set; }

    public string? Observe(string? text)
    {
        if (string.IsNullOrEmpty(text) || Completed || SafetyViolation)
            return null;

        _rolling += text;
        if (_rolling.Length > MaximumRollingCharacters)
            _rolling = _rolling[^MaximumRollingCharacters..];

        if (Contains("LIVE MILLENOVA DEPLOYMENT") || Contains("Type DEPLOY MILLENOVA to continue:"))
        {
            Fail("The approved script entered or requested live deployment while PDA-5 was executing a dry run.");
            return null;
        }

        if (Contains("DRY RUN — NOTHING WILL BE MODIFIED"))
        {
            if (Stage != DryRunPromptStage.AwaitDryMarker && Stage != DryRunPromptStage.RunningDry)
            {
                Fail("The dry-run marker appeared before the dry mode prompt was safely answered.");
                return null;
            }

            DryModeConfirmed = true;
            Stage = DryRunPromptStage.RunningDry;

            if (Contains("DRY RUN COMPLETE"))
            {
                Stage = DryRunPromptStage.Complete;
                _rolling = string.Empty;
            }

            return null;
        }

        if (Contains("DRY RUN COMPLETE"))
        {
            if (!DryModeConfirmed)
            {
                Fail("The script reported dry-run completion without first proving dry mode.");
                return null;
            }

            Stage = DryRunPromptStage.Complete;
            _rolling = string.Empty;
            return null;
        }

        switch (Stage)
        {
            case DryRunPromptStage.AwaitTarget when Contains("Choose deployment destination [1/2/3]:"):
                Stage = _plan.ReleaseChoice is null ? DryRunPromptStage.AwaitMode : DryRunPromptStage.AwaitRelease;
                _rolling = string.Empty;
                return _plan.TargetChoice.ToString(System.Globalization.CultureInfo.InvariantCulture);

            case DryRunPromptStage.AwaitRelease when Contains("Choose release type [1/2/3]:"):
                Stage = DryRunPromptStage.AwaitMode;
                _rolling = string.Empty;
                return _plan.ReleaseChoice!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

            case DryRunPromptStage.AwaitMode when Contains("Choose [1/2]:"):
                Stage = DryRunPromptStage.AwaitDryMarker;
                _rolling = string.Empty;
                return "1";
        }

        return null;
    }

    public void MarkProcessExited(int exitCode)
    {
        if (SafetyViolation || Completed)
            return;

        if (exitCode == 0)
            Fail("The script exited successfully without completing the verified PDA-5 dry-run protocol.");
    }

    private bool Contains(string value)
        => _rolling.Contains(value, StringComparison.Ordinal);

    private void Fail(string reason)
    {
        ViolationReason = reason;
        Stage = DryRunPromptStage.SafetyViolation;
    }
}
