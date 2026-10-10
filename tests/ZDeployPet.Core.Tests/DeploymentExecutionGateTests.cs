namespace ZDeployPet.Core.Tests;

public sealed class DeploymentExecutionGateTests
{
    [Fact]
    public void Gate_AllowsOnlyOneLeaseAtATime()
    {
        DeploymentExecutionGate gate = new();

        Assert.True(gate.TryAcquire(out IDisposable? first));
        Assert.NotNull(first);
        Assert.True(gate.IsRunning);
        Assert.False(gate.TryAcquire(out IDisposable? second));
        Assert.Null(second);

        first.Dispose();

        Assert.False(gate.IsRunning);
        Assert.True(gate.TryAcquire(out IDisposable? third));
        Assert.NotNull(third);
        third.Dispose();
    }
}
