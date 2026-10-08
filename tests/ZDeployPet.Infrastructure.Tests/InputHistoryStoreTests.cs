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

    [Fact]
    public async Task SavesAndReloadsAnIncompleteSetupDraft()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            SetupDraft draft = new(
                "Millenova", "", "Ubuntu-24.04", "", "", "DEPLOY",
                [], []);
            InputHistoryStore store = new(root);

            await store.SaveDraftAsync(draft);
            SetupDraft? loaded = await store.LoadDraftAsync();
            InputHistory history = await store.LoadAsync();

            Assert.NotNull(loaded);
            Assert.Equal("Millenova", loaded.ProfileName);
            Assert.DoesNotContain("Millenova", history.ProfileNames);
            await store.RememberDraftAsync(draft);
            Assert.Contains("Millenova", (await store.LoadAsync()).ProfileNames);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task ConcurrentDraftWritesDoNotCompeteForOneTemporaryFile()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            InputHistoryStore store = new(root);
            SetupDraft first = new("First", "", "Ubuntu", "", "", "DEPLOY", [], []);
            SetupDraft second = first with { ProfileName = "Second" };

            await Task.WhenAll(store.SaveDraftAsync(first), store.SaveDraftAsync(second));

            SetupDraft? loaded = await store.LoadDraftAsync();
            Assert.NotNull(loaded);
            Assert.Contains(loaded.ProfileName, new[] { "First", "Second" });
            Assert.Empty(Directory.GetFiles(root, "*.tmp.*"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task SynchronousClosePathPersistsDraftWithoutAsyncWait()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            InputHistoryStore store = new(root);
            SetupDraft draft = new("Millenova", "", "Ubuntu-24.04", "", "", "DEPLOY", [], []);

            store.SaveDraft(draft);

            Assert.Equal("Millenova", (await store.LoadDraftAsync())?.ProfileName);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
