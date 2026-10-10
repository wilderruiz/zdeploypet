using System.Diagnostics;
using System.Globalization;
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

/// <summary>
/// Read-only reader for the deployment reporter's latest.json inside the selected WSL distribution.
/// The reader resolves paths with direct WSL utility invocations rather than an inline shell script,
/// rejects path escapes in managed code, imposes a bounded payload size, and never writes to the report tree.
/// </summary>
public sealed class WslDeploymentReportReader
{
    public const int MaximumReportBytes = 2 * 1024 * 1024;

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
        WslCommandResult candidateResult = await RunWslCommandAsync(
            distribution,
            ["realpath", "-e", "--", requestedLatest],
            cancellationToken);
        if (!candidateResult.Success)
            return WslDeploymentReportReadResult.Failed(
                FriendlyFailure(candidateResult, "latest.json does not exist or cannot be resolved"));

        string canonicalPath = candidateResult.Output.Trim();
        if (!DeploymentReportPathBoundary.IsWithinCanonicalRoot(canonicalRoot, canonicalPath))
            return WslDeploymentReportReadResult.Failed("latest.json resolves outside the configured report root.");

        WslCommandResult typeResult = await RunWslCommandAsync(
            distribution,
            ["test", "-f", canonicalPath],
            cancellationToken);
        if (!typeResult.Success)
            return WslDeploymentReportReadResult.Failed("latest.json is not a regular file.");

        WslCommandResult sizeResult = await RunWslCommandAsync(
            distribution,
            ["stat", "-c", "%s", "--", canonicalPath],
            cancellationToken);
        if (!sizeResult.Success ||
            !long.TryParse(sizeResult.Output.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out long size))
            return WslDeploymentReportReadResult.Failed("Cannot read latest.json size safely.");

        if (size < 0 || size > MaximumReportBytes)
            return WslDeploymentReportReadResult.Failed("latest.json exceeds the maximum supported size.");

        WslCommandResult readResult = await RunWslCommandAsync(
            distribution,
            ["cat", "--", canonicalPath],
            cancellationToken);
        if (!readResult.Success)
            return WslDeploymentReportReadResult.Failed(
                FriendlyFailure(readResult, "latest.json could not be read"));

        string json = readResult.Output;
        if (string.IsNullOrWhiteSpace(json))
            return WslDeploymentReportReadResult.Failed("latest.json is empty.");

        return new WslDeploymentReportReadResult(
            true,
            canonicalRoot,
            canonicalPath,
            json,
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
