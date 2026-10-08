using ZDeployPet.Core;
using ZDeployPet.Infrastructure;

namespace ZDeployPet.Infrastructure.Tests;

public sealed class InputHistoryStoreTests
{
    [Fact]
    public async Task RemembersNonSecretProfileValuesAndCanForgetThem()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            DeploymentProfile profile = new(
                1, Guid.NewGuid().ToString("D"), "Example", "C:\\project", "Ubuntu", "/mnt/c/project",
                "deploy.sh", "/home/user/reports", "DEPLOY",
                [new DeploymentTarget("production", "Production", "example.invalid", 2222, "deploy")],
                [new DeploymentDestination("app", "production", "Application", "/srv/example")]);
            InputHistoryStore store = new(root);

            await store.RememberAsync(profile);
            InputHistory history = await store.LoadAsync();

            Assert.Contains("Example", history.ProfileNames);
            Assert.Contains("example.invalid", history.Hosts);
            Assert.Contains("2222", history.Ports);
            Assert.Contains("/srv/example", history.RemotePaths);

            await store.ClearAsync();
            Assert.Equal(InputHistory.Empty, await store.LoadAsync());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
