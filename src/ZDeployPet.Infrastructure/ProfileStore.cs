using System.Text.Json;
using ZDeployPet.Core;

namespace ZDeployPet.Infrastructure;

public sealed class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _root;
    private readonly string _profilesRoot;
    private readonly string _settingsPath;

    public ProfileStore(string? root = null)
    {
        _root = root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Zomniverse", "ZDeployPet");
        _profilesRoot = Path.Combine(_root, "profiles");
        _settingsPath = Path.Combine(_root, "settings.json");
    }

    public string Root => _root;

    public async Task<DeploymentProfile?> LoadActiveAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsPath)) return null;
        await using FileStream settingsStream = File.OpenRead(_settingsPath);
        AppSettings? settings = await JsonSerializer.DeserializeAsync<AppSettings>(settingsStream, JsonOptions, cancellationToken);
        if (settings is null || string.IsNullOrWhiteSpace(settings.ActiveProfileId)) return null;

        string profilePath = GetProfilePath(settings.ActiveProfileId);
        if (!File.Exists(profilePath)) return null;
        await using FileStream profileStream = File.OpenRead(profilePath);
        return await JsonSerializer.DeserializeAsync<DeploymentProfile>(profileStream, JsonOptions, cancellationToken);
    }

    public async Task SaveActiveAsync(DeploymentProfile profile, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_profilesRoot);
        string profilePath = GetProfilePath(profile.Id);
        await WriteAtomicAsync(profilePath, profile, cancellationToken);
        await WriteAtomicAsync(_settingsPath, new AppSettings(1, profile.Id), cancellationToken);
    }

    private string GetProfilePath(string profileId)
    {
        if (!Guid.TryParse(profileId, out Guid parsed) || parsed.ToString("D") != profileId.ToLowerInvariant())
            throw new InvalidDataException("Profile ID must be a canonical GUID.");
        return Path.Combine(_profilesRoot, profileId + ".json");
    }

    private static async Task WriteAtomicAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(path);
        if (directory is null) throw new InvalidOperationException("Profile path has no parent directory.");
        Directory.CreateDirectory(directory);
        string temporaryPath = path + ".tmp";
        await using (FileStream stream = new(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        File.Move(temporaryPath, path, true);
    }

    private sealed record AppSettings(int SchemaVersion, string ActiveProfileId);
}
