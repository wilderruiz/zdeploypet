using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZDeployPet.Core;

namespace ZDeployPet.Infrastructure;

public sealed record SetupProfileSnapshot(
    int SchemaVersion,
    SetupDraft Draft,
    string VerifiedWslPath);

public sealed class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _root;
    private readonly string _profilesRoot;
    private readonly string _setupProfilesRoot;
    private readonly string _settingsPath;

    public ProfileStore(string? root = null)
    {
        _root = root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Zomniverse", "ZDeployPet");
        _profilesRoot = Path.Combine(_root, "profiles");
        _setupProfilesRoot = Path.Combine(_root, "setup-profiles");
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

    public async Task SaveSetupSnapshotAsync(
        SetupDraft draft,
        string? verifiedWslPath,
        CancellationToken cancellationToken = default)
    {
        string profileName = draft.ProfileName.Trim();
        if (profileName.Length == 0)
            throw new InvalidDataException("Profile name is required before this setup can be saved as JSON.");

        Directory.CreateDirectory(_setupProfilesRoot);
        SetupProfileSnapshot snapshot = new(1, draft, verifiedWslPath?.Trim() ?? string.Empty);
        await WriteAtomicAsync(GetSetupProfilePath(profileName), snapshot, cancellationToken);
    }

    public async Task<SetupProfileSnapshot?> LoadSetupSnapshotByNameAsync(
        string profileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileName)) return null;
        string path = GetSetupProfilePath(profileName.Trim());
        if (!File.Exists(path)) return null;

        try
        {
            await using FileStream stream = File.OpenRead(path);
            SetupProfileSnapshot? snapshot = await JsonSerializer.DeserializeAsync<SetupProfileSnapshot>(stream, JsonOptions, cancellationToken);
            if (snapshot is null || snapshot.SchemaVersion != 1) return null;
            if (!string.Equals(snapshot.Draft.ProfileName.Trim(), profileName.Trim(), StringComparison.OrdinalIgnoreCase)) return null;
            return snapshot;
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

    public async Task<IReadOnlyList<string>> LoadSetupProfileNamesAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_setupProfilesRoot)) return [];

        List<string> names = [];
        foreach (string path in Directory.EnumerateFiles(_setupProfilesRoot, "*.json", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using FileStream stream = File.OpenRead(path);
                SetupProfileSnapshot? snapshot = await JsonSerializer.DeserializeAsync<SetupProfileSnapshot>(stream, JsonOptions, cancellationToken);
                if (snapshot is not null && snapshot.SchemaVersion == 1 && !string.IsNullOrWhiteSpace(snapshot.Draft.ProfileName))
                    names.Add(snapshot.Draft.ProfileName.Trim());
            }
            catch (JsonException)
            {
                // A damaged setup snapshot must not hide other saved profiles.
            }
            catch (IOException)
            {
                // Ignore an unreadable snapshot and continue.
            }
        }

        return names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task SaveActiveAsync(DeploymentProfile profile, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_profilesRoot);
        string profilePath = GetProfilePath(profile.Id);

        DeploymentProfile? existing = File.Exists(profilePath)
            ? await LoadByIdAsync(profile.Id, cancellationToken)
            : null;
        if (existing is not null)
            PreserveStableTargetIds(profile, existing);

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

    private static void PreserveStableTargetIds(DeploymentProfile incoming, DeploymentProfile existing)
    {
        if (incoming.Targets is not IList<DeploymentTarget> mutableTargets ||
            incoming.Destinations is not IList<DeploymentDestination> mutableDestinations)
            return;

        Dictionary<string, string> remappedTargetIds = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < mutableTargets.Count; index++)
        {
            DeploymentTarget candidate = mutableTargets[index];
            DeploymentTarget[] endpointMatches = existing.Targets
                .Where(previous =>
                    string.Equals(previous.Host, candidate.Host, StringComparison.OrdinalIgnoreCase) &&
                    previous.Port == candidate.Port &&
                    string.Equals(previous.User, candidate.User, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            // Preserve identity only when the endpoint match is unambiguous. A changed
            // host/port/user is deliberately treated as a new target so existing host
            // trust cannot silently follow a materially different SSH endpoint.
            if (endpointMatches.Length != 1)
                continue;

            DeploymentTarget previousTarget = endpointMatches[0];
            if (string.Equals(previousTarget.Id, candidate.Id, StringComparison.OrdinalIgnoreCase))
                continue;

            remappedTargetIds[candidate.Id] = previousTarget.Id;
            mutableTargets[index] = candidate with { Id = previousTarget.Id };
        }

        if (remappedTargetIds.Count == 0)
            return;

        for (int index = 0; index < mutableDestinations.Count; index++)
        {
            DeploymentDestination destination = mutableDestinations[index];
            if (remappedTargetIds.TryGetValue(destination.TargetId, out string? stableTargetId))
                mutableDestinations[index] = destination with { TargetId = stableTargetId };
        }
    }

    private string GetProfilePath(string profileId)
    {
        if (!Guid.TryParse(profileId, out Guid parsed) || parsed.ToString("D") != profileId.ToLowerInvariant())
            throw new InvalidDataException("Profile ID must be a canonical GUID.");
        return Path.Combine(_profilesRoot, profileId + ".json");
    }

    private string GetSetupProfilePath(string profileName)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(profileName.Trim().ToUpperInvariant()));
        string fileName = Convert.ToHexString(digest).ToLowerInvariant() + ".json";
        return Path.Combine(_setupProfilesRoot, fileName);
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
