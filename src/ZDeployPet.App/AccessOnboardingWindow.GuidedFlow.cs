using ZDeployPet.Core;

namespace ZDeployPet.App;

public partial class AccessOnboardingWindow
{
    private bool _guidedBusy;
    private string? _installerLaunchedTargetId;

    private void KeyComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) =>
        UpdateGuidedActionState();

    private void UpdateGuidedActionState()
    {
        if (_guidedBusy)
        {
            RefreshKeysButton.IsEnabled = false;
            CreateKeyButton.IsEnabled = false;
            UseKeyButton.IsEnabled = false;
            ScanHostButton.IsEnabled = false;
            EnrollHostButton.IsEnabled = false;
            InstallKeyButton.IsEnabled = false;
            ProbeButton.IsEnabled = false;
            return;
        }

        RefreshKeysButton.IsEnabled = false;
        CreateKeyButton.IsEnabled = false;
        UseKeyButton.IsEnabled = false;
        ScanHostButton.IsEnabled = false;
        EnrollHostButton.IsEnabled = false;
        InstallKeyButton.IsEnabled = false;
        ProbeButton.IsEnabled = false;

        if (_identity.DeploymentKey is null)
        {
            if (KeyComboBox.SelectedItem is SshPublicKeyCandidate)
                UseKeyButton.IsEnabled = true;
            else
                CreateKeyButton.IsEnabled = true;
            return;
        }

        if (TargetComboBox.SelectedItem is not DeploymentTarget target)
            return;

        TargetHostIdentity? enrolled = _identity.FindHostKey(target.Id);
        if (enrolled is null)
        {
            if (_lastScan?.Success == true)
                EnrollHostButton.IsEnabled = true;
            else
                ScanHostButton.IsEnabled = true;
            return;
        }

        if (_lastScan?.Success == true &&
            !string.IsNullOrWhiteSpace(_lastScan.Fingerprint) &&
            !string.Equals(_lastScan.Fingerprint.Trim(), enrolled.Fingerprint.Trim(), StringComparison.Ordinal))
        {
            EnrollHostButton.IsEnabled = true;
            return;
        }

        if (string.Equals(_installerLaunchedTargetId, target.Id, StringComparison.OrdinalIgnoreCase))
            ProbeButton.IsEnabled = true;
        else
            InstallKeyButton.IsEnabled = true;
    }
}
