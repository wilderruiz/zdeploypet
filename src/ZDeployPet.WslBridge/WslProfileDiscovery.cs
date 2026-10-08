using System.Diagnostics;

namespace ZDeployPet.WslBridge;

public sealed record WslPathResolution(bool Success, string? WslPath, string? Error);
public sealed record WslScriptValidation(bool Success, string? CanonicalScriptPath, string? Error);

public sealed class WslProfileDiscovery
{
    public async Task<IReadOnlyList<string>> ListDistributionsAsync(CancellationToken cancellationToken = default)
    {
        ProcessStartInfo startInfo = CreateStartInfo();
        startInfo.ArgumentList.Add("--list");
        startInfo.ArgumentList.Add("--quiet");
        (int exitCode, string output, _) = await RunAsync(startInfo, cancellationToken);
        if (exitCode != 0) return [];

        return output.Replace("\0", string.Empty, StringComparison.Ordinal)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<WslPathResolution> ResolveAndVerifyProjectPathAsync(
        string distribution,
        string windowsPath,
        CancellationToken cancellationToken = default)
    {
        ProcessStartInfo translate = CreateStartInfo();
        translate.ArgumentList.Add("--distribution");
        translate.ArgumentList.Add(distribution);
        translate.ArgumentList.Add("--exec");
        translate.ArgumentList.Add("wslpath");
        translate.ArgumentList.Add("-a");
        translate.ArgumentList.Add("-u");
        translate.ArgumentList.Add(windowsPath);
        (int translateExit, string translated, string translateError) = await RunAsync(translate, cancellationToken);
        string wslPath = translated.Trim();
        if (translateExit != 0 || string.IsNullOrWhiteSpace(wslPath))
            return new WslPathResolution(false, null, translateError.Trim());

        const string verifyScript = "test -d \"$1\" && realpath \"$1\"";
        ProcessStartInfo verify = CreateStartInfo();
        verify.ArgumentList.Add("--distribution");
        verify.ArgumentList.Add(distribution);
        verify.ArgumentList.Add("--exec");
        verify.ArgumentList.Add("sh");
        verify.ArgumentList.Add("-c");
        verify.ArgumentList.Add(verifyScript);
        verify.ArgumentList.Add("zdeploypet-profile-check");
        verify.ArgumentList.Add(wslPath);
        (int verifyExit, string canonical, string verifyError) = await RunAsync(verify, cancellationToken);
        return verifyExit == 0
            ? new WslPathResolution(true, canonical.Trim(), null)
            : new WslPathResolution(false, wslPath, verifyError.Trim().Length > 0 ? verifyError.Trim() : "WSL cannot access the selected project folder.");
    }

    public async Task<WslScriptValidation> VerifyScriptContainmentAsync(
        string distribution,
        string wslProjectPath,
        string scriptRelativePath,
        CancellationToken cancellationToken = default)
    {
        const string script = """
            root="$(realpath "$1")" || exit 10
            candidate="$(realpath -e "$root/$2")" || exit 11
            test -f "$candidate" || exit 12
            case "$candidate" in "$root"/*) printf '%s\n' "$candidate" ;; *) exit 13 ;; esac
            """;
        ProcessStartInfo verify = CreateStartInfo();
        verify.ArgumentList.Add("--distribution");
        verify.ArgumentList.Add(distribution);
        verify.ArgumentList.Add("--exec");
        verify.ArgumentList.Add("sh");
        verify.ArgumentList.Add("-c");
        verify.ArgumentList.Add(script);
        verify.ArgumentList.Add("zdeploypet-script-check");
        verify.ArgumentList.Add(wslProjectPath);
        verify.ArgumentList.Add(scriptRelativePath);
        (int exitCode, string output, string error) = await RunAsync(verify, cancellationToken);
        if (exitCode == 0)
            return new WslScriptValidation(true, output.Trim(), null);

        string message = exitCode == 13
            ? "The deployment script resolves outside the approved project root."
            : "WSL could not verify the selected deployment script as a regular file inside the project.";
        if (!string.IsNullOrWhiteSpace(error)) message += " " + error.Trim();
        return new WslScriptValidation(false, null, message);
    }

    private static ProcessStartInfo CreateStartInfo() => new()
    {
        FileName = "wsl.exe",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    private static async Task<(int ExitCode, string Output, string Error)> RunAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken)
    {
        using Process process = new() { StartInfo = startInfo };
        try
        {
            process.Start();
            Task<string> output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> error = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return (process.ExitCode, await output, await error);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return (-1, string.Empty, exception.Message);
        }
    }
}
