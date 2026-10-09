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

    [Fact]
    public async Task SetupSnapshotPreservesEveryRowAndLaterSaveCompletelyReplacesIt()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            ProfileStore store = new(root);
            SetupDraft first = new(
                "Millenova",
                @"H:\PROGRAMMING\public_html\millenova",
                "Ubuntu-24.04",
                "deploy_millenova.sh",
                "",
                "DEPLOY MILLENOVA",
                [
                    new SetupTargetDraft("Hostinger", "92.113.18.112", 65002, "u256155942"),
                    new SetupTargetDraft("VPS", "45.93.139.114", 22, "root")
                ],
                [
                    new SetupDestinationDraft("Hostinger", "Website", "/home/user/public_html"),
                    new SetupDestinationDraft("VPS", "Backend", "/opt/example/backend")
                ]);

            await store.SaveSetupSnapshotAsync(first, "/mnt/h/PROGRAMMING/public_html/millenova");
            SetupProfileSnapshot? loadedFirst = await store.LoadSetupSnapshotByNameAsync("Millenova");

            Assert.NotNull(loadedFirst);
            Assert.Equal(2, loadedFirst.Draft.Targets.Count);
            Assert.Contains(loadedFirst.Draft.Targets, target => target.Label == "VPS" && target.Host == "45.93.139.114");
            Assert.Equal(2, loadedFirst.Draft.Destinations.Count);

            SetupDraft replacement = first with
            {
                Targets = [new SetupTargetDraft("Hostinger", "92.113.18.112", 65002, "u256155942")],
                Destinations = []
            };

            await store.SaveSetupSnapshotAsync(replacement, "/mnt/h/PROGRAMMING/public_html/millenova");
            SetupProfileSnapshot? loadedReplacement = await store.LoadSetupSnapshotByNameAsync("Millenova");

            Assert.NotNull(loadedReplacement);
            Assert.Single(loadedReplacement.Draft.Targets);
            Assert.Equal("Hostinger", loadedReplacement.Draft.Targets[0].Label);
            Assert.Empty(loadedReplacement.Draft.Destinations);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
