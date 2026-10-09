using System.Text.Json;
using ZDeployPet.Core;

namespace ZDeployPet.Infrastructure;

public sealed class DeploymentAccessIdentityStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _root;

    public DeploymentAccessIdentityStore(string? root = null)
    {
        string applicationRoot = root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Zomniverse", "ZDeployPet");
        _root = Path.Combine(applicationRoot, "identity");
    }

    public async Task<DeploymentAccessIdentity> LoadAsync(
        string profileId,
        CancellationToken cancellationToken = default)
    {
        string path = GetPath(profileId);
        if (!File.Exists(path)) return DeploymentAccessIdentity.Empty(profileId);

        await using FileStream stream = File.OpenRead(path);
        DeploymentAccessIdentity? identity = await JsonSerializer.DeserializeAsync<DeploymentAccessIdentity>(
            stream, JsonOptions, cancellationToken);

        if (identity is null ||
            identity.SchemaVersion != DeploymentAccessIdentity.CurrentSchemaVersion ||
            !string.Equals(identity.ProfileId, profileId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Deployment identity metadata is invalid or belongs to another profile.");
        }

        return identity;
    }

    public async Task SaveAsync(
        DeploymentAccessIdentity identity,
        CancellationToken cancellationToken = default)
    {
        if (identity.SchemaVersion != DeploymentAccessIdentity.CurrentSchemaVersion)
            throw new InvalidDataException("Unsupported deployment identity schema.");

        string path = GetPath(identity.ProfileId);
        Directory.CreateDirectory(_root);
        string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            await using (FileStream stream = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, identity, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                try { File.Delete(temporaryPath); } catch { }
            }
        }
    }

    public async Task SaveDeploymentKeyAsync(
        string profileId,
        DeploymentKeyIdentity deploymentKey,
        CancellationToken cancellationToken = default)
    {
        DeploymentAccessIdentity current = await LoadAsync(profileId, cancellationToken);
        await SaveAsync(current with { DeploymentKey = deploymentKey }, cancellationToken);
    }

    public async Task EnrollHostKeyAsync(
        string profileId,
        TargetHostIdentity hostKey,
        CancellationToken cancellationToken = default)
    {
        DeploymentAccessIdentity current = await LoadAsync(profileId, cancellationToken);
        List<TargetHostIdentity> hostKeys = current.HostKeys
            .Where(item => !string.Equals(item.TargetId, hostKey.TargetId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        hostKeys.Add(hostKey);
        await SaveAsync(current with { HostKeys = hostKeys }, cancellationToken);
    }

    private string GetPath(string profileId)
    {
        if (!Guid.TryParse(profileId, out Guid parsed) ||
            !string.Equals(parsed.ToString("D"), profileId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Profile ID must be a canonical GUID.");

        return Path.Combine(_root, parsed.ToString("D") + ".json");
    }
}
