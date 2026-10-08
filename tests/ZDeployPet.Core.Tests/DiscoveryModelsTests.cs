using ZDeployPet.Core;

namespace ZDeployPet.Core.Tests;

public sealed class DiscoveryModelsTests
{
    [Fact]
    public void OptionsPreserveExplicitLocations()
    {
        DiscoveryOptions options = new("C:\\repo", "Ubuntu", "/mnt/c/repo", "/tmp/reports", "deploy.sh");
        Assert.Equal("Ubuntu", options.WslDistribution);
        Assert.Equal("/tmp/reports", options.DeploymentReportRoot);
    }
}
