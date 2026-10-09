using System.Diagnostics;
using ZDeployPet.Core;

namespace ZDeployPet.WslBridge;

public sealed class WslSshDiagnostics
{
    private const string ListKeysScript = """
        set -eu
        found=0
        for f in "$HOME"/.ssh/*.pub; do
          [ -f "$f" ] || continue
          line="$(ssh-keygen -lf "$f" -E sha256 2>/dev/null || true)"
          [ -n "$line" ] || continue
          found=1
          printf 'path=%s\n' "$f"
          printf 'info=%s\n' "$line"
          printf '%s\n' '--'
        done
        [ "$found" -eq 1 ] || true
        """;

    private const string ScanHostKeyScript = """
        set -eu
        host="$1"
        port="$2"
        line="$(ssh-keyscan -T 6 -p "$port" "$host" 2>/dev/null | head -n 1 || true)"
        if [ -z "$line" ]; then
          printf 'error=No SSH host key was returned.\n'
          exit 2
        fi
        info="$(printf '%s\n' "$line" | ssh-keygen -lf - -E sha256 2>/dev/null || true)"
        if [ -z "$info" ]; then
          printf 'error=The returned SSH host key could not be fingerprinted.\n'
          exit 3
        fi
        printf 'info=%s\n' "$info"
        printf 'key=%s\n' "$line"
        """;

    private const string ProbeScript = """
        set -eu
        host="$1"
        port="$2"
        user="$3"
        public_key="$4"
        host_key_line="$5"
        private_key="${public_key%.pub}"
        if [ ! -f "$public_key" ] || [ ! -f "$private_key" ]; then
          printf 'probe_error=MISSING_KEY\n'
          exit 20
        fi
        known_hosts="$(mktemp)"
        trap 'rm -f "$known_hosts"' EXIT HUP INT TERM
        printf '%s\n' "$host_key_line" > "$known_hosts"
        set +e
        output="$(ssh \
          -o BatchMode=yes \
          -o PasswordAuthentication=no \
          -o KbdInteractiveAuthentication=no \
          -o StrictHostKeyChecking=yes \
          -o UserKnownHostsFile="$known_hosts" \
          -o GlobalKnownHostsFile=/dev/null \
          -o IdentitiesOnly=yes \
          -o ConnectTimeout=7 \
          -o ConnectionAttempts=1 \
          -p "$port" \
          -i "$private_key" \
          "$user@$host" \
          'printf ZDEPLOYPET_PROBE_OK' 2>&1)"
        code=$?
        set -e
        if [ "$code" -eq 0 ] && [ "$output" = 'ZDEPLOYPET_PROBE_OK' ]; then
          printf 'probe_ok=true\n'
          exit 0
        fi
        printf 'probe_error=%s\n' "$output"
        exit "$code"
        """;

    public async Task<IReadOnlyList<SshPublicKeyCandidate>> ListPublicKeysAsync(
        string distribution,
        CancellationToken cancellationToken = default)
    {
        CommandResult result = await RunWslAsync(distribution, ListKeysScript, [], cancellationToken);
        if (!result.Started || result.ExitCode != 0) return [];
        return ParsePublicKeys(result.Output);
    }

    public async Task<HostKeyScanResult> ScanHostKeyAsync(
        string distribution,
        string host,
        int port,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host) || port is < 1 or > 65535)
            return new(false, null, null, null, "The target host or SSH port is invalid.");

        CommandResult result = await RunWslAsync(
            distribution,
            ScanHostKeyScript,
            [host.Trim(), port.ToString(System.Globalization.CultureInfo.InvariantCulture)],
            cancellationToken);

        Dictionary<string, string> values = ParseKeyValues(result.Output);
        if (!result.Started || result.ExitCode != 0 ||
            !values.TryGetValue("info", out string? info) ||
            !values.TryGetValue("key", out string? keyLine))
        {
            string error = values.TryGetValue("error", out string? reported)
                ? reported
                : string.IsNullOrWhiteSpace(result.Error) ? "SSH host-key scan failed." : result.Error.Trim();
            return new(false, null, null, null, error);
        }

        ParsedFingerprint? parsed = ParseFingerprintInfo(info);
        if (parsed is null)
            return new(false, null, null, null, "SSH host-key fingerprint output was not recognized.");

        return new(true, parsed.Fingerprint, parsed.Algorithm, keyLine, null);
    }

    public async Task<SshProbeResult> ProbeAsync(
        string distribution,
        DeploymentTarget target,
        DeploymentKeyIdentity deploymentKey,
        TargetHostIdentity enrolledHostKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(target.Host) || string.IsNullOrWhiteSpace(target.User) ||
            target.Port is < 1 or > 65535)
            return new(SshProbeStatus.InvalidTarget, "The configured target host, user or port is invalid.");

        if (string.IsNullOrWhiteSpace(deploymentKey.PublicKeyPath))
            return new(SshProbeStatus.MissingKey, "No dedicated deployment public key is selected.");

        IReadOnlyList<SshPublicKeyCandidate> currentKeys = await ListPublicKeysAsync(distribution, cancellationToken);
        SshPublicKeyCandidate? currentKey = currentKeys.FirstOrDefault(candidate =>
            string.Equals(candidate.PublicKeyPath, deploymentKey.PublicKeyPath, StringComparison.Ordinal));
        if (currentKey is null)
            return new(SshProbeStatus.MissingKey, "The selected deployment public key no longer exists in WSL.");
        if (!string.Equals(
                NormalizeFingerprint(currentKey.Fingerprint),
                NormalizeFingerprint(deploymentKey.Fingerprint),
                StringComparison.Ordinal))
        {
            return new(
                SshProbeStatus.KeyMismatch,
                "The selected deployment public key changed since enrollment. ZDeployPet blocked the probe. Select and approve the key again before continuing.");
        }

        HostKeyScanResult scan = await ScanHostKeyAsync(
            distribution, target.Host, target.Port, cancellationToken);
        if (!scan.Success || scan.Fingerprint is null || scan.HostKeyLine is null)
            return new(SshProbeStatus.HostUnreachable, scan.Error ?? "The SSH host could not be reached.");

        if (!string.Equals(
                NormalizeFingerprint(scan.Fingerprint),
                NormalizeFingerprint(enrolledHostKey.Fingerprint),
                StringComparison.Ordinal))
        {
            return new(
                SshProbeStatus.HostKeyMismatch,
                "The server host key changed. ZDeployPet blocked the probe. Re-enroll only after independently verifying the new fingerprint.",
                scan.Fingerprint);
        }

        CommandResult result = await RunWslAsync(
            distribution,
            ProbeScript,
            [
                target.Host.Trim(),
                target.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
                target.User.Trim(),
                deploymentKey.PublicKeyPath,
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
                "Authenticated read-only SSH probe passed. No deployment or target file write was performed.",
                scan.Fingerprint);
        }

        string diagnostic = values.TryGetValue("probe_error", out string? probeError)
            ? probeError
            : result.Error;
        if (diagnostic.Contains("MISSING_KEY", StringComparison.OrdinalIgnoreCase))
            return new(SshProbeStatus.MissingKey, "The selected public key or its matching private key is missing in WSL.", scan.Fingerprint);
        if (diagnostic.Contains("Permission denied", StringComparison.OrdinalIgnoreCase) ||
            diagnostic.Contains("no mutual signature", StringComparison.OrdinalIgnoreCase))
            return new(SshProbeStatus.AuthenticationRejected, "The server rejected the configured deployment key or username.", scan.Fingerprint);
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
                ? "The SSH probe failed without a recognized diagnostic."
                : "The SSH probe failed: " + diagnostic.Trim(),
            scan.Fingerprint);
    }

    internal static IReadOnlyList<SshPublicKeyCandidate> ParsePublicKeys(string output)
    {
        List<SshPublicKeyCandidate> result = [];
        string? path = null;
        string? info = null;

        foreach (string raw in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string line = raw.Trim();
            if (line == "--")
            {
                AddCandidate();
                path = null;
                info = null;
                continue;
            }
            if (line.StartsWith("path=", StringComparison.Ordinal)) path = line[5..];
            else if (line.StartsWith("info=", StringComparison.Ordinal)) info = line[5..];
        }
        AddCandidate();
        return result;

        void AddCandidate()
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(info)) return;
            ParsedFingerprint? parsed = ParseFingerprintInfo(info);
            if (parsed is null) return;
            result.Add(new(path, parsed.Fingerprint, parsed.Algorithm, parsed.Comment));
        }
    }

    internal static ParsedFingerprint? ParseFingerprintInfo(string info)
    {
        string[] parts = info.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3) return null;
        string fingerprint = parts[1].Trim();
        if (!fingerprint.StartsWith("SHA256:", StringComparison.Ordinal)) return null;
        string algorithm = parts[^1].Trim('(', ')');
        string? comment = parts.Length > 3
            ? string.Join(" ", parts.Skip(2).Take(parts.Length - 3))
            : null;
        return new(fingerprint, algorithm, string.IsNullOrWhiteSpace(comment) ? null : comment);
    }

    private static Dictionary<string, string> ParseKeyValues(string output)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        foreach (string raw in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            int equals = raw.IndexOf('=');
            if (equals <= 0) continue;
            values[raw[..equals].Trim()] = raw[(equals + 1)..].Trim();
        }
        return values;
    }

    private static string NormalizeFingerprint(string value) => value.Trim();

    private static async Task<CommandResult> RunWslAsync(
        string distribution,
        string script,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        using Process process = new();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "wsl.exe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        process.StartInfo.ArgumentList.Add("--distribution");
        process.StartInfo.ArgumentList.Add(distribution);
        process.StartInfo.ArgumentList.Add("--exec");
        process.StartInfo.ArgumentList.Add("sh");
        process.StartInfo.ArgumentList.Add("-c");
        process.StartInfo.ArgumentList.Add(script);
        process.StartInfo.ArgumentList.Add("zdeploypet-pda1");
        foreach (string argument in arguments) process.StartInfo.ArgumentList.Add(argument);

        try
        {
            process.Start();
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return new(true, process.ExitCode, await outputTask, await errorTask);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new(false, -1, string.Empty, exception.Message);
        }
    }

    internal sealed record ParsedFingerprint(string Fingerprint, string Algorithm, string? Comment);
    private sealed record CommandResult(bool Started, int ExitCode, string Output, string Error);
}
