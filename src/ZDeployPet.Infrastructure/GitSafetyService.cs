using System.Diagnostics;
using System.Text;

namespace ZDeployPet.Infrastructure;

public sealed record GitSafetyFinding(
    string RelativePath,
    string Reason,
    bool IsTracked);

public sealed record GitSafetyReport(
    bool IsGitRepository,
    bool ManagedIgnoreBlockPresent,
    IReadOnlyList<GitSafetyFinding> Findings,
    string? Error = null)
{
    public bool IsSafe => IsGitRepository && Findings.Count == 0 && string.IsNullOrWhiteSpace(Error);
}

public sealed record GitIgnoreUpdateResult(
    bool Success,
    bool Changed,
    string Message);

public sealed class GitSafetyService
{
    public const string IgnoreBlockStart = "# >>> ZDeployPet local/private data >>>";
    public const string IgnoreBlockEnd = "# <<< ZDeployPet local/private data <<<";

    private const int MaxScannedFileBytes = 2 * 1024 * 1024;

    private static readonly string[] PrivateKeyMarkers =
    [
        "-----BEGIN OPENSSH PRIVATE KEY-----",
        "-----BEGIN PRIVATE KEY-----",
        "-----BEGIN RSA PRIVATE KEY-----",
        "-----BEGIN EC PRIVATE KEY-----",
        "-----BEGIN DSA PRIVATE KEY-----"
    ];

    private static readonly string ManagedIgnoreBlock = string.Join('\n',
    [
        IgnoreBlockStart,
        ".zdeploypet/local/",
        ".zdeploypet/private/",
        "zdeploypet-credentials*",
        "zdeploypet_*_ed25519",
        "zdeploypet_*_rsa",
        IgnoreBlockEnd
    ]);

    public async Task<GitSafetyReport> ScanAsync(
        string projectRoot,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
            return new(false, false, [], "The configured project folder does not exist.");

        string root = Path.GetFullPath(projectRoot);
        CommandResult repositoryCheck = await RunGitAsync(root, ["rev-parse", "--is-inside-work-tree"], cancellationToken);
        if (!repositoryCheck.Started || repositoryCheck.ExitCode != 0 ||
            !repositoryCheck.Output.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return new(false, HasManagedIgnoreBlock(root), [],
                string.IsNullOrWhiteSpace(repositoryCheck.Error)
                    ? "The selected project folder is not a Git working tree."
                    : repositoryCheck.Error.Trim());
        }

        CommandResult trackedResult = await RunGitAsync(root, ["ls-files", "-z"], cancellationToken);
        if (!trackedResult.Started || trackedResult.ExitCode != 0)
            return new(true, HasManagedIgnoreBlock(root), [], "Git could not enumerate tracked files.");

        CommandResult untrackedResult = await RunGitAsync(
            root,
            ["ls-files", "--others", "--exclude-standard", "-z"],
            cancellationToken);
        if (!untrackedResult.Started || untrackedResult.ExitCode != 0)
            return new(true, HasManagedIgnoreBlock(root), [], "Git could not enumerate untracked files.");

        HashSet<string> tracked = ParseNullSeparatedPaths(trackedResult.Output);
        HashSet<string> candidates = new(tracked, StringComparer.Ordinal);
        candidates.UnionWith(ParseNullSeparatedPaths(untrackedResult.Output));

        List<GitSafetyFinding> findings = [];
        foreach (string relativePath in candidates.OrderBy(path => path, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? fullPath = ResolveInsideRoot(root, relativePath);
            if (fullPath is null || !File.Exists(fullPath)) continue;

            FileInfo file = new(fullPath);
            if (file.Length > MaxScannedFileBytes) continue;

            string fileName = Path.GetFileName(relativePath);
            if (LooksLikeZDeployPetPrivateKeyName(fileName))
            {
                findings.Add(new(relativePath,
                    "A ZDeployPet-style private-key filename is inside the repository. Managed deployment private keys must stay under WSL ~/.ssh.",
                    tracked.Contains(relativePath)));
            }

            string? marker = await FindPrivateKeyMarkerAsync(fullPath, cancellationToken);
            if (marker is not null)
            {
                findings.Add(new(relativePath,
                    $"Private-key material marker found: {marker}",
                    tracked.Contains(relativePath)));
            }
        }

        return new(true, HasManagedIgnoreBlock(root), findings);
    }

    public async Task<GitIgnoreUpdateResult> EnsureManagedIgnoreBlockAsync(
        string projectRoot,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
            return new(false, false, "The configured project folder does not exist.");

        string root = Path.GetFullPath(projectRoot);
        CommandResult repositoryCheck = await RunGitAsync(root, ["rev-parse", "--is-inside-work-tree"], cancellationToken);
        if (!repositoryCheck.Started || repositoryCheck.ExitCode != 0 ||
            !repositoryCheck.Output.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return new(false, false, "The selected project folder is not a Git working tree. No file was changed.");
        }

        string gitIgnorePath = Path.Combine(root, ".gitignore");
        string existing = File.Exists(gitIgnorePath)
            ? await File.ReadAllTextAsync(gitIgnorePath, cancellationToken)
            : string.Empty;
        string normalized = existing.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        string updated = UpsertManagedIgnoreBlock(normalized);
        if (string.Equals(normalized, updated, StringComparison.Ordinal))
            return new(true, false, "The ZDeployPet .gitignore safety block is already current.");

        string temporaryPath = gitIgnorePath + ".zdeploypet.tmp";
        await File.WriteAllTextAsync(temporaryPath, updated.Replace("\n", Environment.NewLine, StringComparison.Ordinal), new UTF8Encoding(false), cancellationToken);
        File.Move(temporaryPath, gitIgnorePath, true);
        return new(true, true, "The recommended ZDeployPet safety block was written to .gitignore. Existing project rules were preserved.");
    }

    internal static string UpsertManagedIgnoreBlock(string existing)
    {
        string normalized = existing.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        int start = normalized.IndexOf(IgnoreBlockStart, StringComparison.Ordinal);
        int end = normalized.IndexOf(IgnoreBlockEnd, StringComparison.Ordinal);

        if (start >= 0 && end >= start)
        {
            int endExclusive = end + IgnoreBlockEnd.Length;
            string prefix = normalized[..start].TrimEnd('\n');
            string suffix = normalized[endExclusive..].TrimStart('\n');
            StringBuilder replacement = new();
            if (prefix.Length > 0) replacement.Append(prefix).Append("\n\n");
            replacement.Append(ManagedIgnoreBlock);
            if (suffix.Length > 0) replacement.Append("\n\n").Append(suffix);
            replacement.Append('\n');
            return replacement.ToString();
        }

        StringBuilder appended = new(normalized.TrimEnd('\n'));
        if (appended.Length > 0) appended.Append("\n\n");
        appended.Append(ManagedIgnoreBlock).Append('\n');
        return appended.ToString();
    }

    private static bool HasManagedIgnoreBlock(string root)
    {
        string path = Path.Combine(root, ".gitignore");
        if (!File.Exists(path)) return false;
        string contents = File.ReadAllText(path);
        return contents.Contains(IgnoreBlockStart, StringComparison.Ordinal) &&
               contents.Contains(IgnoreBlockEnd, StringComparison.Ordinal);
    }

    private static HashSet<string> ParseNullSeparatedPaths(string output) =>
        output.Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

    private static string? ResolveInsideRoot(string root, string relativePath)
    {
        string candidate = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        string rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) ? candidate : null;
    }

    private static bool LooksLikeZDeployPetPrivateKeyName(string fileName)
    {
        if (!fileName.StartsWith("zdeploypet_", StringComparison.OrdinalIgnoreCase)) return false;
        if (fileName.EndsWith(".pub", StringComparison.OrdinalIgnoreCase)) return false;
        return fileName.EndsWith("_ed25519", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith("_rsa", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string?> FindPrivateKeyMarkerAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            string contents = await File.ReadAllTextAsync(path, cancellationToken);
            return PrivateKeyMarkers.FirstOrDefault(marker => contents.Contains(marker, StringComparison.Ordinal));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DecoderFallbackException)
        {
            return null;
        }
    }

    private static async Task<CommandResult> RunGitAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        using Process process = new();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (string argument in arguments) process.StartInfo.ArgumentList.Add(argument);

        try
        {
            process.Start();
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return new(true, process.ExitCode, await outputTask, await errorTask);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new(false, -1, string.Empty, exception.Message);
        }
    }

    private sealed record CommandResult(bool Started, int ExitCode, string Output, string Error);
}
