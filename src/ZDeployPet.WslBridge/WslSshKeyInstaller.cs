using System.Diagnostics;
using System.Security.Cryptography;
using ZDeployPet.Core;

namespace ZDeployPet.WslBridge;

public sealed record SshPublicKeyInstallLaunchResult(
    bool Success,
    string Message);

public sealed class WslSshKeyInstaller
{
    private const string InstallScript = """
        set -u
        host="$1"
        port="$2"
        user="$3"
        public_key="$4"
        expected_fingerprint="$5"
        host_key_line="$6"
        target_label="$7"

        if [ ! -f "$public_key" ]; then
          echo "ZDeployPet blocked installation: the approved public key is missing."
          echo "Press Enter to close."
          read _
          exit 20
        fi

        if [ ! -x /usr/bin/ssh-copy-id ]; then
          echo "ZDeployPet cannot continue because /usr/bin/ssh-copy-id is not available in this WSL distribution."
          echo "Press Enter to close."
          read _
          exit 21
        fi

        known_hosts="$(mktemp)"
        trap 'rm -f "$known_hosts"' EXIT HUP INT TERM
        echo "$host_key_line" > "$known_hosts"
        chmod 600 "$known_hosts"

        echo
        echo "============================================================"
        echo " ZDEPLOYPET PASSWORD PROMPT — CHECK THE TARGET BEFORE TYPING"
        echo "============================================================"
        echo "Friendly server name : $target_label"
        echo "SSH username         : $user"
        echo "SSH host             : $host"
        echo "SSH port             : $port"
        echo "Account              : $user@$host:$port"
        echo
        echo "Use the SSH account password for '$target_label'."
        echo "Do NOT enter the password for another configured target."
        echo "For example, if this says Hostinger, do not enter the VPS/root password."
        echo "ZDeployPet does not receive or store the password you type here."
        echo "============================================================"
        echo
        echo "Host identity verified by ZDeployPet: $expected_fingerprint"
        echo "Installing only the approved PUBLIC key on $user@$host:$port."
        echo "The password may be requested once below by ssh-copy-id."
        echo

        set +e
        ssh-copy-id \
          -i "$public_key" \
          -p "$port" \
          -o StrictHostKeyChecking=yes \
          -o UserKnownHostsFile="$known_hosts" \
          -o GlobalKnownHostsFile=/dev/null \
          "$user@$host"
        code=$?
        set -e

        echo
        if [ "$code" -eq 0 ]; then
          echo "ZDeployPet public-key installation completed for '$target_label'. Return to ZDeployPet and run Probe selected target."
        else
          echo "ZDeployPet public-key installation did not complete for '$target_label'. ssh-copy-id exit code: $code"
        fi
        echo "Press Enter to close."
        read _
        exit "$code"
        """;

    private readonly WslSshDiagnostics _diagnostics = new();

    public async Task<SshPublicKeyInstallLaunchResult> LaunchAsync(
        string distribution,
        DeploymentTarget target,
        DeploymentKeyIdentity deploymentKey,
        TargetHostIdentity enrolledHostKey,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SshPublicKeyCandidate> keys =
            await _diagnostics.ListPublicKeysAsync(distribution, cancellationToken);
        SshPublicKeyCandidate? currentKey = keys.FirstOrDefault(candidate =>
            string.Equals(candidate.PublicKeyPath, deploymentKey.PublicKeyPath, StringComparison.Ordinal));
        if (currentKey is null)
            return new(false, "The approved deployment public key no longer exists in WSL.");

        if (!string.Equals(
                currentKey.Fingerprint.Trim(),
                deploymentKey.Fingerprint.Trim(),
                StringComparison.Ordinal))
        {
            return new(false, "The approved deployment key fingerprint changed. Approve the key again before installing it on a target.");
        }

        HostKeyVerificationResult hostVerification = await VerifyEnrolledHostKeyAsync(
            distribution,
            target,
            enrolledHostKey,
            cancellationToken);
        if (!hostVerification.Success || string.IsNullOrWhiteSpace(hostVerification.HostKeyLine))
            return new(false, hostVerification.Message);

        string normalizedInstallScript = InstallScript
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
        process.StartInfo.ArgumentList.Add(distribution);
        process.StartInfo.ArgumentList.Add("--exec");
        process.StartInfo.ArgumentList.Add("/bin/sh");
        process.StartInfo.ArgumentList.Add("-c");
        process.StartInfo.ArgumentList.Add(normalizedInstallScript);
        process.StartInfo.ArgumentList.Add("zdeploypet-install-key");
        process.StartInfo.ArgumentList.Add(target.Host.Trim());
        process.StartInfo.ArgumentList.Add(target.Port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        process.StartInfo.ArgumentList.Add(target.User.Trim());
        process.StartInfo.ArgumentList.Add(deploymentKey.PublicKeyPath);
        process.StartInfo.ArgumentList.Add(enrolledHostKey.Fingerprint.Trim());
        process.StartInfo.ArgumentList.Add(hostVerification.HostKeyLine);
        process.StartInfo.ArgumentList.Add(target.Label.Trim());

        try
        {
            if (!process.Start())
                return new(false, "Windows could not start the interactive WSL key installer.");
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new(false, "Windows could not start the interactive WSL key installer: " + exception.Message);
        }

        return new(
            true,
            $"The enrolled SSH host identity for '{target.Label}' was re-verified and the interactive public-key installer was opened. Enter only the SSH account password for '{target.Label}' in that terminal prompt if requested, then return to ZDeployPet and run the non-writing probe.");
    }

    private static async Task<HostKeyVerificationResult> VerifyEnrolledHostKeyAsync(
        string distribution,
        DeploymentTarget target,
        TargetHostIdentity enrolledHostKey,
        CancellationToken cancellationToken)
    {
        string? scanType = MapSshKeyscanType(enrolledHostKey.KeyType);
        if (scanType is null)
        {
            return new(false, null,
                $"ZDeployPet does not know how to re-scan the enrolled SSH host key type '{enrolledHostKey.KeyType}'. Re-scan the target before installation.");
        }

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
        process.StartInfo.ArgumentList.Add("/usr/bin/ssh-keyscan");
        process.StartInfo.ArgumentList.Add("-T");
        process.StartInfo.ArgumentList.Add("6");
        process.StartInfo.ArgumentList.Add("-p");
        process.StartInfo.ArgumentList.Add(target.Port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        process.StartInfo.ArgumentList.Add("-t");
        process.StartInfo.ArgumentList.Add(scanType);
        process.StartInfo.ArgumentList.Add(target.Host.Trim());

        try
        {
            process.Start();
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            string output = await outputTask;
            string error = await errorTask;

            if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(output))
            {
                return new(false, null,
                    string.IsNullOrWhiteSpace(error)
                        ? "The selected target did not return the enrolled SSH host-key type."
                        : "SSH host-key re-scan failed: " + error.Trim());
            }

            string? line = output
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .FirstOrDefault(item => item.Length > 0 && !item.StartsWith('#'));
            if (string.IsNullOrWhiteSpace(line))
                return new(false, null, "The selected target did not return the enrolled SSH host-key type.");

            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                return new(false, null, "The selected target returned an SSH host key that ZDeployPet could not parse.");

            byte[] keyBlob;
            try
            {
                keyBlob = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return new(false, null, "The selected target returned an SSH host key with invalid key data.");
            }

            string observedFingerprint = "SHA256:" +
                Convert.ToBase64String(SHA256.HashData(keyBlob)).TrimEnd('=');
            if (!string.Equals(
                    observedFingerprint,
                    enrolledHostKey.Fingerprint.Trim(),
                    StringComparison.Ordinal))
            {
                return new(false, null,
                    "The selected target's SSH host fingerprint no longer matches the enrolled fingerprint. " +
                    $"Expected {enrolledHostKey.Fingerprint.Trim()}, observed {observedFingerprint}. Installation was blocked.");
            }

            return new(true, line, "Host identity verified.");
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new(false, null, "Windows could not run the SSH host-key verification: " + exception.Message);
        }
    }

    private static string? MapSshKeyscanType(string keyType) => keyType.Trim().ToUpperInvariant() switch
    {
        "ED25519" or "SSH-ED25519" => "ed25519",
        "ECDSA" or "ECDSA-SHA2-NISTP256" => "ecdsa",
        "RSA" or "SSH-RSA" => "rsa",
        _ => null
    };

    private sealed record HostKeyVerificationResult(
        bool Success,
        string? HostKeyLine,
        string Message);
}
