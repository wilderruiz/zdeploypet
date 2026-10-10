using ZDeployPet.Core;

namespace ZDeployPet.Core.Tests;

public sealed class LiveDeploymentReviewTests
{
    private const string Sha = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    private const string Phrase = "DEPLOY MILLENOVA";

    [Fact]
    public void ReviewSnapshotCapturesImmutableLiveIntent()
    {
        LiveDeploymentReviewResult result = LiveDeploymentReviewGuard.CreateReview(
            new LiveDeploymentRequest("profile-1", "both", "patch"),
            ReviewContext(),
            new DateTimeOffset(2026, 10, 10, 20, 0, 0, TimeSpan.Zero));

        Assert.True(result.IsAllowed);
        Assert.NotNull(result.Snapshot);
        Assert.Equal("profile-1", result.Snapshot!.ProfileId);
        Assert.Equal("both", result.Snapshot.TargetId);
        Assert.Equal("patch", result.Snapshot.Release);
        Assert.Equal(Sha, result.Snapshot.ScriptSha256);
        Assert.Equal(Phrase, result.Snapshot.LiveConfirmationPhrase);
        Assert.False(string.IsNullOrWhiteSpace(result.Snapshot.ReviewId));
    }

    [Fact]
    public void ExactTypedPhraseConfirmsUnchangedReview()
    {
        LiveDeploymentReviewSnapshot snapshot = CreateSnapshot();

        LiveDeploymentConfirmationResult result = LiveDeploymentReviewGuard.ValidateConfirmation(
            snapshot,
            Phrase,
            ConfirmationContext());

        Assert.True(result.IsConfirmed);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("deploy millenova")]
    [InlineData("DEPLOY MILLENOVA ")]
    [InlineData(" DEPLOY MILLENOVA")]
    [InlineData("")]
    public void ConfirmationPhraseMustMatchExactly(string typed)
    {
        LiveDeploymentConfirmationResult result = LiveDeploymentReviewGuard.ValidateConfirmation(
            CreateSnapshot(),
            typed,
            ConfirmationContext());

        Assert.False(result.IsConfirmed);
        Assert.Contains(result.Errors, error => error.Contains("typed exactly", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ScriptFingerprintChangeInvalidatesConfirmation()
    {
        LiveDeploymentConfirmationContext current = ConfirmationContext() with
        {
            CurrentScriptSha256 = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB"
        };

        LiveDeploymentConfirmationResult result = LiveDeploymentReviewGuard.ValidateConfirmation(
            CreateSnapshot(),
            Phrase,
            current);

        Assert.False(result.IsConfirmed);
        Assert.Contains(result.Errors, error => error.Contains("fingerprint changed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TargetOrReleaseChangeInvalidatesConfirmation()
    {
        LiveDeploymentConfirmationResult targetChanged = LiveDeploymentReviewGuard.ValidateConfirmation(
            CreateSnapshot(),
            Phrase,
            ConfirmationContext() with { TargetId = "hostinger" });
        LiveDeploymentConfirmationResult releaseChanged = LiveDeploymentReviewGuard.ValidateConfirmation(
            CreateSnapshot(),
            Phrase,
            ConfirmationContext() with { Release = "minor" });

        Assert.False(targetChanged.IsConfirmed);
        Assert.False(releaseChanged.IsConfirmed);
    }

    [Fact]
    public void LockingSessionInvalidatesConfirmation()
    {
        LiveDeploymentConfirmationResult result = LiveDeploymentReviewGuard.ValidateConfirmation(
            CreateSnapshot(),
            Phrase,
            ConfirmationContext() with { DeploymentAccessReady = false });

        Assert.False(result.IsConfirmed);
        Assert.Contains(result.Errors, error => error.Contains("no longer READY", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ConcurrentExecutionInvalidatesConfirmation()
    {
        LiveDeploymentConfirmationResult result = LiveDeploymentReviewGuard.ValidateConfirmation(
            CreateSnapshot(),
            Phrase,
            ConfirmationContext() with { ExecutionAlreadyRunning = true });

        Assert.False(result.IsConfirmed);
        Assert.Contains(result.Errors, error => error.Contains("already running", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReviewFailsClosedForUnsafeInputs()
    {
        LiveDeploymentReviewContext context = ReviewContext() with
        {
            DeploymentAccessReady = false,
            LiveConfirmationPhrase = "DEPLOY\nNOW"
        };

        LiveDeploymentReviewResult result = LiveDeploymentReviewGuard.CreateReview(
            new LiveDeploymentRequest("profile-1", "not-allowed", "patch;rm"),
            context,
            DateTimeOffset.UtcNow);

        Assert.False(result.IsAllowed);
        Assert.Null(result.Snapshot);
        Assert.NotEmpty(result.Errors);
    }

    private static LiveDeploymentReviewSnapshot CreateSnapshot()
    {
        LiveDeploymentReviewResult result = LiveDeploymentReviewGuard.CreateReview(
            new LiveDeploymentRequest("profile-1", "both", "patch"),
            ReviewContext(),
            DateTimeOffset.UtcNow);
        return Assert.IsType<LiveDeploymentReviewSnapshot>(result.Snapshot);
    }

    private static LiveDeploymentReviewContext ReviewContext() => new(
        ProfileName: "Millenova",
        ConfiguredScriptRelativePath: "deploy_millenova.sh",
        ApprovedScriptSha256: Sha,
        CurrentScriptSha256: Sha,
        AllowedTargetIds: ["hostinger", "vps", "both"],
        TargetLabel: "Both targets",
        LiveConfirmationPhrase: Phrase,
        DeploymentAccessReady: true,
        ExecutionAlreadyRunning: false);

    private static LiveDeploymentConfirmationContext ConfirmationContext() => new(
        ProfileId: "profile-1",
        ScriptRelativePath: "deploy_millenova.sh",
        CurrentScriptSha256: Sha,
        TargetId: "both",
        Release: "patch",
        LiveConfirmationPhrase: Phrase,
        DeploymentAccessReady: true,
        ExecutionAlreadyRunning: false);
}
