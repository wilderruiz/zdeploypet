using System.Diagnostics;
using System.Globalization;
using System.Text;
using ZDeployPet.Core;

namespace ZDeployPet.WslBridge;

public sealed record WslDryRunExecutionResult(
    bool Started,
    int ExitCode,
    string StandardOutput,
    string StandardError,
    string? Error = null);

public sealed class WslDryRunExecutor
{
    private const int MaximumCapturedCharacters = 4 * 1024 * 1024;

    public async Task<WslDryRunExecutionResult> ExecuteAsync(
        string distribution,
        string wslProjectPath,
        string scriptRelativePath,
        WslSshAgentRuntime runtime,
        DryRunPromptPlan promptPlan,
        CancellationToken cancellationToken = default)
    {
        string? validationError = ValidateInvocation(
            distribution,
            wslProjectPath,
            scriptRelativePath,
            runtime,
            promptPlan);
        if (validationError is not null)
            return new(false, -1, string.Empty, string.Empty, validationError);

        IReadOnlyList<string> inputLines;
        try
        {
            inputLines = promptPlan.BuildStandardInputLines();
        }
        catch (InvalidDataException exception)
        {
            return new(false, -1, string.Empty, string.Empty, exception.Message);
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = "wsl.exe",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        startInfo.ArgumentList.Add("--distribution");
        startInfo.ArgumentList.Add(distribution.Trim());
        startInfo.ArgumentList.Add("--cd");
        startInfo.ArgumentList.Add(wslProjectPath.Trim());
        startInfo.ArgumentList.Add("--exec");
        startInfo.ArgumentList.Add("/usr/bin/env");
        startInfo.ArgumentList.Add($"SSH_AUTH_SOCK={runtime.SocketPath}");
        startInfo.ArgumentList.Add($"SSH_AGENT_PID={runtime.AgentPid.ToString(CultureInfo.InvariantCulture)}");
        startInfo.ArgumentList.Add("/usr/bin/bash");
        startInfo.ArgumentList.Add(scriptRelativePath.Trim());

        try
        {
            using Process process = new() { StartInfo = startInfo };
            if (!process.Start())
                return new(false, -1, string.Empty, string.Empty, "Windows could not start the WSL dry-run process.");

            // StreamWriter defaults to the Windows newline (CRLF). Bash `read -r` strips
            // the LF delimiter but preserves the CR, turning a safe numeric choice such as
            // `3` into `3\r` and causing Millenova's exact case match to reject it. The WSL
            // script is a Unix process, so every synthetic prompt answer must be LF-only.
            process.StandardInput.NewLine = "\n";
            foreach (string line in inputLines)
                await process.StandardInput.WriteLineAsync(line.AsMemory(), cancellationToken);
            process.StandardInput.Close();

            Task<string> stdoutTask = ReadBoundedAsync(process.StandardOutput, cancellationToken);
            Task<string> stderrTask = ReadBoundedAsync(process.StandardError, cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best-effort cancellation cleanup only.
                }
                throw;
            }

            string stdout = await stdoutTask;
            string stderr = await stderrTask;
            return new(true, process.ExitCode, stdout, stderr);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return new(false, -1, string.Empty, string.Empty, exception.Message);
        }
    }

    private static string? ValidateInvocation(
        string distribution,
        string wslProjectPath,
        string scriptRelativePath,
        WslSshAgentRuntime runtime,
        DryRunPromptPlan promptPlan)
    {
        if (string.IsNullOrWhiteSpace(distribution))
            return "A WSL distribution is required for dry-run execution.";
        if (!string.Equals(distribution.Trim(), runtime.Distribution.Trim(), StringComparison.Ordinal))
            return "The dry-run WSL distribution does not match the app-owned deployment-access session.";
        if (runtime.AgentPid <= 0 || string.IsNullOrWhiteSpace(runtime.SocketPath) || !runtime.SocketPath.StartsWith('/'))
            return "The app-owned deployment-access session runtime is invalid.";
        if (string.IsNullOrWhiteSpace(wslProjectPath) || !wslProjectPath.StartsWith('/'))
            return "A verified absolute WSL project path is required.";
        if (!IsSafeRelativeScriptPath(scriptRelativePath))
            return "The deployment script must be a safe project-relative path.";

        try
        {
            _ = promptPlan.BuildStandardInputLines();
        }
        catch (InvalidDataException exception)
        {
            return exception.Message;
        }

        return null;
    }

    internal static bool IsSafeRelativeScriptPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.StartsWith('/') || path.Contains('\0'))
            return false;

        string normalized = path.Replace('\\', '/');
        string[] segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
            return false;

        return true;
    }

    private static async Task<string> ReadBoundedAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        char[] buffer = new char[4096];
        StringBuilder text = new();
        while (true)
        {
            int read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0) break;

            int remaining = MaximumCapturedCharacters - text.Length;
            if (remaining <= 0) continue;
            text.Append(buffer, 0, Math.Min(read, remaining));
        }

        if (text.Length >= MaximumCapturedCharacters)
            text.Append("\n[ZDeployPet output capture truncated at 4 MiB]\n");
        return text.ToString();
    }
}
