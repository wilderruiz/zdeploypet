using ZDeployPet.Core;
using ZDeployPet.Infrastructure;

namespace ZDeployPet.Infrastructure.Tests;

public sealed class DeploymentAccessIdentityStoreTests
{
    [Fact]
    public async Task SavesOnlyNonSecretIdentityMetadata()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string profileId = Guid.NewGuid().ToString("D");
        try
        {
            DeploymentAccessIdentityStore store = new(root);
            await store.SaveDeploymentKeyAsync(
                profileId,
                new DeploymentKeyIdentity("/home/example/.ssh/id_ed25519.pub", "SHA256:key", "ED25519", "deploy"));
            await store.EnrollHostKeyAsync(
                profileId,
                new TargetHostIdentity("target-1", "SHA256:host", "ED25519"));

            DeploymentAccessIdentity loaded = await store.LoadAsync(profileId);

            Assert.Equal("SHA256:key", loaded.DeploymentKey?.Fingerprint);
            Assert.Equal("/home/example/.ssh/id_ed25519.pub", loaded.DeploymentKey?.PublicKeyPath);
            Assert.Equal("SHA256:host", loaded.FindHostKey("target-1")?.Fingerprint);

            string json = await File.ReadAllTextAsync(
                Path.Combine(root, "identity", profileId + ".json"));
            Assert.DoesNotContain("passphrase", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("privateKey", json, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task ReEnrollmentReplacesTargetFingerprint()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string profileId = Guid.NewGuid().ToString("D");
        try
        {
            DeploymentAccessIdentityStore store = new(root);
            await store.EnrollHostKeyAsync(profileId, new TargetHostIdentity("target-1", "SHA256:first", "ED25519"));
            await store.EnrollHostKeyAsync(profileId, new TargetHostIdentity("target-1", "SHA256:second", "ED25519"));

            DeploymentAccessIdentity loaded = await store.LoadAsync(profileId);
            Assert.Single(loaded.HostKeys);
            Assert.Equal("SHA256:second", loaded.HostKeys[0].Fingerprint);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
