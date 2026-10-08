using ZDeployPet.Infrastructure;

namespace ZDeployPet.Infrastructure.Tests;

public sealed class LocalCapabilityDiscoveryTests
{
    [Fact]
    public void MissingRepositoryIsReportedWithoutMutation()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var result = new LocalCapabilityDiscovery().Discover(path, "deploy.sh");
        Assert.False(result.RepositoryExists);
        Assert.False(result.DeployScriptExists);
        Assert.Null(result.DeployScriptSha256);
        Assert.False(Directory.Exists(path));
    }

    [Fact]
    public void ComputesKnownSha256()
    {
        string file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, "millenova");
            Assert.Equal("F1707AD5028962E0A3EA86185B32108864F9749CB2629F85C888422CEAE0EB36",
                LocalCapabilityDiscovery.ComputeSha256(file));
        }
        finally
        {
            File.Delete(file);
        }
    }
}
