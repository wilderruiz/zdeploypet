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

        return await LoadByIdAsync(settings.ActiveProfileId, cancellationToken);
    }

    public async Task<IReadOnlyList<DeploymentProfile>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_profilesRoot)) return [];

        List<DeploymentProfile> profiles = [];
        foreach (string profilePath in Directory.EnumerateFiles(_profilesRoot, "*.json", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using FileStream profileStream = File.OpenRead(profilePath);
                DeploymentProfile? profile = await JsonSerializer.DeserializeAsync<DeploymentProfile>(profileStream, JsonOptions, cancellationToken);
                if (profile is not null) profiles.Add(profile);
            }
            catch (JsonException)
            {
                // One damaged profile must not prevent the operator from loading the others.
            }
            catch (IOException)
            {
                // Ignore an individual unreadable profile; callers still receive all healthy profiles.
            }
        }

        return profiles
            .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(profile => profile.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<DeploymentProfile?> LoadByNameAsync(
        string profileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileName)) return null;
        IReadOnlyList<DeploymentProfile> profiles = await LoadAllAsync(cancellationToken);
        return profiles.FirstOrDefault(profile =>
            string.Equals(profile.Name, profileName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task SaveActiveAsync(DeploymentProfile profile, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_profilesRoot);
        string profilePath = GetProfilePath(profile.Id);
        await WriteAtomicAsync(profilePath, profile, cancellationToken);
        await SetActiveProfileAsync(profile.Id, cancellationToken);
    }

    public async Task SetActiveProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        _ = GetProfilePath(profileId); // Validate the canonical profile id before persisting it.
        await WriteAtomicAsync(_settingsPath, new AppSettings(1, profileId), cancellationToken);
    }

    private async Task<DeploymentProfile?> LoadByIdAsync(
        string profileId,
        CancellationToken cancellationToken)
    {
        string profilePath = GetProfilePath(profileId);
        if (!File.Exists(profilePath)) return null;
        await using FileStream profileStream = File.OpenRead(profilePath);
        return await JsonSerializer.DeserializeAsync<DeploymentProfile>(profileStream, JsonOptions, cancellationToken);
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
