using System.Text.Json;
using ZDeployPet.Core;

namespace ZDeployPet.Infrastructure;

public sealed class DeploymentScriptApprovalStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _root;

    public DeploymentScriptApprovalStore(string? root = null)
    {
        string appRoot = root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Zomniverse", "ZDeployPet");
        _root = Path.Combine(appRoot, "script-approvals");
    }

    public async Task SaveAsync(
        DeploymentScriptApproval approval,
        CancellationToken cancellationToken = default)
    {
        ValidateForStorage(approval);
        Directory.CreateDirectory(_root);
        string path = GetPath(approval.ProfileId);
        string temporaryPath = path + ".tmp";

        await using (FileStream stream = new(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, approval, JsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        File.Move(temporaryPath, path, true);
    }

    public async Task<DeploymentScriptApproval?> LoadAsync(
        string profileId,
        CancellationToken cancellationToken = default)
    {
        string path = GetPath(profileId);
        if (!File.Exists(path)) return null;

        try
        {
            await using FileStream stream = File.OpenRead(path);
            DeploymentScriptApproval? approval = await JsonSerializer.DeserializeAsync<DeploymentScriptApproval>(
                stream,
                JsonOptions,
                cancellationToken);
            if (approval is null || approval.SchemaVersion != DeploymentScriptApproval.CurrentSchemaVersion)
                return null;
            if (!string.Equals(approval.ProfileId, profileId, StringComparison.Ordinal))
                return null;
            return approval;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private string GetPath(string profileId)
    {
        if (!Guid.TryParse(profileId, out Guid parsed) || parsed.ToString("D") != profileId.ToLowerInvariant())
            throw new InvalidDataException("Profile ID must be a canonical GUID.");
        return Path.Combine(_root, profileId + ".json");
    }

    private static void ValidateForStorage(DeploymentScriptApproval approval)
    {
        if (approval.SchemaVersion != DeploymentScriptApproval.CurrentSchemaVersion)
            throw new InvalidDataException("Unsupported deployment-script approval schema.");
        if (string.IsNullOrWhiteSpace(approval.ScriptRelativePath) || Path.IsPathRooted(approval.ScriptRelativePath))
            throw new InvalidDataException("Deployment-script approval requires a relative script path.");
        if (!DeploymentScriptApprovalValidator.IsSha256(approval.Sha256))
            throw new InvalidDataException("Deployment-script approval requires a valid SHA-256 fingerprint.");
        if (approval.ApprovedAtUtc == default)
            throw new InvalidDataException("Deployment-script approval requires an approval timestamp.");
    }
}
