using System.Diagnostics;
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

        echo "ZDeployPet is re-checking the SSH host identity before installing the public key..."
        line="$(ssh-keyscan -T 6 -p "$port" "$host" 2>/dev/null | head -n 1 || true)"
        if [ -z "$line" ]; then
          echo "ZDeployPet blocked installation: the server did not return an SSH host key."
          echo "Press Enter to close."
          read _
          exit 22
        fi

        info="$(echo "$line" | ssh-keygen -lf - -E sha256 2>/dev/null || true)"
        observed_fingerprint="$(echo "$info" | awk '{print $2}')"
        if [ -z "$observed_fingerprint" ] || [ "$observed_fingerprint" != "$expected_fingerprint" ]; then
          echo "ZDeployPet blocked installation because the server host fingerprint does not match the enrolled fingerprint."
          echo "Expected: $expected_fingerprint"
          echo "Observed: ${observed_fingerprint:-unavailable}"
          echo "Do not bypass this warning. Re-scan and independently verify the server identity first."
          echo "Press Enter to close."
          read _
          exit 23
        fi

        known_hosts="$(mktemp)"
        trap 'rm -f "$known_hosts"' EXIT HUP INT TERM
        echo "$line" > "$known_hosts"
        chmod 600 "$known_hosts"

        echo
        echo "Host identity verified: $expected_fingerprint"
        echo "Installing only the approved PUBLIC key on $user@$host:$port."
        echo "Your SSH account password may be requested once by ssh-copy-id."
        echo "ZDeployPet does not receive or store that password."
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
          echo "ZDeployPet public-key installation completed. Return to ZDeployPet and run Probe selected target."
        else
          echo "ZDeployPet public-key installation did not complete. ssh-copy-id exit code: $code"
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

        HostKeyScanResult scan = await _diagnostics.ScanHostKeyAsync(
            distribution,
            target.Host,
            target.Port,
            cancellationToken);
        if (!scan.Success || string.IsNullOrWhiteSpace(scan.Fingerprint))
            return new(false, scan.Error ?? "The selected target did not return a usable SSH host key.");

        if (!string.Equals(
                scan.Fingerprint.Trim(),
                enrolledHostKey.Fingerprint.Trim(),
                StringComparison.Ordinal))
        {
            return new(false, "The selected target's SSH host fingerprint no longer matches the enrolled fingerprint. Installation was blocked.");
        }

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
        process.StartInfo.ArgumentList.Add(InstallScript);
        process.StartInfo.ArgumentList.Add("zdeploypet-install-key");
        process.StartInfo.ArgumentList.Add(target.Host.Trim());
        process.StartInfo.ArgumentList.Add(target.Port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        process.StartInfo.ArgumentList.Add(target.User.Trim());
        process.StartInfo.ArgumentList.Add(deploymentKey.PublicKeyPath);
        process.StartInfo.ArgumentList.Add(enrolledHostKey.Fingerprint.Trim());

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
            "The interactive SSH public-key installer was opened. Enter the target account password only in that SSH/WSL prompt if requested, then return to ZDeployPet and run the non-writing probe.");
    }
}
