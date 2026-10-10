using System.Diagnostics;

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
/// The reader canonicalizes both the configured root and latest.json inside WSL before reading,
/// rejects symlink/path escapes, imposes a bounded payload size, and never writes to the report tree.
/// </summary>
public sealed class WslDeploymentReportReader
{
    public const int MaximumReportBytes = 2 * 1024 * 1024;

    private const string ReadLatestScript = """
        root="$(realpath -e -- "$1")" || { printf '%s\n' 'report root does not exist' >&2; exit 10; }
        test -d "$root" || { printf '%s\n' 'report root is not a directory' >&2; exit 11; }

        candidate="$(realpath -e -- "$root/latest.json")" || { printf '%s\n' 'latest.json does not exist' >&2; exit 20; }
        test -f "$candidate" || { printf '%s\n' 'latest.json is not a regular file' >&2; exit 21; }

        relative="$(realpath --relative-to="$root" -- "$candidate")" || { printf '%s\n' 'cannot verify latest.json containment' >&2; exit 22; }
        if test "$relative" = ".." || printf '%s\n' "$relative" | grep -q '^\.\./'; then
            printf '%s\n' 'latest.json resolves outside the configured report root' >&2
            exit 22
        fi

        size="$(stat -c '%s' -- "$candidate")" || { printf '%s\n' 'cannot read latest.json size' >&2; exit 23; }
        test "$size" -le "$2" || { printf '%s\n' 'latest.json exceeds the maximum supported size' >&2; exit 24; }

        printf '%s\0%s\0' "$root" "$candidate"
        cat -- "$candidate"
        """;

    public async Task<WslDeploymentReportReadResult> ReadLatestAsync(
        string distribution,
        string reportRoot,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(distribution))
            return WslDeploymentReportReadResult.Failed("A WSL distribution is required.");
        if (string.IsNullOrWhiteSpace(reportRoot) || !reportRoot.StartsWith('/'))
            return WslDeploymentReportReadResult.Failed("A safe absolute WSL report root is required.");

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
        startInfo.ArgumentList.Add("sh");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(ReadLatestScript);
        startInfo.ArgumentList.Add("zdeploypet-report-reader");
        startInfo.ArgumentList.Add(reportRoot);
        startInfo.ArgumentList.Add(MaximumReportBytes.ToString(System.Globalization.CultureInfo.InvariantCulture));

        (int exitCode, string output, string error) = await RunAsync(startInfo, cancellationToken);
        if (exitCode != 0)
        {
            string message = string.IsNullOrWhiteSpace(error)
                ? $"WSL report read failed with exit code {exitCode}."
                : error.Trim();
            return WslDeploymentReportReadResult.Failed(message);
        }

        int firstSeparator = output.IndexOf('\0');
        int secondSeparator = firstSeparator < 0 ? -1 : output.IndexOf('\0', firstSeparator + 1);
        if (firstSeparator <= 0 || secondSeparator <= firstSeparator + 1)
            return WslDeploymentReportReadResult.Failed("WSL report reader returned an invalid framing header.");

        string canonicalRoot = output[..firstSeparator];
        string canonicalPath = output[(firstSeparator + 1)..secondSeparator];
        string json = output[(secondSeparator + 1)..];

        if (string.IsNullOrWhiteSpace(json))
            return WslDeploymentReportReadResult.Failed("latest.json is empty.");

        return new WslDeploymentReportReadResult(
            true,
            canonicalRoot,
            canonicalPath,
            json,
            null);
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken)
    {
        using Process process = new() { StartInfo = startInfo };
        try
        {
            process.Start();
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return (process.ExitCode, await outputTask, await errorTask);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return (-1, string.Empty, exception.Message);
        }
    }
}
