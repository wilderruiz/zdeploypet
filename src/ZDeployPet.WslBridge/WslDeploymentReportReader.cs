using System.Diagnostics;
using System.Globalization;
using System.Text;
using ZDeployPet.Core;

namespace ZDeployPet.WslBridge;

public sealed record WslDeploymentReportReadResult(
    bool Success,
    string? CanonicalReportRoot,
    string? CanonicalReportPath,
    string? Json,
    string? Error)
{
    public static WslDeploymentReportReadResult Failed(string error) =>
        new(false, null, null, null, error);
}

public sealed record WslDeploymentArtifactReadResult(
    bool Success,
    string? CanonicalPath,
    string? Text,
    string? Error)
{
    public static WslDeploymentArtifactReadResult Failed(string error) =>
        new(false, null, null, error);
}

public sealed record WslDeploymentHistoryListResult(
    bool Success,
    string? CanonicalReportRoot,
    IReadOnlyList<string> JsonPaths,
    int CandidateCount,
    bool Truncated,
    string? Error)
{
    public static WslDeploymentHistoryListResult Failed(string error) =>
        new(false, null, Array.Empty<string>(), 0, false, error);
}

/// <summary>
/// Read-only reader for deployment reporter artifacts inside the selected WSL distribution.
/// Paths are resolved with direct WSL utility invocations, containment is enforced in managed code,
/// payload sizes are bounded, text is decoded explicitly as UTF-8, and no operation writes to the report tree.
/// </summary>
public sealed class WslDeploymentReportReader
{
    public const int MaximumReportBytes = 2 * 1024 * 1024;
    public const int MaximumSummaryBytes = 2 * 1024 * 1024;
    public const int MaximumFullLogBytes = 8 * 1024 * 1024;
    public const int MaximumHistoryCandidates = 90;

    public async Task<WslDeploymentReportReadResult> ReadLatestAsync(
        string distribution,
        string reportRoot,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(distribution))
            return WslDeploymentReportReadResult.Failed("A WSL distribution is required.");
        if (string.IsNullOrWhiteSpace(reportRoot) || !reportRoot.StartsWith('/'))
            return WslDeploymentReportReadResult.Failed("A safe absolute WSL report root is required.");

        WslCommandResult rootResult = await RunWslCommandAsync(
            distribution,
            ["realpath", "-e", "--", reportRoot],
            cancellationToken);
        if (!rootResult.Success)
            return WslDeploymentReportReadResult.Failed(
                FriendlyFailure(rootResult, "report root does not exist or cannot be resolved"));

        string canonicalRoot = rootResult.Output.Trim();
        if (!DeploymentReportPathBoundary.TryNormalizeCanonicalPosixPath(canonicalRoot, out canonicalRoot))
            return WslDeploymentReportReadResult.Failed("The configured report root did not resolve to a safe absolute WSL path.");

        string requestedLatest = canonicalRoot.TrimEnd('/') + "/latest.json";
        WslDeploymentArtifactReadResult read = await ReadTextArtifactWithinCanonicalRootAsync(
            distribution,
            canonicalRoot,
            requestedLatest,
            MaximumReportBytes,
            "latest.json",
            cancellationToken);

        if (!read.Success || read.Text is null || read.CanonicalPath is null)
            return WslDeploymentReportReadResult.Failed(read.Error ?? "latest.json could not be read safely.");

        if (string.IsNullOrWhiteSpace(read.Text))
            return WslDeploymentReportReadResult.Failed("latest.json is empty.");

        return new WslDeploymentReportReadResult(
            true,
            canonicalRoot,
            read.CanonicalPath,
            read.Text,
            null);
    }

    public async Task<WslDeploymentHistoryListResult> ListHistoryJsonAsync(
        string distribution,
        string? reportRoot,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(distribution))
            return WslDeploymentHistoryListResult.Failed("A WSL distribution is required.");
        if (string.IsNullOrWhiteSpace(reportRoot) || !reportRoot.StartsWith('/'))
            return WslDeploymentHistoryListResult.Failed("A safe absolute WSL report root is required.");

        WslCommandResult rootResult = await RunWslCommandAsync(
            distribution,
            ["realpath", "-e", "--", reportRoot],
            cancellationToken);
        if (!rootResult.Success)
            return WslDeploymentHistoryListResult.Failed(
                FriendlyFailure(rootResult, "report root does not exist or cannot be resolved"));

        string canonicalRoot = rootResult.Output.Trim();
        if (!DeploymentReportPathBoundary.TryNormalizeCanonicalPosixPath(canonicalRoot, out canonicalRoot))
            return WslDeploymentHistoryListResult.Failed("The configured report root did not resolve to a safe absolute WSL path.");

        WslCommandResult findResult = await RunWslCommandAsync(
            distribution,
            ["find", canonicalRoot, "-mindepth", "2", "-maxdepth", "2", "-type", "f", "-name", "*.json", "-print0"],
            cancellationToken);
        if (!findResult.Success)
            return WslDeploymentHistoryListResult.Failed(
                FriendlyFailure(findResult, "deployment history could not be enumerated"));

        string[] discovered = findResult.Output
            .Split('\0', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        List<string> safe = [];
        foreach (string path in discovered)
        {
            if (!DeploymentReportPathBoundary.IsWithinCanonicalRoot(canonicalRoot, path))
                continue;
            if (path.EndsWith("/latest.json", StringComparison.Ordinal))
                continue;
            safe.Add(path);
        }

        safe.Sort((left, right) => StringComparer.Ordinal.Compare(right, left));
        int candidateCount = safe.Count;
        bool truncated = candidateCount > MaximumHistoryCandidates;
        if (truncated)
            safe = safe.Take(MaximumHistoryCandidates).ToList();

        return new WslDeploymentHistoryListResult(
            true,
            canonicalRoot,
            safe,
            candidateCount,
            truncated,
            null);
    }

    public async Task<WslDeploymentArtifactReadResult> ReadTextArtifactAsync(
        string distribution,
        string reportRoot,
        string artifactPath,
        int maximumBytes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(distribution))
            return WslDeploymentArtifactReadResult.Failed("A WSL distribution is required.");
        if (string.IsNullOrWhiteSpace(reportRoot) || !reportRoot.StartsWith('/'))
            return WslDeploymentArtifactReadResult.Failed("A safe absolute WSL report root is required.");
        if (string.IsNullOrWhiteSpace(artifactPath) || !artifactPath.StartsWith('/'))
            return WslDeploymentArtifactReadResult.Failed("A safe absolute WSL artifact path is required.");
        if (maximumBytes <= 0)
            return WslDeploymentArtifactReadResult.Failed("The artifact size limit is invalid.");

        WslCommandResult rootResult = await RunWslCommandAsync(
            distribution,
            ["realpath", "-e", "--", reportRoot],
            cancellationToken);
        if (!rootResult.Success)
            return WslDeploymentArtifactReadResult.Failed(
                FriendlyFailure(rootResult, "report root does not exist or cannot be resolved"));

        string canonicalRoot = rootResult.Output.Trim();
        if (!DeploymentReportPathBoundary.TryNormalizeCanonicalPosixPath(canonicalRoot, out canonicalRoot))
            return WslDeploymentArtifactReadResult.Failed("The configured report root did not resolve to a safe absolute WSL path.");

        return await ReadTextArtifactWithinCanonicalRootAsync(
            distribution,
            canonicalRoot,
            artifactPath,
            maximumBytes,
            "artifact",
            cancellationToken);
    }

    private static async Task<WslDeploymentArtifactReadResult> ReadTextArtifactWithinCanonicalRootAsync(
        string distribution,
        string canonicalRoot,
        string requestedPath,
        int maximumBytes,
        string displayName,
        CancellationToken cancellationToken)
    {
        WslCommandResult candidateResult = await RunWslCommandAsync(
            distribution,
            ["realpath", "-e", "--", requestedPath],
            cancellationToken);
        if (!candidateResult.Success)
            return WslDeploymentArtifactReadResult.Failed(
                FriendlyFailure(candidateResult, $"{displayName} does not exist or cannot be resolved"));

        string canonicalPath = candidateResult.Output.Trim();
        if (!DeploymentReportPathBoundary.IsWithinCanonicalRoot(canonicalRoot, canonicalPath))
            return WslDeploymentArtifactReadResult.Failed($"{displayName} resolves outside the configured report root.");

        WslCommandResult typeResult = await RunWslCommandAsync(
            distribution,
            ["test", "-f", canonicalPath],
            cancellationToken);
        if (!typeResult.Success)
            return WslDeploymentArtifactReadResult.Failed($"{displayName} is not a regular file.");

        WslCommandResult sizeResult = await RunWslCommandAsync(
            distribution,
            ["stat", "-c", "%s", "--", canonicalPath],
            cancellationToken);
        if (!sizeResult.Success ||
            !long.TryParse(sizeResult.Output.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out long size))
            return WslDeploymentArtifactReadResult.Failed($"Cannot read {displayName} size safely.");

        if (size < 0 || size > maximumBytes)
            return WslDeploymentArtifactReadResult.Failed($"{displayName} exceeds the maximum supported size of {maximumBytes:N0} bytes.");

        WslCommandResult readResult = await RunWslCommandAsync(
            distribution,
            ["cat", "--", canonicalPath],
            cancellationToken);
        if (!readResult.Success)
            return WslDeploymentArtifactReadResult.Failed(
                FriendlyFailure(readResult, $"{displayName} could not be read"));

        return new WslDeploymentArtifactReadResult(
            true,
            canonicalPath,
            readResult.Output,
            null);
    }

    private static async Task<WslCommandResult> RunWslCommandAsync(
        string distribution,
        IReadOnlyList<string> command,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "wsl.exe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--distribution");
        startInfo.ArgumentList.Add(distribution);
        startInfo.ArgumentList.Add("--exec");
        foreach (string argument in command)
            startInfo.ArgumentList.Add(argument);

        using Process process = new() { StartInfo = startInfo };
        try
        {
            process.Start();
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return new WslCommandResult(
                process.ExitCode,
                await outputTask,
                await errorTask);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new WslCommandResult(-1, string.Empty, exception.Message);
        }
    }

    private static string FriendlyFailure(WslCommandResult result, string fallback)
    {
        string error = result.Error.Trim();
        return string.IsNullOrWhiteSpace(error)
            ? $"{fallback} (exit code {result.ExitCode})."
            : error;
    }

    private sealed record WslCommandResult(int ExitCode, string Output, string Error)
    {
        public bool Success => ExitCode == 0;
    }
}
