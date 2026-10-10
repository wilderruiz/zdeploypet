namespace ZDeployPet.Core;

public enum DryRunExecutionState
{
    Ready,
    Running,
    Completed,
    Blocked,
    Failed
}

public sealed record DryRunRequest(
    string ProfileId,
    string TargetId,
    string Release);

public sealed record DryRunExecutionGuardContext(
    string ConfiguredScriptRelativePath,
    string ApprovedScriptSha256,
    string CurrentScriptSha256,
    IReadOnlyList<string> AllowedTargetIds,
    bool DeploymentAccessReady,
    bool ExecutionAlreadyRunning);

public sealed record DryRunExecutionGuardResult(
    bool IsAllowed,
    IReadOnlyList<string> Errors)
{
    public static DryRunExecutionGuardResult Allowed() =>
        new(true, Array.Empty<string>());

    public static DryRunExecutionGuardResult Blocked(IReadOnlyList<string> errors) =>
        new(false, errors);
}

public static class DryRunExecutionGuard
{
    private const int MaximumReleaseLength = 64;

    public static DryRunExecutionGuardResult Validate(
        DryRunRequest request,
        DryRunExecutionGuardContext context)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(request.ProfileId))
            errors.Add("A profile id is required.");

        if (string.IsNullOrWhiteSpace(context.ConfiguredScriptRelativePath))
            errors.Add("The configured deployment script is missing.");

        if (!IsSha256(context.ApprovedScriptSha256))
            errors.Add("The approved deployment-script SHA-256 is missing or invalid.");
        if (!IsSha256(context.CurrentScriptSha256))
            errors.Add("The current deployment-script SHA-256 is missing or invalid.");
        if (IsSha256(context.ApprovedScriptSha256) &&
            IsSha256(context.CurrentScriptSha256) &&
            !string.Equals(
                context.ApprovedScriptSha256,
                context.CurrentScriptSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("The deployment script fingerprint has changed since approval.");
        }

        if (!context.DeploymentAccessReady)
            errors.Add("Deployment access must be READY before a dry run can start.");

        if (context.ExecutionAlreadyRunning)
            errors.Add("Another deployment execution is already running.");

        if (!IsAllowlistedTarget(request.TargetId, context.AllowedTargetIds))
            errors.Add("The selected dry-run target is not allowlisted.");

        if (!IsSafeReleaseToken(request.Release))
            errors.Add("The release must be a bounded token using only letters, digits, '.', '_' or '-'.");

        return errors.Count == 0
            ? DryRunExecutionGuardResult.Allowed()
            : DryRunExecutionGuardResult.Blocked(errors);
    }

    public static bool IsSafeReleaseToken(string? release)
    {
        if (string.IsNullOrWhiteSpace(release) || release.Length > MaximumReleaseLength)
            return false;

        char first = release[0];
        if (!IsAsciiLetterOrDigit(first))
            return false;

        foreach (char character in release)
        {
            if (IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-')
                continue;

            return false;
        }

        return true;
    }

    public static bool IsAllowlistedTarget(
        string? targetId,
        IReadOnlyList<string>? allowedTargetIds)
    {
        if (string.IsNullOrWhiteSpace(targetId) || allowedTargetIds is null || allowedTargetIds.Count == 0)
            return false;

        return allowedTargetIds.Any(allowed =>
            !string.IsNullOrWhiteSpace(allowed) &&
            string.Equals(allowed, targetId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSha256(string? value)
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

    private static bool IsAsciiLetterOrDigit(char character)
        => character is >= 'a' and <= 'z'
           or >= 'A' and <= 'Z'
           or >= '0' and <= '9';
}
