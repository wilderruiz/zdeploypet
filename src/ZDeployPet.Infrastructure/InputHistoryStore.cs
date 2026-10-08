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

public sealed class InputHistoryStore
{
    private const int MaximumSuggestionsPerField = 12;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    private readonly string _historyPath;

    public InputHistoryStore(string? root = null)
    {
        string storageRoot = root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Zomniverse", "ZDeployPet");
        _historyPath = Path.Combine(storageRoot, "input-history.json");
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

        string? directory = Path.GetDirectoryName(_historyPath);
        if (directory is null) throw new InvalidOperationException("Input history path has no parent directory.");
        Directory.CreateDirectory(directory);
        string temporaryPath = _historyPath + ".tmp";
        await using (FileStream stream = new(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, updated, JsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        File.Move(temporaryPath, _historyPath, true);
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
}
