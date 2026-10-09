using System.Diagnostics;
using System.Globalization;
using ZDeployPet.Core;

namespace ZDeployPet.WslBridge;

public sealed record WslSshAgentRuntime(
    string Distribution,
    int AgentPid,
    string SocketPath,
    DateTimeOffset StartedAtUtc);

public sealed record WslSshAgentInspection(
    bool AgentReachable,
    IReadOnlyList<string> LoadedFingerprints,
    string? Error = null);

public sealed record WslSshAgentStartResult(
    bool Success,
    WslSshAgentRuntime? Runtime,
    string? Error = null);

public sealed record WslSshAgentUnlockLaunchResult(
    bool Success,
    string Message);

public sealed record WslSshAgentLockResult(bool Success, string? Error = null);

public sealed class WslSshAgentSession
{
    private const string UnlockScript = """
        set -eu
        socket="$1"
        pid="$2"
        public_key="$3"
        expected_fingerprint="$4"
        lifetime_seconds="$5"
        profile_name="$6"
        private_key="${public_key%.pub}"

        export SSH_AUTH_SOCK="$socket"
        export SSH_AGENT_PID="$pid"

        echo
        echo "============================================================"
        echo " ZDEPLOYPET BOUNDED DEPLOYMENT ACCESS"
        echo "============================================================"
        echo "Profile              : $profile_name"
        echo "Approved fingerprint : $expected_fingerprint"
        echo "Lease                : $lifetime_seconds seconds"
        echo
        echo "ZDeployPet is asking OpenSSH to load ONLY the approved deployment key"
        echo "into this app-owned ssh-agent for the bounded lease above."
        echo "If the private key has a passphrase, type it only in this terminal."
        echo "ZDeployPet does not receive or store the passphrase."
        echo "============================================================"
        echo

        if [ ! -S "$socket" ]; then
          echo "ZDeployPet blocked unlock: the app-owned ssh-agent socket is missing."
          echo "Press Enter to close."
          read _
          exit 20
        fi
        if [ ! -f "$private_key" ]; then
          echo "ZDeployPet blocked unlock: the approved private key is missing."
          echo "Press Enter to close."
          read _
          exit 21
        fi

        set +e
        ssh-add -t "$lifetime_seconds" "$private_key"
        code=$?
        set -e

        echo
        if [ "$code" -eq 0 ]; then
          echo "OpenSSH accepted the key. Return to ZDeployPet and click Check session."
        else
          echo "The key was not loaded. ssh-add exit code: $code"
        fi
        echo "Press Enter to close."
        read _
        exit "$code"
        """;

    public async Task<WslSshAgentStartResult> StartAsync(
        string distribution,
        string profileId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(distribution))
            return new(false, null, "A WSL distribution is required.");

        string slug = SanitizeToken(profileId);
        string socket = $"/tmp/zdeploypet-{slug}-{Guid.NewGuid():N}.sock";
        CommandResult result = await RunWslExecutableAsync(
            distribution,
            "/usr/bin/ssh-agent",
            ["-s", "-a", socket],
            cancellationToken);

        if (!result.Started || result.ExitCode != 0)
            return new(false, null, FirstError(result, "ssh-agent could not be started."));

        int? pid = ParseAgentPid(result.Output);
        if (pid is null || pid <= 0)
            return new(false, null, "ssh-agent started, but its process ID could not be parsed.");

        return new(true, new(distribution.Trim(), pid.Value, socket, DateTimeOffset.UtcNow));
    }

    public async Task<WslSshAgentUnlockLaunchResult> LaunchUnlockAsync(
        WslSshAgentRuntime runtime,
        DeploymentKeyIdentity deploymentKey,
        string profileName,
        TimeSpan? lifetime = null,
        CancellationToken cancellationToken = default)
    {
        CommandResult identity = await ValidateOwnedAgentAsync(runtime, cancellationToken);
        if (!identity.Started || identity.ExitCode != 0)
            return new(false, "ZDeployPet refused to unlock because the app-owned ssh-agent could not be verified.");

        TimeSpan duration = lifetime ?? DeploymentAccessSessionLease.DefaultLifetime;
        if (duration <= TimeSpan.Zero || duration > TimeSpan.FromHours(24))
            return new(false, "The requested SSH-agent lease is outside the allowed range.");

        string normalizedScript = UnlockScript
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        using Process process = new();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "wsl.exe",
            UseShellExecute = true,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Normal
        };
        process.StartInfo.ArgumentList.Add("--distribution");
        process.StartInfo.ArgumentList.Add(runtime.Distribution);
        process.StartInfo.ArgumentList.Add("--exec");
        process.StartInfo.ArgumentList.Add("/bin/sh");
        process.StartInfo.ArgumentList.Add("-c");
        process.StartInfo.ArgumentList.Add(normalizedScript);
        process.StartInfo.ArgumentList.Add("zdeploypet-pda2-unlock");
        process.StartInfo.ArgumentList.Add(runtime.SocketPath);
        process.StartInfo.ArgumentList.Add(runtime.AgentPid.ToString(CultureInfo.InvariantCulture));
        process.StartInfo.ArgumentList.Add(deploymentKey.PublicKeyPath);
        process.StartInfo.ArgumentList.Add(deploymentKey.Fingerprint.Trim());
        process.StartInfo.ArgumentList.Add(((int)duration.TotalSeconds).ToString(CultureInfo.InvariantCulture));
        process.StartInfo.ArgumentList.Add(profileName.Trim());

        try
        {
            if (!process.Start())
                return new(false, "Windows could not start the trusted WSL ssh-add terminal.");
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new(false, "Windows could not start the trusted WSL ssh-add terminal: " + exception.Message);
        }

        return new(true,
            "The trusted OpenSSH unlock terminal was opened. Complete ssh-add there, then return to ZDeployPet and click Check session. ZDeployPet never receives the key passphrase.");
    }

    public async Task<WslSshAgentInspection> InspectAsync(
        WslSshAgentRuntime runtime,
        CancellationToken cancellationToken = default)
    {
        CommandResult identity = await ValidateOwnedAgentAsync(runtime, cancellationToken);
        if (!identity.Started || identity.ExitCode != 0)
            return new(false, [], FirstError(identity, "The ZDeployPet ssh-agent is no longer reachable or is not the expected process."));

        CommandResult keys = await RunWithAgentEnvironmentAsync(
            runtime,
            "/usr/bin/ssh-add",
            ["-l", "-E", "sha256"],
            cancellationToken);

        if (!keys.Started)
            return new(false, [], "ssh-add could not be started.");

        if (keys.ExitCode == 1 && keys.Output.Contains("no identities", StringComparison.OrdinalIgnoreCase))
            return new(true, []);

        if (keys.ExitCode != 0)
            return new(false, [], FirstError(keys, "The ZDeployPet ssh-agent could not be inspected."));

        return new(true, ParseLoadedFingerprints(keys.Output));
    }

    public async Task<WslSshAgentLockResult> LockAsync(
        WslSshAgentRuntime runtime,
        CancellationToken cancellationToken = default)
    {
        CommandResult identity = await ValidateOwnedAgentAsync(runtime, cancellationToken);
        if (!identity.Started || identity.ExitCode != 0)
            return new(false, "ZDeployPet refused to terminate an agent whose ownership/process identity could not be verified.");

        CommandResult result = await RunWithAgentEnvironmentAsync(
            runtime,
            "/usr/bin/ssh-agent",
            ["-k"],
            cancellationToken);
        if (!result.Started || result.ExitCode != 0)
            return new(false, FirstError(result, "The ZDeployPet ssh-agent could not be stopped."));

        return new(true);
    }

    public static DeploymentAccessSessionLease CreateLease(
        string profileId,
        WslSshAgentRuntime runtime,
        string approvedKeyFingerprint,
        DateTimeOffset? nowUtc = null,
        TimeSpan? lifetime = null)
    {
        DateTimeOffset started = nowUtc ?? DateTimeOffset.UtcNow;
        TimeSpan duration = lifetime ?? DeploymentAccessSessionLease.DefaultLifetime;
        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime), "Session lifetime must be positive.");

        return new(
            profileId,
            runtime.Distribution,
            runtime.AgentPid,
            runtime.SocketPath,
            approvedKeyFingerprint,
            started,
            started.Add(duration));
    }

    internal static int? ParseAgentPid(string output)
    {
        const string marker = "SSH_AGENT_PID=";
        foreach (string raw in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string line = raw.Trim();
            int index = line.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0) continue;
            string value = line[(index + marker.Length)..].Split(';', 2)[0].Trim();
            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int pid))
                return pid;
        }
        return null;
    }

    internal static IReadOnlyList<string> ParseLoadedFingerprints(string output)
    {
        List<string> fingerprints = [];
        foreach (string raw in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string? fingerprint = parts.FirstOrDefault(part => part.StartsWith("SHA256:", StringComparison.Ordinal));
            if (!string.IsNullOrWhiteSpace(fingerprint)) fingerprints.Add(fingerprint);
        }
        return fingerprints.Distinct(StringComparer.Ordinal).ToArray();
    }

    private async Task<CommandResult> ValidateOwnedAgentAsync(
        WslSshAgentRuntime runtime,
        CancellationToken cancellationToken)
    {
        const string script = """
            set -eu
            pid="$1"
            socket="$2"
            [ -S "$socket" ] || exit 21
            [ -r "/proc/$pid/comm" ] || exit 22
            comm="$(cat "/proc/$pid/comm")"
            [ "$comm" = "ssh-agent" ] || exit 23
            kill -0 "$pid" 2>/dev/null || exit 24
            """;

        return await RunWslShellAsync(
            runtime.Distribution,
            script,
            [runtime.AgentPid.ToString(CultureInfo.InvariantCulture), runtime.SocketPath],
            cancellationToken);
    }

    private async Task<CommandResult> RunWithAgentEnvironmentAsync(
        WslSshAgentRuntime runtime,
        string executable,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        List<string> shellArguments =
        [
            runtime.SocketPath,
            runtime.AgentPid.ToString(CultureInfo.InvariantCulture),
            executable,
            .. arguments
        ];

        const string script = """
            set -eu
            export SSH_AUTH_SOCK="$1"
            export SSH_AGENT_PID="$2"
            shift 2
            exec "$@"
            """;

        return await RunWslShellAsync(runtime.Distribution, script, shellArguments, cancellationToken);
    }

    private static async Task<CommandResult> RunWslShellAsync(
        string distribution,
        string script,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        List<string> allArguments = ["-d", distribution, "--exec", "/bin/sh", "-c", script, "zdeploypet-pda2"];
        allArguments.AddRange(arguments);
        return await RunProcessAsync("wsl.exe", allArguments, cancellationToken);
    }

    private static Task<CommandResult> RunWslExecutableAsync(
        string distribution,
        string executable,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        List<string> allArguments = ["-d", distribution, "--exec", executable];
        allArguments.AddRange(arguments);
        return RunProcessAsync("wsl.exe", allArguments, cancellationToken);
    }

    private static async Task<CommandResult> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo info = new(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (string argument in arguments) info.ArgumentList.Add(argument);

        try
        {
            using Process process = new() { StartInfo = info };
            if (!process.Start()) return new(false, -1, string.Empty, "Process did not start.");
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(cancellationToken);
            return new(true, process.ExitCode, await stdout, await stderr);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new(false, -1, string.Empty, exception.Message);
        }
    }

    private static string SanitizeToken(string value)
    {
        string sanitized = new((value ?? string.Empty)
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());
        sanitized = sanitized.Trim('-');
        if (sanitized.Length == 0) sanitized = "profile";
        return sanitized.Length <= 32 ? sanitized : sanitized[..32];
    }

    private static string FirstError(CommandResult result, string fallback) =>
        !string.IsNullOrWhiteSpace(result.Error)
            ? result.Error.Trim()
            : !string.IsNullOrWhiteSpace(result.Output)
                ? result.Output.Trim()
                : fallback;

    private sealed record CommandResult(bool Started, int ExitCode, string Output, string Error);
}
