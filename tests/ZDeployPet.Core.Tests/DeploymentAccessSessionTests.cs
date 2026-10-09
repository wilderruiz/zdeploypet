using ZDeployPet.Core;

namespace ZDeployPet.Core.Tests;

public sealed class DeploymentAccessSessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ReadyWhenAgentAndApprovedKeyArePresent()
    {
        DeploymentAccessSessionLease lease = CreateLease(Now.AddHours(8));
        Assert.Equal(DeploymentAccessSessionState.Ready, lease.Evaluate(Now, true, true));
    }

    [Fact]
    public void ExpiringInsideDefaultThirtyMinuteWindow()
    {
        DeploymentAccessSessionLease lease = CreateLease(Now.AddMinutes(20));
        Assert.Equal(DeploymentAccessSessionState.Expiring, lease.Evaluate(Now, true, true));
    }

    [Fact]
    public void ExpiredWhenLeaseTimeHasPassed()
    {
        DeploymentAccessSessionLease lease = CreateLease(Now.AddSeconds(-1));
        Assert.Equal(DeploymentAccessSessionState.Expired, lease.Evaluate(Now, true, true));
    }

    [Fact]
    public void LockedWhenAgentOrApprovedKeyIsMissing()
    {
        DeploymentAccessSessionLease lease = CreateLease(Now.AddHours(8));
        Assert.Equal(DeploymentAccessSessionState.Locked, lease.Evaluate(Now, false, true));
        Assert.Equal(DeploymentAccessSessionState.Locked, lease.Evaluate(Now, true, false));
    }

    [Fact]
    public void InvalidWhenAgentMetadataIsMalformed()
    {
        DeploymentAccessSessionLease lease = new("profile", "Ubuntu", 0, string.Empty, "SHA256:x", Now, Now.AddHours(8));
        Assert.Equal(DeploymentAccessSessionState.Invalid, lease.Evaluate(Now, true, true));
    }

    private static DeploymentAccessSessionLease CreateLease(DateTimeOffset expiresAt) =>
        new("profile", "Ubuntu-24.04", 1234, "/tmp/zdeploypet.sock", "SHA256:test", Now, expiresAt);
}
