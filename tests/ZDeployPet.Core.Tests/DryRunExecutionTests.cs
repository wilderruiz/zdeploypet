using ZDeployPet.Core;

namespace ZDeployPet.Core.Tests;

public sealed class DryRunExecutionTests
{
    private const string ApprovedSha = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    private const string ChangedSha = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";

    [Fact]
    public void AllowsKnownTargetSafeReleaseReadySessionAndMatchingScript()
    {
        DryRunExecutionGuardResult result = DryRunExecutionGuard.Validate(
            new DryRunRequest("profile-1", "hostinger", "4.4.23"),
            Context());

        Assert.True(result.IsAllowed);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void BlocksChangedScriptFingerprint()
    {
        DryRunExecutionGuardResult result = DryRunExecutionGuard.Validate(
            new DryRunRequest("profile-1", "hostinger", "4.4.23"),
            Context(currentSha: ChangedSha));

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Errors, error => error.Contains("fingerprint has changed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BlocksNonReadyDeploymentAccess()
    {
        DryRunExecutionGuardResult result = DryRunExecutionGuard.Validate(
            new DryRunRequest("profile-1", "hostinger", "4.4.23"),
            Context(deploymentAccessReady: false));

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Errors, error => error.Contains("must be READY", StringComparison.Ordinal));
    }

    [Fact]
    public void BlocksConcurrentExecution()
    {
        DryRunExecutionGuardResult result = DryRunExecutionGuard.Validate(
            new DryRunRequest("profile-1", "hostinger", "4.4.23"),
            Context(executionAlreadyRunning: true));

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Errors, error => error.Contains("already running", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("hostinger;rm -rf /")]
    [InlineData("$(whoami)")]
    [InlineData("hostinger\nvps")]
    public void BlocksTargetsOutsideExactAllowlist(string target)
    {
        DryRunExecutionGuardResult result = DryRunExecutionGuard.Validate(
            new DryRunRequest("profile-1", target, "4.4.23"),
            Context());

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Errors, error => error.Contains("target is not allowlisted", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("4.4.23")]
    [InlineData("release-2026_10.10")]
    [InlineData("v1")]
    [InlineData("20261010")]
    public void AcceptsBoundedReleaseTokens(string release)
    {
        Assert.True(DryRunExecutionGuard.IsSafeReleaseToken(release));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" release")]
    [InlineData("release value")]
    [InlineData("../release")]
    [InlineData("release/next")]
    [InlineData("release;touch-pwned")]
    [InlineData("$(whoami)")]
    [InlineData("`whoami`")]
    [InlineData("$HOME")]
    [InlineData("'release'")]
    [InlineData("\"release\"")]
    [InlineData("release\nnext")]
    public void RejectsShellLikeOrPathLikeReleaseInput(string release)
    {
        Assert.False(DryRunExecutionGuard.IsSafeReleaseToken(release));
    }

    [Fact]
    public void RejectsReleaseLongerThanSixtyFourCharacters()
    {
        Assert.False(DryRunExecutionGuard.IsSafeReleaseToken("a" + new string('b', 64)));
    }

    [Theory]
    [InlineData("hostinger")]
    [InlineData("HOSTINGER")]
    [InlineData("vps")]
    public void AllowlistedTargetComparisonIsCaseInsensitive(string target)
    {
        Assert.True(DryRunExecutionGuard.IsAllowlistedTarget(target, ["hostinger", "vps"]));
    }

    [Fact]
    public void InvalidOrMissingHashesFailClosed()
    {
        DryRunExecutionGuardResult result = DryRunExecutionGuard.Validate(
            new DryRunRequest("profile-1", "hostinger", "4.4.23"),
            Context(approvedSha: "not-a-hash", currentSha: string.Empty));

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Errors, error => error.Contains("approved deployment-script SHA-256", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => error.Contains("current deployment-script SHA-256", StringComparison.Ordinal));
    }

    private static DryRunExecutionGuardContext Context(
        string approvedSha = ApprovedSha,
        string currentSha = ApprovedSha,
        bool deploymentAccessReady = true,
        bool executionAlreadyRunning = false)
        => new(
            "deploy_millenova.sh",
            approvedSha,
            currentSha,
            ["hostinger", "vps", "both"],
            deploymentAccessReady,
            executionAlreadyRunning);
}
