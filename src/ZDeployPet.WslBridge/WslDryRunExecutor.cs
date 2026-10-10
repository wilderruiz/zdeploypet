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
    private const int MaximumLiveLineCharacters = 16 * 1024;
    private const int SafetyViolationExitCode = 97;
    private static readonly TimeSpan MaximumExecutionTime = TimeSpan.FromMinutes(20);

    // The approved Millenova script uses `read -r -p ...`. Bash suppresses -p prompts
    // when stdin is not a terminal, so a redirected-process adapter that waits to SEE
    // those prompts can deadlock forever. This fixed wrapper never pre-buffers answers
    // on stdin. Instead it overrides only the exact three interactive read calls used
    // by the approved deployment script, injects the allowlisted target/release values,
    // and structurally forces deployment mode 1 (dry). Any live confirmation prompt is
    // rejected. All other `read` calls delegate untouched to Bash's builtin.
    private const string DryRunReadAdapter = """
set -euo pipefail
script_path="$1"
target_choice="$2"
release_choice="$3"

read() {
    if [[ "$#" -eq 4 && "$1" == "-r" && "$2" == "-p" ]]; then
        case "$3" in
            "Choose deployment destination [1/2/3]: ")
                printf '%s%s\n' "$3" "$target_choice" >&2
                printf -v "$4" '%s' "$target_choice"
                return 0
                ;;
            "Choose release type [1/2/3]: ")
                if [[ -z "$release_choice" ]]; then
                    echo "FAIL [ZDeployPet safety] Release choice was requested but no allowlisted release choice exists." >&2
                    return 97
                fi
                printf '%s%s\n' "$3" "$release_choice" >&2
                printf -v "$4" '%s' "$release_choice"
                return 0
                ;;
            "Choose [1/2]: ")
                printf '%s1\n' "$3" >&2
                printf -v "$4" '%s' "1"
                return 0
                ;;
            "Type DEPLOY MILLENOVA to continue: ")
                echo "FAIL [ZDeployPet safety] Live confirmation was requested during a PDA-5 dry run." >&2
                return 97
                ;;
        esac
    fi

    builtin read "$@"
}

source "$script_path"
""";

    public async Task<WslDryRunExecutionResult> ExecuteAsync(
        string distribution,
        string wslProjectPath,
        string scriptRelativePath,
        WslSshAgentRuntime runtime,
        DryRunPromptPlan promptPlan,
        Action<string, bool>? outputLine = null,
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

        DryRunPromptProtocol protocol;
        try
        {
            protocol = new DryRunPromptProtocol(promptPlan);
        }
        catch (InvalidDataException exception)
        {
            return new(false, -1, string.Empty, string.Empty, exception.Message);
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = "wsl.exe",
            RedirectStandardInput = false,
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
        startInfo.ArgumentList.Add("-c");
        // Raw string literals follow the source file's physical line endings. Normalize
        // the adapter explicitly because bash -c interprets a stray CR in `pipefail\r`
        // as part of the option name when the project is checked out with CRLF on Windows.
        startInfo.ArgumentList.Add(NormalizeBashScript(DryRunReadAdapter));
        startInfo.ArgumentList.Add("zdeploypet-dry-run");
        startInfo.ArgumentList.Add(scriptRelativePath.Trim());
        startInfo.ArgumentList.Add(promptPlan.TargetChoice.ToString(CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add(promptPlan.ReleaseChoice?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

        try
        {
            using Process process = new() { StartInfo = startInfo };
            if (!process.Start())
                return new(false, -1, string.Empty, string.Empty, "Windows could not start the WSL dry-run process.");

            object protocolGate = new();

            Task<string> stdoutTask = ReadAndMonitorAsync(
                process.StandardOutput,
                process,
                protocol,
                protocolGate,
                isError: false,
                outputLine,
                cancellationToken);
            Task<string> stderrTask = ReadAndMonitorAsync(
                process.StandardError,
                process,
                protocol,
                protocolGate,
                isError: true,
                outputLine,
                cancellationToken);

            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(MaximumExecutionTime);

            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                KillBestEffort(process);
                string timeoutMessage = $"Dry-run execution exceeded the {MaximumExecutionTime.TotalMinutes:0}-minute safety limit and was terminated.";
                outputLine?.Invoke("FAIL [ZDeployPet safety] " + timeoutMessage, true);
                string stdoutTimed = await stdoutTask;
                string stderrTimed = await stderrTask;
                return new(true, SafetyViolationExitCode, stdoutTimed, AppendBounded(stderrTimed, "\nFAIL [ZDeployPet safety] " + timeoutMessage + "\n"), timeoutMessage);
            }
            catch (OperationCanceledException)
            {
                KillBestEffort(process);
                throw;
            }

            string stdout = await stdoutTask;
            string stderr = await stderrTask;

            lock (protocolGate)
                protocol.MarkProcessExited(process.ExitCode);

            if (protocol.SafetyViolation)
            {
                string reason = protocol.ViolationReason ?? "The dry-run prompt protocol failed closed.";
                stderr = AppendBounded(stderr, $"\nFAIL [ZDeployPet safety] {reason}\n");
                return new(true, SafetyViolationExitCode, stdout, stderr, reason);
            }

            if (!protocol.Completed || !protocol.DryModeConfirmed)
            {
                const string reason = "The approved script did not prove DRY RUN mode and completion; ZDeployPet failed closed.";
                stderr = AppendBounded(stderr, $"\nFAIL [ZDeployPet safety] {reason}\n");
                return new(true, SafetyViolationExitCode, stdout, stderr, reason);
            }

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

    private static string NormalizeBashScript(string script)
        => script.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static async Task<string> ReadAndMonitorAsync(
        StreamReader reader,
        Process process,
        DryRunPromptProtocol protocol,
        object protocolGate,
        bool isError,
        Action<string, bool>? outputLine,
        CancellationToken cancellationToken)
    {
        char[] buffer = new char[1024];
        StringBuilder captured = new();
        StringBuilder liveLine = new();

        while (true)
        {
            int read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0)
                break;

            string chunk = new(buffer, 0, read);
            AppendBounded(captured, chunk);
            EmitLiveLines(chunk, liveLine, isError, outputLine);

            bool violation;
            lock (protocolGate)
            {
                // The read adapter already supplies the allowlisted answers. Observe still
                // advances the protocol stages and detects any attempt to enter live mode;
                // the returned answer is intentionally ignored and is never written to stdin.
                _ = protocol.Observe(chunk);
                violation = protocol.SafetyViolation;
            }

            if (violation)
                KillBestEffort(process);
        }

        EmitPendingLiveLine(liveLine, isError, outputLine);
        return captured.ToString();
    }

    private static void EmitLiveLines(
        string chunk,
        StringBuilder liveLine,
        bool isError,
        Action<string, bool>? outputLine)
    {
        if (outputLine is null)
            return;

        foreach (char character in chunk)
        {
            if (character == '\n')
            {
                EmitPendingLiveLine(liveLine, isError, outputLine);
                continue;
            }

            if (character == '\r')
                continue;

            if (liveLine.Length < MaximumLiveLineCharacters)
                liveLine.Append(character);
        }
    }

    private static void EmitPendingLiveLine(
        StringBuilder liveLine,
        bool isError,
        Action<string, bool>? outputLine)
    {
        if (outputLine is null || liveLine.Length == 0)
        {
            liveLine.Clear();
            return;
        }

        string line = liveLine.ToString();
        liveLine.Clear();

        try
        {
            outputLine(line, isError);
        }
        catch
        {
            // Live display is observational only and must never influence execution.
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
            promptPlan.Validate();
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

    private static void AppendBounded(StringBuilder builder, string text)
    {
        int remaining = MaximumCapturedCharacters - builder.Length;
        if (remaining <= 0)
            return;

        builder.Append(text, 0, Math.Min(text.Length, remaining));
        if (builder.Length >= MaximumCapturedCharacters)
            builder.Append("\n[ZDeployPet output capture truncated at 4 MiB]\n");
    }

    private static string AppendBounded(string existing, string suffix)
    {
        if (existing.Length >= MaximumCapturedCharacters)
            return existing;

        int remaining = MaximumCapturedCharacters - existing.Length;
        return existing + suffix[..Math.Min(suffix.Length, remaining)];
    }

    private static void KillBestEffort(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best-effort fail-closed cleanup only.
        }
    }
}
