using System.Diagnostics;
using System.Globalization;
using System.Text;
using ZDeployPet.Core;

namespace ZDeployPet.WslBridge;

public sealed record WslLiveDeploymentExecutionResult(
    bool Started,
    int ExitCode,
    string StandardOutput,
    string StandardError,
    string? Error = null);

public sealed class WslLiveDeploymentExecutor
{
    private const int MaximumCapturedCharacters = 4 * 1024 * 1024;
    private const int MaximumLiveLineCharacters = 16 * 1024;
    private const int SafetyViolationExitCode = 97;
    private static readonly TimeSpan MaximumExecutionTime = TimeSpan.FromMinutes(30);

    // The operator's already-validated phrase is passed as an argument and is forwarded
    // exactly once, only when the approved script asks the exact configured confirmation
    // prompt. The adapter supplies only allowlisted numeric target/release answers and
    // structurally forces mode 2 (live). It never derives or invents confirmation text.
    private const string LiveReadAdapter = """
set -euo pipefail
script_path="$1"
target_choice="$2"
release_choice="$3"
expected_phrase="$4"
operator_phrase="$5"
confirmation_prompt="Type ${expected_phrase} to continue: "
confirmation_used=0

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
                printf '%s2\n' "$3" >&2
                printf -v "$4" '%s' "2"
                return 0
                ;;
        esac

        if [[ "$3" == "$confirmation_prompt" ]]; then
            if [[ "$confirmation_used" -ne 0 ]]; then
                echo "FAIL [ZDeployPet safety] Live confirmation prompt was requested more than once." >&2
                return 97
            fi
            if [[ "$operator_phrase" != "$expected_phrase" ]]; then
                echo "FAIL [ZDeployPet safety] Human confirmation no longer exactly matches the configured phrase." >&2
                return 97
            fi
            printf '%s[operator-entered confirmation accepted]\n' "$3" >&2
            printf -v "$4" '%s' "$operator_phrase"
            confirmation_used=1
            return 0
        fi
    fi

    builtin read "$@"
}

source "$script_path"
""";

    public async Task<WslLiveDeploymentExecutionResult> ExecuteAsync(
        string distribution,
        string wslProjectPath,
        string scriptRelativePath,
        WslSshAgentRuntime runtime,
        LiveDeploymentPromptPlan promptPlan,
        string operatorTypedConfirmation,
        Action<string, bool>? outputLine = null,
        CancellationToken cancellationToken = default)
    {
        string? validationError = ValidateInvocation(
            distribution,
            wslProjectPath,
            scriptRelativePath,
            runtime,
            promptPlan,
            operatorTypedConfirmation);
        if (validationError is not null)
            return new(false, -1, string.Empty, string.Empty, validationError);

        LiveDeploymentPromptProtocol protocol;
        try
        {
            protocol = new LiveDeploymentPromptProtocol(promptPlan);
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
        startInfo.ArgumentList.Add(NormalizeBashScript(LiveReadAdapter));
        startInfo.ArgumentList.Add("zdeploypet-live-deploy");
        startInfo.ArgumentList.Add(scriptRelativePath.Trim());
        startInfo.ArgumentList.Add(promptPlan.TargetChoice.ToString(CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add(promptPlan.ReleaseChoice?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        startInfo.ArgumentList.Add(promptPlan.ConfirmationPhrase);
        startInfo.ArgumentList.Add(operatorTypedConfirmation);

        try
        {
            using Process process = new() { StartInfo = startInfo };
            if (!process.Start())
                return new(false, -1, string.Empty, string.Empty, "Windows could not start the WSL live-deployment process.");

            object protocolGate = new();
            Task<string> stdoutTask = ReadAndMonitorAsync(
                process.StandardOutput,
                protocol,
                protocolGate,
                isError: false,
                outputLine,
                cancellationToken);
            Task<string> stderrTask = ReadAndMonitorAsync(
                process.StandardError,
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
                string timeoutMessage = $"Live deployment exceeded the {MaximumExecutionTime.TotalMinutes:0}-minute safety limit and was terminated. ZDeployPet will not retry it automatically.";
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
                string reason = protocol.ViolationReason ?? "The live-deployment protocol failed closed.";
                stderr = AppendBounded(stderr, $"\nFAIL [ZDeployPet safety] {reason}\n");
                return new(true, SafetyViolationExitCode, stdout, stderr, reason);
            }

            if (!protocol.Completed || !protocol.LiveModeObserved || !protocol.ConfirmationPromptObserved)
            {
                const string reason = "The approved script did not prove the complete PDA-6 live-deployment protocol; ZDeployPet failed closed.";
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

    private static string? ValidateInvocation(
        string distribution,
        string wslProjectPath,
        string scriptRelativePath,
        WslSshAgentRuntime runtime,
        LiveDeploymentPromptPlan promptPlan,
        string operatorTypedConfirmation)
    {
        if (string.IsNullOrWhiteSpace(distribution))
            return "A WSL distribution is required for live deployment.";
        if (!string.Equals(distribution.Trim(), runtime.Distribution.Trim(), StringComparison.Ordinal))
            return "The live-deployment WSL distribution does not match the app-owned deployment-access session.";
        if (runtime.AgentPid <= 0 || string.IsNullOrWhiteSpace(runtime.SocketPath) || !runtime.SocketPath.StartsWith('/'))
            return "The app-owned deployment-access session runtime is invalid.";
        if (string.IsNullOrWhiteSpace(wslProjectPath) || !wslProjectPath.StartsWith('/'))
            return "A verified absolute WSL project path is required.";
        if (!WslDryRunExecutor.IsSafeRelativeScriptPath(scriptRelativePath))
            return "The deployment script must be a safe project-relative path.";

        try
        {
            promptPlan.Validate();
        }
        catch (InvalidDataException exception)
        {
            return exception.Message;
        }

        if (!string.Equals(operatorTypedConfirmation, promptPlan.ConfirmationPhrase, StringComparison.Ordinal))
            return "The operator-entered live confirmation no longer exactly matches the immutable review phrase.";

        return null;
    }

    private static string NormalizeBashScript(string script)
        => script.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static async Task<string> ReadAndMonitorAsync(
        StreamReader reader,
        LiveDeploymentPromptProtocol protocol,
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

            lock (protocolGate)
                protocol.Observe(chunk);
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
            // Best-effort cleanup. A live deployment is never retried automatically.
        }
    }
}
