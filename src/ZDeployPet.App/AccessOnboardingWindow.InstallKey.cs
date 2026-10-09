using System.Windows;
using ZDeployPet.Core;
using ZDeployPet.WslBridge;

namespace ZDeployPet.App;

public partial class AccessOnboardingWindow
{
    private readonly WslSshKeyInstaller _keyInstaller = new();

    private void InstallKeyHelpButton_Loaded(object sender, RoutedEventArgs e)
    {
        HelpTipFactory.AttachToButton(InstallKeyHelpButton, new HelpTipSpec(
            "Install only the approved ZDeployPet public key on the selected target.",
            "This is the one-time authorization step that lets later SSH probes and deployments use the dedicated key instead of repeatedly asking for the server account password.",
            WhenToUse: "Use this only after approving the ZDeployPet deployment key and independently verifying and enrolling the selected server's SSH host fingerprint.",
            WhatItDoes: "ZDeployPet re-checks both the local deployment-key fingerprint and the live server host fingerprint, then opens the standard WSL/SSH key installer. ssh-copy-id adds the public key to the remote account's authorized_keys file.",
            Example: "For Hostinger, ZDeployPet can install the Millenova public key for the configured SSH account on port 65002. The SSH password may be requested once in the trusted terminal prompt; future key-based access should not need it.",
            Safety: "The private key never leaves WSL. ZDeployPet does not receive or store the SSH account password. Installation is blocked if the approved local key or enrolled server fingerprint has changed.",
            Tip: "After the installer reports success, return here and run Probe selected target. The probe confirms that key authentication works without deploying files."));
    }

    private async void InstallKey_Click(object sender, RoutedEventArgs e)
    {
        if (TargetComboBox.SelectedItem is not DeploymentTarget target)
        {
            StatusText.Text = "Choose a configured target first.";
            return;
        }

        if (_identity.DeploymentKey is null)
        {
            StatusText.Text = "Approve the dedicated ZDeployPet deployment key before installing it.";
            ResultTextBox.Text = "Use the dedicated deployment key section first, then return to Install public key.";
            return;
        }

        TargetHostIdentity? hostKey = _identity.FindHostKey(target.Id);
        if (hostKey is null)
        {
            StatusText.Text = "Verify and enroll the selected target host fingerprint before installing a public key.";
            ResultTextBox.Text = "Scan host key, independently verify the fingerprint, and click Enroll scanned key first.";
            return;
        }

        MessageBoxResult answer = MessageBox.Show(
            this,
            "ZDeployPet will install ONLY the approved public key on this SSH account.\n\n" +
            $"Target: {target.Label}\n" +
            $"Account: {target.User}@{target.Host}:{target.Port}\n" +
            $"Public-key fingerprint: {_identity.DeploymentKey.Fingerprint}\n" +
            $"Enrolled server fingerprint: {hostKey.Fingerprint}\n\n" +
            "A WSL/SSH terminal will open. The server account password may be requested once by ssh-copy-id. " +
            "Type it only in that terminal prompt; ZDeployPet does not receive or store it.\n\n" +
            "Continue?",
            "Install ZDeployPet public key?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        InstallKeyButton.IsEnabled = false;
        StatusText.Text = $"Re-checking identities before opening the installer for {target.Label}…";
        try
        {
            SshPublicKeyInstallLaunchResult result = await _keyInstaller.LaunchAsync(
                _profile.WslDistribution,
                target,
                _identity.DeploymentKey,
                hostKey);

            StatusText.Text = result.Success
                ? "Interactive public-key installer opened."
                : "Public-key installation was blocked before any remote change.";
            ResultTextBox.Text = result.Message;
        }
        catch (Exception exception)
        {
            StatusText.Text = "Could not start public-key installation.";
            ResultTextBox.Text = exception.Message;
        }
        finally
        {
            InstallKeyButton.IsEnabled = true;
        }
    }
}
