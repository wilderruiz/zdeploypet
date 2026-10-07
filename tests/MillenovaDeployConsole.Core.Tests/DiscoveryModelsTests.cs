using MillenovaDeployConsole.Core;

namespace MillenovaDeployConsole.Core.Tests;

public sealed class DiscoveryModelsTests
{
    [Fact]
    public void OptionsPreserveExplicitLocations()
    {
        DiscoveryOptions options = new("C:\\repo", "Ubuntu", "/mnt/c/repo", "/tmp/reports");
        Assert.Equal("Ubuntu", options.WslDistribution);
        Assert.Equal("/tmp/reports", options.DeploymentReportRoot);
    }
}
