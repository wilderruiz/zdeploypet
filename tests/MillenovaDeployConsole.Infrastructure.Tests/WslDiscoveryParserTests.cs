using MillenovaDeployConsole.WslBridge;

namespace MillenovaDeployConsole.Infrastructure.Tests;

public sealed class WslDiscoveryParserTests
{
    [Fact]
    public void ProbeUsesExecAndPreservesPositionalPaths()
    {
        var options = new MillenovaDeployConsole.Core.DiscoveryOptions(
            "H:\\repo", "Ubuntu-24.04", "/mnt/h/repo", "/home/user/reports");

        var arguments = WslCapabilityDiscovery.CreateProbeStartInfo(options).ArgumentList;

        Assert.Equal("--exec", arguments[2]);
        Assert.Equal("pda0-probe", arguments[6]);
        Assert.Equal("/mnt/h/repo", arguments[7]);
        Assert.Equal("/home/user/reports", arguments[8]);
    }

    [Fact]
    public void ParsesProbeOutput()
    {
        const string output = "ssh_path=/usr/bin/ssh\nssh_version=OpenSSH_9.6p1\nssh_agent_path=/usr/bin/ssh-agent\nssh_add_path=/usr/bin/ssh-add\nrepository_exists=true\ndeploy_script_exists=true\nreport_root_exists=true\n";
        var result = WslDiscoveryParser.Parse("Ubuntu-24.04", output);
        Assert.True(result.Available);
        Assert.Equal("/usr/bin/ssh", result.SshPath);
        Assert.True(result.RepositoryExists);
        Assert.True(result.DeployScriptExists);
        Assert.True(result.ReportRootExists);
    }
}
