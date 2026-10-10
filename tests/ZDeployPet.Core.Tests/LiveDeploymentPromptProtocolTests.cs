using ZDeployPet.Core;

namespace ZDeployPet.Core.Tests;

public sealed class LiveDeploymentPromptProtocolTests
{
    private const string Phrase = "DEPLOY MILLENOVA";

    [Fact]
    public void CompleteLiveFlowIsAcceptedAcrossSplitChunks()
    {
        LiveDeploymentPromptProtocol protocol = new(new LiveDeploymentPromptPlan(3, 1, Phrase));

        protocol.Observe("LIVE MILLENOVA ");
        protocol.Observe("DEPLOYMENT\n");
        protocol.Observe("Type DEPLOY MILLENOVA ");
        protocol.Observe("to continue: ");
        protocol.Observe("MILLENOVA DEPLOYMENT ");
        protocol.Observe("COMPLETE\n");

        Assert.True(protocol.LiveModeObserved);
        Assert.True(protocol.ConfirmationPromptObserved);
        Assert.True(protocol.Completed);
        Assert.False(protocol.SafetyViolation);
    }

    [Fact]
    public void DryRunMarkerFailsClosed()
    {
        LiveDeploymentPromptProtocol protocol = new(new LiveDeploymentPromptPlan(3, 1, Phrase));

        protocol.Observe("DRY RUN — NOTHING WILL BE MODIFIED");

        Assert.True(protocol.SafetyViolation);
        Assert.Contains("dry-run mode", protocol.ViolationReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CompletionWithoutLiveMarkerFailsClosed()
    {
        LiveDeploymentPromptProtocol protocol = new(new LiveDeploymentPromptPlan(3, 1, Phrase));

        protocol.Observe("Type DEPLOY MILLENOVA to continue: ");
        protocol.Observe("MILLENOVA DEPLOYMENT COMPLETE");

        Assert.True(protocol.SafetyViolation);
        Assert.False(protocol.Completed);
    }

    [Fact]
    public void CompletionWithoutExactConfirmationPromptFailsClosed()
    {
        LiveDeploymentPromptProtocol protocol = new(new LiveDeploymentPromptPlan(3, 1, Phrase));

        protocol.Observe("LIVE MILLENOVA DEPLOYMENT");
        protocol.Observe("MILLENOVA DEPLOYMENT COMPLETE");

        Assert.True(protocol.SafetyViolation);
        Assert.False(protocol.Completed);
    }

    [Fact]
    public void SuccessfulExitWithoutVerifiedCompletionFailsClosed()
    {
        LiveDeploymentPromptProtocol protocol = new(new LiveDeploymentPromptPlan(3, 1, Phrase));

        protocol.Observe("LIVE MILLENOVA DEPLOYMENT");
        protocol.MarkProcessExited(0);

        Assert.True(protocol.SafetyViolation);
        Assert.False(protocol.Completed);
    }

    [Theory]
    [InlineData(0, 1, Phrase)]
    [InlineData(4, 1, Phrase)]
    [InlineData(3, 0, Phrase)]
    [InlineData(3, 4, Phrase)]
    [InlineData(3, 1, "")]
    public void InvalidPlanFailsClosedAtConstruction(int target, int release, string phrase)
    {
        Assert.Throws<InvalidDataException>(() =>
            new LiveDeploymentPromptProtocol(new LiveDeploymentPromptPlan(target, release, phrase)));
    }
}
