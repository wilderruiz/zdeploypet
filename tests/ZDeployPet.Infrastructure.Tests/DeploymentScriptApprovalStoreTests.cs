using ZDeployPet.Core;
using ZDeployPet.Infrastructure;

namespace ZDeployPet.Infrastructure.Tests;

public sealed class DeploymentScriptApprovalStoreTests
{
    private const string Sha = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task SaveAndLoadRoundTripsApproval()
    {
        string root = Path.Combine(Path.GetTempPath(), "zdeploypet-tests", Guid.NewGuid().ToString("N"));
        try
        {
            string profileId = Guid.NewGuid().ToString("D");
            DeploymentScriptApprovalStore store = new(root);
            DeploymentScriptApproval approval = new(
                DeploymentScriptApproval.CurrentSchemaVersion,
                profileId,
                "deploy_millenova.sh",
                Sha,
                DateTimeOffset.Parse("2026-10-10T16:00:00Z"));

            await store.SaveAsync(approval);
            DeploymentScriptApproval? loaded = await store.LoadAsync(profileId);

            Assert.Equal(approval, loaded);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task DamagedApprovalFailsClosed()
    {
        string root = Path.Combine(Path.GetTempPath(), "zdeploypet-tests", Guid.NewGuid().ToString("N"));
        try
        {
            string profileId = Guid.NewGuid().ToString("D");
            string approvalRoot = Path.Combine(root, "script-approvals");
            Directory.CreateDirectory(approvalRoot);
            await File.WriteAllTextAsync(Path.Combine(approvalRoot, profileId + ".json"), "{not-json");

            DeploymentScriptApprovalStore store = new(root);
            Assert.Null(await store.LoadAsync(profileId));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task InvalidFingerprintIsNotPersisted()
    {
        string root = Path.Combine(Path.GetTempPath(), "zdeploypet-tests", Guid.NewGuid().ToString("N"));
        try
        {
            DeploymentScriptApprovalStore store = new(root);
            DeploymentScriptApproval approval = new(
                DeploymentScriptApproval.CurrentSchemaVersion,
                Guid.NewGuid().ToString("D"),
                "deploy_millenova.sh",
                "NOT-A-SHA",
                DateTimeOffset.UtcNow);

            await Assert.ThrowsAsync<InvalidDataException>(() => store.SaveAsync(approval));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
