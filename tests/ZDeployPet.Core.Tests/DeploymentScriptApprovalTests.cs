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
    public void PromptPlanAlwaysAppendsDryMode()
    {
        DryRunPromptPlan plan = new(TargetChoice: 3, ReleaseChoice: 1);

        Assert.Equal(["3", "1", "1"], plan.BuildStandardInputLines());
    }

    [Fact]
    public void VpsStylePromptPlanCanOmitReleaseChoice()
    {
        DryRunPromptPlan plan = new(TargetChoice: 2, ReleaseChoice: null);

        Assert.Equal(["2", "1"], plan.BuildStandardInputLines());
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(4, null)]
    [InlineData(1, 0)]
    [InlineData(1, 4)]
    public void PromptPlanRejectsOutOfRangeChoices(int targetChoice, int? releaseChoice)
    {
        DryRunPromptPlan plan = new(targetChoice, releaseChoice);

        Assert.Throws<InvalidDataException>(() => plan.BuildStandardInputLines());
    }
}
