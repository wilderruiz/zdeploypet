using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using ZDeployPet.Core;

namespace ZDeployPet.WslBridge;

public sealed class WslSshAgentProbe
{
    private const string ProbeScript = """
        set -eu
        socket="$1"
        pid="$2"
        host="$3"
        port="$4"
        user="$5"
        host_key_line="$6"

        [ -S "$socket" ] || { echo "probe_error=AGENT_SOCKET_MISSING"; exit 31; }
        kill -0 "$pid" 2>/dev/null || { echo "probe_error=AGENT_PID_MISSING"; exit 32; }

        export SSH_AUTH_SOCK="$socket"
        export SSH_AGENT_PID="$pid"

        known_hosts="$(mktemp)"
        trap 'rm -f "$known_hosts"' EXIT HUP INT TERM
        echo "$host_key_line" > "$known_hosts"
        chmod 600 "$known_hosts"

        set +e
        output="$(ssh \
          -o BatchMode=yes \
          -o PasswordAuthentication=no \
          -o KbdInteractiveAuthentication=no \
          -o StrictHostKeyChecking=yes \
          -o UserKnownHostsFile="$known_hosts" \
          -o GlobalKnownHostsFile=/dev/null \
          -o IdentityAgent="$socket" \
          -o ConnectTimeout=7 \
          -o ConnectionAttempts=1 \
          -p "$port" \
          "$user@$host" \
          'printf ZDEPLOYPET_AGENT_PROBE_OK' 2>&1)"
        code=$?
        set -e

        if [ "$code" -eq 0 ] && [ "$output" = 'ZDEPLOYPET_AGENT_PROBE_OK' ]; then
          echo "probe_ok=true"
          exit 0
        fi

        echo "probe_error=$output"
        exit "$code"
        """;

    public async Task<SshProbeResult> ProbeAsync(
        string distribution,
        DeploymentTarget target,
        DeploymentKeyIdentity deploymentKey,
        TargetHostIdentity enrolledHostKey,
        WslSshAgentRuntime runtime,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(distribution, runtime.Distribution, StringComparison.Ordinal))
            return new(SshProbeStatus.Failed, "The bounded ssh-agent belongs to a different WSL distribution.");

        if (string.IsNullOrWhiteSpace(target.Host) || string.IsNullOrWhiteSpace(target.User) ||
            target.Port is < 1 or > 65535)
            return new(SshProbeStatus.InvalidTarget, "The configured target host, user or port is invalid.");

        string? observedKeyFingerprint = await ReadPublicKeyFingerprintAsync(
            distribution,
            deploymentKey.PublicKeyPath,
            cancellationToken);
        if (observedKeyFingerprint is null)
            return new(SshProbeStatus.MissingKey, "The approved deployment public key could not be read in WSL.");
        if (!FingerprintEquals(observedKeyFingerprint, deploymentKey.Fingerprint))
            return new(SshProbeStatus.KeyMismatch, "The approved deployment public key changed after enrollment. The bounded-agent probe was blocked.");

        HostScanResult scan = await ScanEnrolledHostKeyAsync(
            distribution,
            target,
            enrolledHostKey.KeyType,
            cancellationToken);
        if (!scan.Success || scan.Fingerprint is null || scan.HostKeyLine is null)
            return new(SshProbeStatus.HostUnreachable, scan.Error ?? "The SSH host could not be reached.");

        if (!FingerprintEquals(scan.Fingerprint, enrolledHostKey.Fingerprint))
        {
            return new(
                SshProbeStatus.HostKeyMismatch,
                "The server host key changed. ZDeployPet blocked the bounded-agent probe. Re-enroll only after independently verifying the new fingerprint.",
                scan.Fingerprint);
        }

        CommandResult result = await RunWslShellAsync(
            distribution,
            ProbeScript,
            [
                runtime.SocketPath,
                runtime.AgentPid.ToString(CultureInfo.InvariantCulture),
                target.Host.Trim(),
                target.Port.ToString(CultureInfo.InvariantCulture),
                target.User.Trim(),
                scan.HostKeyLine
            ],
            cancellationToken);

        Dictionary<string, string> values = ParseKeyValues(result.Output);
        if (result.Started && result.ExitCode == 0 &&
            values.TryGetValue("probe_ok", out string? ok) &&
            string.Equals(ok, "true", StringComparison.OrdinalIgnoreCase))
        {
            return new(
                SshProbeStatus.Success,
                "Authenticated read-only SSH probe passed through the bounded ZDeployPet ssh-agent. No private-key file was passed to ssh and no target file write was performed.",
                scan.Fingerprint);
        }

        string diagnostic = values.TryGetValue("probe_error", out string? probeError)
            ? probeError
            : result.Error;
        if (diagnostic.Contains("AGENT_SOCKET_MISSING", StringComparison.OrdinalIgnoreCase) ||
            diagnostic.Contains("AGENT_PID_MISSING", StringComparison.OrdinalIgnoreCase))
            return new(SshProbeStatus.Failed, "The bounded ZDeployPet ssh-agent disappeared before the probe completed.", scan.Fingerprint);
        if (diagnostic.Contains("Permission denied", StringComparison.OrdinalIgnoreCase) ||
            diagnostic.Contains("no mutual signature", StringComparison.OrdinalIgnoreCase))
            return new(SshProbeStatus.AuthenticationRejected, "The server rejected the identity offered by the bounded ZDeployPet ssh-agent.", scan.Fingerprint);
        if (diagnostic.Contains("Host key verification failed", StringComparison.OrdinalIgnoreCase) ||
            diagnostic.Contains("REMOTE HOST IDENTIFICATION", StringComparison.OrdinalIgnoreCase))
            return new(SshProbeStatus.HostKeyMismatch, "SSH host-key verification failed closed.", scan.Fingerprint);
        if (diagnostic.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
            diagnostic.Contains("Connection timed out", StringComparison.OrdinalIgnoreCase) ||
            diagnostic.Contains("No route to host", StringComparison.OrdinalIgnoreCase) ||
            diagnostic.Contains("Could not resolve hostname", StringComparison.OrdinalIgnoreCase))
            return new(SshProbeStatus.HostUnreachable, "The SSH target is unreachable with the configured host and port.", scan.Fingerprint);

        return new(
            SshProbeStatus.Failed,
            string.IsNullOrWhiteSpace(diagnostic)
                ? "The bounded-agent SSH probe failed without a recognized diagnostic."
                : "The bounded-agent SSH probe failed: " + diagnostic.Trim(),
            scan.Fingerprint);
    }

    private static async Task<string?> ReadPublicKeyFingerprintAsync(
        string distribution,
        string publicKeyPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(publicKeyPath)) return null;
        CommandResult result = await RunWslExecutableAsync(
            distribution,
            "/usr/bin/ssh-keygen",
            ["-lf", publicKeyPath, "-E", "sha256"],
            cancellationToken);
        if (!result.Started || result.ExitCode != 0) return null;

        return result.Output
            .Split([' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(part => part.StartsWith("SHA256:", StringComparison.Ordinal));
    }

    private static async Task<HostScanResult> ScanEnrolledHostKeyAsync(
        string distribution,
        DeploymentTarget target,
        string enrolledKeyType,
        CancellationToken cancellationToken)
    {
        string? scanType = MapSshKeyscanType(enrolledKeyType);
        if (scanType is null)
            return new(false, null, null, $"Unsupported enrolled SSH host-key type '{enrolledKeyType}'.");

        CommandResult result = await RunWslExecutableAsync(
            distribution,
            "/usr/bin/ssh-keyscan",
            [
                "-T", "6",
                "-p", target.Port.ToString(CultureInfo.InvariantCulture),
                "-t", scanType,
                target.Host.Trim()
            ],
            cancellationToken);

        string? line = result.Output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim())
            .FirstOrDefault(value => value.Length > 0 && !value.StartsWith('#'));
        if (!result.Started || string.IsNullOrWhiteSpace(line))
        {
            string error = string.IsNullOrWhiteSpace(result.Error)
                ? "The SSH host did not return the enrolled host-key type."
                : result.Error.Trim();
            return new(false, null, null, error);
        }

        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            return new(false, null, null, "The SSH host key returned by the server could not be parsed.");

        try
        {
            byte[] keyBlob = Convert.FromBase64String(parts[2]);
            string fingerprint = "SHA256:" + Convert.ToBase64String(SHA256.HashData(keyBlob)).TrimEnd('=');
            return new(true, fingerprint, line, null);
        }
        catch (FormatException)
        {
            return new(false, null, null, "The SSH host key returned by the server contained invalid key data.");
        }
    }

    private static string? MapSshKeyscanType(string keyType) => keyType.Trim().ToUpperInvariant() switch
    {
        "ED25519" or "SSH-ED25519" => "ed25519",
        "ECDSA" or "ECDSA-SHA2-NISTP256" => "ecdsa",
        "RSA" or "SSH-RSA" => "rsa",
        _ => null
    };

    private static bool FingerprintEquals(string left, string right) =>
        string.Equals(left.Trim().TrimEnd('='), right.Trim().TrimEnd('='), StringComparison.Ordinal);

    private static Dictionary<string, string> ParseKeyValues(string output)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        foreach (string raw in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            int index = raw.IndexOf('=');
            if (index <= 0) continue;
            values[raw[..index].Trim()] = raw[(index + 1)..].Trim();
        }
        return values;
    }

    private static async Task<CommandResult> RunWslShellAsync(
        string distribution,
        string script,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        // Raw string literals in a Windows checkout can carry CRLF line endings.
        // Passing those bytes directly to /bin/sh -c leaves a trailing CR on shell
        // built-ins such as `set -eu`, which dash reports as `set: Illegal option -`.
        // Normalize every inline probe script before crossing the WSL boundary.
        string normalizedScript = script
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        List<string> allArguments = ["-d", distribution, "--exec", "/bin/sh", "-c", normalizedScript, "zdeploypet-agent-probe"];
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

    private sealed record HostScanResult(bool Success, string? Fingerprint, string? HostKeyLine, string? Error);
    private sealed record CommandResult(bool Started, int ExitCode, string Output, string Error);
}
