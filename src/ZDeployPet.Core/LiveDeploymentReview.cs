namespace ZDeployPet.Core;

public sealed record LiveDeploymentRequest(
    string ProfileId,
    string TargetId,
    string Release);

public sealed record LiveDeploymentReviewContext(
    string ProfileName,
    string ConfiguredScriptRelativePath,
    string ApprovedScriptSha256,
    string CurrentScriptSha256,
    IReadOnlyList<string> AllowedTargetIds,
    string TargetLabel,
    string LiveConfirmationPhrase,
    bool DeploymentAccessReady,
    bool ExecutionAlreadyRunning);

public sealed record LiveDeploymentReviewSnapshot(
    string ReviewId,
    string ProfileId,
    string ProfileName,
    string ScriptRelativePath,
    string ScriptSha256,
    string TargetId,
    string TargetLabel,
    string Release,
    string LiveConfirmationPhrase,
    DateTimeOffset CreatedAtUtc);

public sealed record LiveDeploymentReviewResult(
    bool IsAllowed,
    LiveDeploymentReviewSnapshot? Snapshot,
    IReadOnlyList<string> Errors)
{
    public static LiveDeploymentReviewResult Allowed(LiveDeploymentReviewSnapshot snapshot) =>
        new(true, snapshot, Array.Empty<string>());

    public static LiveDeploymentReviewResult Blocked(IReadOnlyList<string> errors) =>
        new(false, null, errors);
}

public sealed record LiveDeploymentConfirmationContext(
    string ProfileId,
    string ScriptRelativePath,
    string CurrentScriptSha256,
    string TargetId,
    string Release,
    string LiveConfirmationPhrase,
    bool DeploymentAccessReady,
    bool ExecutionAlreadyRunning);

public sealed record LiveDeploymentConfirmationResult(
    bool IsConfirmed,
    IReadOnlyList<string> Errors)
{
    public static LiveDeploymentConfirmationResult Confirmed() =>
        new(true, Array.Empty<string>());

    public static LiveDeploymentConfirmationResult Blocked(IReadOnlyList<string> errors) =>
        new(false, errors);
}

public static class LiveDeploymentReviewGuard
{
    private const int MaximumConfirmationPhraseLength = 128;

    public static LiveDeploymentReviewResult CreateReview(
        LiveDeploymentRequest request,
        LiveDeploymentReviewContext context,
        DateTimeOffset nowUtc)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(request.ProfileId))
            errors.Add("A profile id is required.");

        if (string.IsNullOrWhiteSpace(context.ProfileName))
            errors.Add("A profile name is required for live review.");

        if (string.IsNullOrWhiteSpace(context.ConfiguredScriptRelativePath))
            errors.Add("The configured deployment script is missing.");

        if (!DeploymentScriptApprovalValidator.IsSha256(context.ApprovedScriptSha256))
            errors.Add("The approved deployment-script SHA-256 is missing or invalid.");
        if (!DeploymentScriptApprovalValidator.IsSha256(context.CurrentScriptSha256))
            errors.Add("The current deployment-script SHA-256 is missing or invalid.");
        if (DeploymentScriptApprovalValidator.IsSha256(context.ApprovedScriptSha256) &&
            DeploymentScriptApprovalValidator.IsSha256(context.CurrentScriptSha256) &&
            !string.Equals(context.ApprovedScriptSha256, context.CurrentScriptSha256, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("The deployment script fingerprint has changed since approval.");
        }

        if (!context.DeploymentAccessReady)
            errors.Add("Deployment access must be READY before a live review can be created.");

        if (context.ExecutionAlreadyRunning)
            errors.Add("Another deployment execution is already running.");

        if (!DryRunExecutionGuard.IsAllowlistedTarget(request.TargetId, context.AllowedTargetIds))
            errors.Add("The selected live-deployment target is not allowlisted.");

        if (string.IsNullOrWhiteSpace(context.TargetLabel))
            errors.Add("The selected live-deployment target label is missing.");

        if (!DryRunExecutionGuard.IsSafeReleaseToken(request.Release))
            errors.Add("The release must be a bounded token using only letters, digits, '.', '_' or '-'.");

        if (!IsSafeConfirmationPhrase(context.LiveConfirmationPhrase))
            errors.Add("The live confirmation phrase is missing, too long, or contains control characters.");

        if (errors.Count != 0)
            return LiveDeploymentReviewResult.Blocked(errors);

        LiveDeploymentReviewSnapshot snapshot = new(
            ReviewId: Guid.NewGuid().ToString("D"),
            ProfileId: request.ProfileId,
            ProfileName: context.ProfileName.Trim(),
            ScriptRelativePath: context.ConfiguredScriptRelativePath.Trim(),
            ScriptSha256: context.CurrentScriptSha256.ToUpperInvariant(),
            TargetId: request.TargetId,
            TargetLabel: context.TargetLabel.Trim(),
            Release: request.Release,
            LiveConfirmationPhrase: context.LiveConfirmationPhrase,
            CreatedAtUtc: nowUtc);

        return LiveDeploymentReviewResult.Allowed(snapshot);
    }

    public static LiveDeploymentConfirmationResult ValidateConfirmation(
        LiveDeploymentReviewSnapshot snapshot,
        string? operatorTypedPhrase,
        LiveDeploymentConfirmationContext current)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(snapshot.ReviewId))
            errors.Add("The live deployment review is invalid.");

        if (!current.DeploymentAccessReady)
            errors.Add("Deployment access is no longer READY. Create a new live review.");

        if (current.ExecutionAlreadyRunning)
            errors.Add("Another deployment execution is already running.");

        if (!string.Equals(snapshot.ProfileId, current.ProfileId, StringComparison.Ordinal))
            errors.Add("The active profile changed after the live review was created.");

        if (!string.Equals(snapshot.ScriptRelativePath, current.ScriptRelativePath, StringComparison.Ordinal))
            errors.Add("The configured deployment script changed after the live review was created.");

        if (!DeploymentScriptApprovalValidator.IsSha256(current.CurrentScriptSha256) ||
            !string.Equals(snapshot.ScriptSha256, current.CurrentScriptSha256, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("The deployment script fingerprint changed after the live review was created.");
        }

        if (!string.Equals(snapshot.TargetId, current.TargetId, StringComparison.Ordinal))
            errors.Add("The selected target changed after the live review was created.");

        if (!string.Equals(snapshot.Release, current.Release, StringComparison.Ordinal))
            errors.Add("The selected release changed after the live review was created.");

        if (!string.Equals(snapshot.LiveConfirmationPhrase, current.LiveConfirmationPhrase, StringComparison.Ordinal))
            errors.Add("The configured live confirmation phrase changed after the review was created.");

        if (!string.Equals(operatorTypedPhrase, snapshot.LiveConfirmationPhrase, StringComparison.Ordinal))
            errors.Add("The live confirmation phrase must be typed exactly as displayed.");

        return errors.Count == 0
            ? LiveDeploymentConfirmationResult.Confirmed()
            : LiveDeploymentConfirmationResult.Blocked(errors);
    }

    public static bool IsSafeConfirmationPhrase(string? phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase) || phrase.Length > MaximumConfirmationPhraseLength)
            return false;

        foreach (char character in phrase)
        {
            if (char.IsControl(character))
                return false;
        }

        return true;
    }
}
