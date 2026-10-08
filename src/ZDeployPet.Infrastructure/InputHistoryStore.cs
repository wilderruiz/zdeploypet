using System.Text.Json;
using ZDeployPet.Core;

namespace ZDeployPet.Infrastructure;

public sealed record InputHistory(
    IReadOnlyList<string> ProfileNames,
    IReadOnlyList<string> ProjectPaths,
    IReadOnlyList<string> ScriptPaths,
    IReadOnlyList<string> ReportRoots,
    IReadOnlyList<string> ConfirmationPhrases,
    IReadOnlyList<string> TargetLabels,
    IReadOnlyList<string> Hosts,
    IReadOnlyList<string> Ports,
    IReadOnlyList<string> Users,
    IReadOnlyList<string> DestinationLabels,
    IReadOnlyList<string> RemotePaths)
{
    public static InputHistory Empty { get; } = new([], [], [], [], [], [], [], [], [], [], []);
}

public sealed record SetupTargetDraft(string Label, string Host, int Port, string User);
public sealed record SetupDestinationDraft(string TargetLabel, string Label, string RemotePath);
public sealed record SetupDraft(
    string ProfileName,
    string ProjectPath,
    string WslDistribution,
    string ScriptPath,
    string ReportRoot,
    string ConfirmationPhrase,
    IReadOnlyList<SetupTargetDraft> Targets,
    IReadOnlyList<SetupDestinationDraft> Destinations);

public sealed class InputHistoryStore
{
    private const int MaximumSuggestionsPerField = 12;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    private readonly string _historyPath;
    private readonly string _draftPath;

    public InputHistoryStore(string? root = null)
    {
        string storageRoot = root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Zomniverse", "ZDeployPet");
        _historyPath = Path.Combine(storageRoot, "input-history.json");
        _draftPath = Path.Combine(storageRoot, "setup-draft.json");
    }

    public async Task<InputHistory> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_historyPath)) return InputHistory.Empty;
        await using FileStream stream = File.OpenRead(_historyPath);
        return await JsonSerializer.DeserializeAsync<InputHistory>(stream, JsonOptions, cancellationToken)
            ?? InputHistory.Empty;
    }

    public async Task RememberAsync(DeploymentProfile profile, CancellationToken cancellationToken = default)
    {
        InputHistory current = await LoadAsync(cancellationToken);
        InputHistory updated = new(
            Merge(current.ProfileNames, [profile.Name]),
            Merge(current.ProjectPaths, [profile.WindowsProjectPath]),
            Merge(current.ScriptPaths, [profile.ScriptRelativePath]),
            Merge(current.ReportRoots, [profile.ReportRoot ?? string.Empty]),
            Merge(current.ConfirmationPhrases, [profile.LiveConfirmationPhrase]),
            Merge(current.TargetLabels, profile.Targets.Select(target => target.Label)),
            Merge(current.Hosts, profile.Targets.Select(target => target.Host)),
            Merge(current.Ports, profile.Targets.Select(target => target.Port.ToString())),
            Merge(current.Users, profile.Targets.Select(target => target.User)),
            Merge(current.DestinationLabels, profile.Destinations.Select(destination => destination.Label)),
            Merge(current.RemotePaths, profile.Destinations.Select(destination => destination.RemotePath)));

        await WriteAtomicAsync(_historyPath, updated, cancellationToken);
    }

    public async Task<SetupDraft?> LoadDraftAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_draftPath)) return null;
        await using FileStream stream = File.OpenRead(_draftPath);
        return await JsonSerializer.DeserializeAsync<SetupDraft>(stream, JsonOptions, cancellationToken);
    }

    public async Task SaveDraftAsync(SetupDraft draft, CancellationToken cancellationToken = default)
    {
        await WriteAtomicAsync(_draftPath, draft, cancellationToken);
    }

    public void SaveDraft(SetupDraft draft)
    {
        WriteAtomic(_draftPath, draft);
    }

    public async Task RememberDraftAsync(SetupDraft draft, CancellationToken cancellationToken = default)
    {
        InputHistory current = await LoadAsync(cancellationToken);
        InputHistory updated = new(
            Merge(current.ProfileNames, [draft.ProfileName]),
            Merge(current.ProjectPaths, [draft.ProjectPath]),
            Merge(current.ScriptPaths, [draft.ScriptPath]),
            Merge(current.ReportRoots, [draft.ReportRoot]),
            Merge(current.ConfirmationPhrases, [draft.ConfirmationPhrase]),
            Merge(current.TargetLabels, draft.Targets.Select(target => target.Label)),
            Merge(current.Hosts, draft.Targets.Select(target => target.Host)),
            Merge(current.Ports, draft.Targets.Select(target => target.Port.ToString())),
            Merge(current.Users, draft.Targets.Select(target => target.User)),
            Merge(current.DestinationLabels, draft.Destinations.Select(destination => destination.Label)),
            Merge(current.RemotePaths, draft.Destinations.Select(destination => destination.RemotePath)));
        await WriteAtomicAsync(_historyPath, updated, cancellationToken);
    }

    public Task ClearAsync()
    {
        if (File.Exists(_historyPath)) File.Delete(_historyPath);
        return Task.CompletedTask;
    }

    private static IReadOnlyList<string> Merge(IEnumerable<string> existing, IEnumerable<string> newest) =>
        newest.Concat(existing)
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaximumSuggestionsPerField)
            .ToArray();

    private static async Task WriteAtomicAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(path);
        if (directory is null) throw new InvalidOperationException("Local data path has no parent directory.");
        Directory.CreateDirectory(directory);
        string temporaryPath = path + ".tmp." + Guid.NewGuid().ToString("N");
        try
        {
            await using (FileStream stream = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static void WriteAtomic<T>(string path, T value)
    {
        string? directory = Path.GetDirectoryName(path);
        if (directory is null) throw new InvalidOperationException("Local data path has no parent directory.");
        Directory.CreateDirectory(directory);
        string temporaryPath = path + ".tmp." + Guid.NewGuid().ToString("N");
        try
        {
            using (FileStream stream = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, value, JsonOptions);
                stream.Flush(true);
            }
            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
