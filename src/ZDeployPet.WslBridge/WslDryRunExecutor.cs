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

            // Never pre-buffer synthetic answers. The earlier PDA-5 implementation wrote
            // target/release/mode up front, which allowed commands launched during preflight
            // to consume later answers and caused a supposed dry run to enter live mode.
            // Answers are now emitted only after the exact approved-script prompt is observed.
            process.StandardInput.NewLine = "\n";

            object protocolGate = new();
            using SemaphoreSlim inputGate = new(1, 1);

            Task<string> stdoutTask = ReadAndDriveAsync(
                process.StandardOutput,
                process,
                protocol,
                protocolGate,
                inputGate,
                isError: false,
                outputLine,
                cancellationToken);
            Task<string> stderrTask = ReadAndDriveAsync(
                process.StandardError,
                process,
                protocol,
                protocolGate,
                inputGate,
                isError: true,
                outputLine,
                cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken);
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

    private static async Task<string> ReadAndDriveAsync(
        StreamReader reader,
        Process process,
        DryRunPromptProtocol protocol,
        object protocolGate,
        SemaphoreSlim inputGate,
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

            string? response;
            bool violation;
            lock (protocolGate)
            {
                response = protocol.Observe(chunk);
                violation = protocol.SafetyViolation;
            }

            if (violation)
            {
                KillBestEffort(process);
                continue;
            }

            if (response is null)
                continue;

            await inputGate.WaitAsync(cancellationToken);
            try
            {
                if (!process.HasExited)
                {
                    await process.StandardInput.WriteLineAsync(response.AsMemory(), cancellationToken);
                    await process.StandardInput.FlushAsync(cancellationToken);
                }
            }
            catch (IOException)
            {
                // The process may have exited between observing a prompt and writing the
                // allowlisted answer. Process/report truth below remains authoritative.
            }
            finally
            {
                inputGate.Release();
            }
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
