using ZDeployPet.Core;
using ZDeployPet.WslBridge;

namespace ZDeployPet.Infrastructure.Tests;

public sealed class WslSshAgentSessionTests
{
    [Fact]
    public void ParsesAgentPidFromStandardSshAgentOutput()
    {
        const string output = "SSH_AUTH_SOCK=/tmp/ssh-abc/agent.100; export SSH_AUTH_SOCK;\nSSH_AGENT_PID=4242; export SSH_AGENT_PID;\necho Agent pid 4242;\n";
        Assert.Equal(4242, WslSshAgentSession.ParseAgentPid(output));
    }

    [Fact]
    public void ParsesDistinctSha256FingerprintsFromSshAddOutput()
    {
        const string output = "256 SHA256:alpha zdeploypet:test (ED25519)\n3072 SHA256:beta old-key (RSA)\n256 SHA256:alpha duplicate (ED25519)\n";
        IReadOnlyList<string> fingerprints = WslSshAgentSession.ParseLoadedFingerprints(output);
        Assert.Equal(["SHA256:alpha", "SHA256:beta"], fingerprints);
    }

    [Fact]
    public void CreatesEightHourLeaseByDefault()
    {
        DateTimeOffset now = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
        WslSshAgentRuntime runtime = new("Ubuntu-24.04", 77, "/tmp/zdeploypet.sock", now);
        DeploymentAccessSessionLease lease = WslSshAgentSession.CreateLease("profile", runtime, "SHA256:key", now);

        Assert.Equal(TimeSpan.FromHours(8), lease.ExpiresAtUtc - lease.StartedAtUtc);
        Assert.Equal(77, lease.AgentPid);
        Assert.Equal("/tmp/zdeploypet.sock", lease.AgentSocketPath);
        Assert.Equal("SHA256:key", lease.ApprovedKeyFingerprint);
    }

    [Fact]
    public void RejectsNonPositiveLeaseLifetime()
    {
        WslSshAgentRuntime runtime = new("Ubuntu-24.04", 77, "/tmp/zdeploypet.sock", DateTimeOffset.UtcNow);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WslSshAgentSession.CreateLease("profile", runtime, "SHA256:key", lifetime: TimeSpan.Zero));
    }
}
