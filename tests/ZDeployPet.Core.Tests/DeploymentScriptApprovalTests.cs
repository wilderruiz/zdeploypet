using ZDeployPet.Core;

namespace ZDeployPet.Core.Tests;

public sealed class DeploymentScriptApprovalTests
{
    private const string Sha = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public void MatchingApprovalPasses()
    {
        string profileId = Guid.NewGuid().ToString("D");
        DeploymentScriptApproval approval = new(
            DeploymentScriptApproval.CurrentSchemaVersion,
            profileId,
            "deploy_millenova.sh",
            Sha,
            DateTimeOffset.UtcNow);

        DeploymentScriptApprovalMatchResult result = DeploymentScriptApprovalValidator.Validate(
            approval,
            profileId,
            "deploy_millenova.sh",
            Sha.ToLowerInvariant());

        Assert.True(result.IsMatch);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ChangedFingerprintFailsClosed()
    {
        string profileId = Guid.NewGuid().ToString("D");
        DeploymentScriptApproval approval = new(
            DeploymentScriptApproval.CurrentSchemaVersion,
            profileId,
            "deploy_millenova.sh",
            Sha,
            DateTimeOffset.UtcNow);

        DeploymentScriptApprovalMatchResult result = DeploymentScriptApprovalValidator.Validate(
            approval,
            profileId,
            "deploy_millenova.sh",
            new string('B', 64));

        Assert.False(result.IsMatch);
        Assert.Contains(result.Errors, error => error.Contains("fingerprint has changed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DifferentScriptPathFailsClosed()
    {
        string profileId = Guid.NewGuid().ToString("D");
        DeploymentScriptApproval approval = new(
            DeploymentScriptApproval.CurrentSchemaVersion,
            profileId,
            "scripts/deploy.sh",
            Sha,
            DateTimeOffset.UtcNow);

        DeploymentScriptApprovalMatchResult result = DeploymentScriptApprovalValidator.Validate(
            approval,
            profileId,
            "other/deploy.sh",
            Sha);

        Assert.False(result.IsMatch);
        Assert.Contains(result.Errors, error => error.Contains("path", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PromptProtocolEmitsOnlyAfterExactExpectedPrompts()
    {
        DryRunPromptProtocol protocol = new(new DryRunPromptPlan(TargetChoice: 3, ReleaseChoice: 1));

        Assert.Null(protocol.Observe("preflight noise\n"));
        Assert.Equal("3", protocol.Observe("Choose deployment destination [1/2/3]: "));
        Assert.Null(protocol.Observe("many validator lines\n"));
        Assert.Equal("1", protocol.Observe("Choose release type [1/2/3]: "));
        Assert.Equal("1", protocol.Observe("Choose [1/2]: "));
        Assert.Null(protocol.Observe("DRY RUN — NOTHING WILL BE MODIFIED\n"));
        Assert.True(protocol.DryModeConfirmed);
        Assert.Null(protocol.Observe("DRY RUN COMPLETE\n"));
        Assert.True(protocol.Completed);
        Assert.False(protocol.SafetyViolation);
    }

    [Fact]
    public void VpsStyleProtocolSkipsReleasePrompt()
    {
        DryRunPromptProtocol protocol = new(new DryRunPromptPlan(TargetChoice: 2, ReleaseChoice: null));

        Assert.Equal("2", protocol.Observe("Choose deployment destination [1/2/3]: "));
        Assert.Equal("1", protocol.Observe("Choose [1/2]: "));
        Assert.Null(protocol.Observe("DRY RUN — NOTHING WILL BE MODIFIED\nDRY RUN COMPLETE\n"));
        Assert.True(protocol.Completed);
    }

    [Fact]
    public void LiveMarkerFailsClosedAndNeverEmitsConfirmationPhrase()
    {
        DryRunPromptProtocol protocol = new(new DryRunPromptPlan(TargetChoice: 3, ReleaseChoice: 1));

        Assert.Equal("3", protocol.Observe("Choose deployment destination [1/2/3]: "));
        Assert.Equal("1", protocol.Observe("Choose release type [1/2/3]: "));
        Assert.Equal("1", protocol.Observe("Choose [1/2]: "));
        Assert.Null(protocol.Observe("LIVE MILLENOVA DEPLOYMENT\nType DEPLOY MILLENOVA to continue: "));

        Assert.True(protocol.SafetyViolation);
        Assert.False(protocol.Completed);
        Assert.DoesNotContain("DEPLOY MILLENOVA", new[] { "3", "1", "1" });
    }

    [Fact]
    public void SuccessfulExitWithoutVerifiedDryCompletionFailsClosed()
    {
        DryRunPromptProtocol protocol = new(new DryRunPromptPlan(TargetChoice: 1, ReleaseChoice: 1));

        _ = protocol.Observe("Choose deployment destination [1/2/3]: ");
        _ = protocol.Observe("Choose release type [1/2/3]: ");
        _ = protocol.Observe("Choose [1/2]: ");
        protocol.MarkProcessExited(0);

        Assert.True(protocol.SafetyViolation);
        Assert.Contains("without completing", protocol.ViolationReason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(4, null)]
    [InlineData(1, 0)]
    [InlineData(1, 4)]
    public void PromptPlanRejectsOutOfRangeChoices(int targetChoice, int? releaseChoice)
    {
        DryRunPromptPlan plan = new(targetChoice, releaseChoice);

        Assert.Throws<InvalidDataException>(() => plan.Validate());
    }
}
