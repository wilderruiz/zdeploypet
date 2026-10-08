using ZDeployPet.Core;
using ZDeployPet.Infrastructure;

namespace ZDeployPet.Infrastructure.Tests;

public sealed class ProfileStoreTests
{
    [Fact]
    public async Task SavesAndReloadsActiveProfileBelowInjectedRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            DeploymentProfile profile = new(
                1, Guid.NewGuid().ToString("D"), "Example", "C:\\project", "Ubuntu", "/mnt/c/project",
                "deploy.sh", "/home/user/reports", "DEPLOY",
                [new DeploymentTarget("production", "Production", "example.invalid", 22, "deploy")],
                [new DeploymentDestination("app", "production", "Application", "/srv/example")]);
            ProfileStore store = new(root);

            await store.SaveActiveAsync(profile);
            DeploymentProfile? loaded = await store.LoadActiveAsync();

            Assert.NotNull(loaded);
            Assert.Equal(profile.Id, loaded.Id);
            Assert.Equal(profile.Name, loaded.Name);
            Assert.Single(loaded.Targets);
            Assert.Equal("example.invalid", loaded.Targets[0].Host);
            Assert.Single(loaded.Destinations);
            Assert.Equal("/srv/example", loaded.Destinations[0].RemotePath);
            Assert.True(File.Exists(Path.Combine(root, "settings.json")));
            Assert.True(File.Exists(Path.Combine(root, "profiles", profile.Id + ".json")));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
