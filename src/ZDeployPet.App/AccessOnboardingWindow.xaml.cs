using System.Windows;
using System.Windows.Controls;
using ZDeployPet.Core;
using ZDeployPet.Infrastructure;
using ZDeployPet.WslBridge;

namespace ZDeployPet.App;

public partial class AccessOnboardingWindow : Window
{
    private readonly DeploymentProfile _profile;
    private readonly DeploymentAccessIdentityStore _identityStore;
    private readonly WslSshDiagnostics _diagnostics = new();
    private DeploymentAccessIdentity _identity;
    private HostKeyScanResult? _lastScan;

    public AccessOnboardingWindow(DeploymentProfile profile, string? applicationRoot = null)
    {
        InitializeComponent();
        _profile = profile;
        _identityStore = new DeploymentAccessIdentityStore(applicationRoot);
        _identity = DeploymentAccessIdentity.Empty(profile.Id);
        TargetComboBox.ItemsSource = profile.Targets;
        ProfileText.Text = $"Profile: {profile.Name}   •   WSL: {profile.WslDistribution}   •   Targets: {profile.Targets.Count}";
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _identity = await _identityStore.LoadAsync(_profile.Id);
            if (_profile.Targets.Count > 0) TargetComboBox.SelectedIndex = 0;
            await RefreshKeysAsync();
            RefreshIdentityPresentation();
            StatusText.Text = "Identity metadata loaded locally.";
        }
        catch (Exception exception)
        {
            StatusText.Text = "Could not load deployment access identity.";
            ResultTextBox.Text = exception.Message;
        }
    }

    private async void RefreshKeys_Click(object sender, RoutedEventArgs e) => await RefreshKeysAsync();

    private async Task RefreshKeysAsync()
    {
        SetBusy(true, "Scanning public keys in WSL…");
        try
        {
            IReadOnlyList<SshPublicKeyCandidate> keys = await _diagnostics.ListPublicKeysAsync(_profile.WslDistribution);
            KeyComboBox.ItemsSource = keys;
            if (_identity.DeploymentKey is not null)
            {
                SshPublicKeyCandidate? selected = keys.FirstOrDefault(candidate =>
                    string.Equals(candidate.PublicKeyPath, _identity.DeploymentKey.PublicKeyPath, StringComparison.Ordinal));
                KeyComboBox.SelectedItem = selected;
            }
            if (KeyComboBox.SelectedItem is null && keys.Count == 1) KeyComboBox.SelectedIndex = 0;
            StatusText.Text = keys.Count == 0
                ? "No public keys were found under ~/.ssh in the selected WSL environment."
                : $"Found {keys.Count} public key{(keys.Count == 1 ? "" : "s")}.";
        }
        catch (Exception exception)
        {
            StatusText.Text = "Public-key discovery failed.";
            ResultTextBox.Text = exception.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void UseKey_Click(object sender, RoutedEventArgs e)
    {
        if (KeyComboBox.SelectedItem is not SshPublicKeyCandidate candidate)
        {
            StatusText.Text = "Choose a public key first.";
            return;
        }

        DeploymentKeyIdentity key = new(
            candidate.PublicKeyPath,
            candidate.Fingerprint,
            candidate.Algorithm,
            candidate.Comment);
        try
        {
            await _identityStore.SaveDeploymentKeyAsync(_profile.Id, key);
            _identity = await _identityStore.LoadAsync(_profile.Id);
            RefreshIdentityPresentation();
            StatusText.Text = "Deployment public-key fingerprint approved locally.";
            ResultTextBox.Text =
                "Approved deployment public key\r\n" +
                $"Path: {candidate.PublicKeyPath}\r\n" +
                $"Fingerprint: {candidate.Fingerprint}\r\n" +
                $"Algorithm: {candidate.Algorithm}\r\n\r\n" +
                "No private-key content or passphrase was read or stored.";
        }
        catch (Exception exception)
        {
            StatusText.Text = "Could not save deployment key identity.";
            ResultTextBox.Text = exception.Message;
        }
    }

    private void TargetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _lastScan = null;
        ScannedHostFingerprintText.Text = "Not scanned";
        EnrollHostButton.IsEnabled = false;
        RefreshIdentityPresentation();
    }

    private async void ScanHost_Click(object sender, RoutedEventArgs e)
    {
        if (TargetComboBox.SelectedItem is not DeploymentTarget target)
        {
            StatusText.Text = "Choose a configured target first.";
            return;
        }

        SetBusy(true, $"Scanning host key for {target.Label}…");
        try
        {
            _lastScan = await _diagnostics.ScanHostKeyAsync(
                _profile.WslDistribution, target.Host, target.Port);
            if (!_lastScan.Success || _lastScan.Fingerprint is null)
            {
                ScannedHostFingerprintText.Text = "Not available";
                EnrollHostButton.IsEnabled = false;
                StatusText.Text = "Host-key scan failed.";
                ResultTextBox.Text = _lastScan.Error ?? "The server did not return a host key.";
                return;
            }

            ScannedHostFingerprintText.Text = $"{_lastScan.Fingerprint} ({_lastScan.KeyType})";
            EnrollHostButton.IsEnabled = true;
            StatusText.Text = "Host key scanned — verify it independently before enrolling.";
            ResultTextBox.Text =
                $"Server: {target.Label}\r\n" +
                $"Endpoint: {target.User}@{target.Host}:{target.Port}\r\n" +
                $"Observed fingerprint: {_lastScan.Fingerprint}\r\n" +
                $"Key type: {_lastScan.KeyType}\r\n\r\n" +
                "IMPORTANT: ssh-keyscan proves what the endpoint presented, not that the endpoint is trusted. " +
                "Compare this fingerprint with your hosting provider/server console or another trusted channel before clicking Enroll scanned key.";
        }
        catch (Exception exception)
        {
            StatusText.Text = "Host-key scan failed.";
            ResultTextBox.Text = exception.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void EnrollHost_Click(object sender, RoutedEventArgs e)
    {
        if (TargetComboBox.SelectedItem is not DeploymentTarget target ||
            _lastScan is null || !_lastScan.Success ||
            string.IsNullOrWhiteSpace(_lastScan.Fingerprint) ||
            string.IsNullOrWhiteSpace(_lastScan.KeyType))
        {
            StatusText.Text = "Scan the selected target before enrolling its host key.";
            return;
        }

        MessageBoxResult answer = MessageBox.Show(
            this,
            "Enroll this SSH host fingerprint only if you verified it through a trusted source.\n\n" +
            $"{target.Label}\n{target.Host}:{target.Port}\n{_lastScan.Fingerprint}\n\n" +
            "A later change will be blocked until you explicitly re-enroll.",
            "Enroll verified host key?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        try
        {
            await _identityStore.EnrollHostKeyAsync(
                _profile.Id,
                new TargetHostIdentity(target.Id, _lastScan.Fingerprint, _lastScan.KeyType));
            _identity = await _identityStore.LoadAsync(_profile.Id);
            RefreshIdentityPresentation();
            StatusText.Text = "Expected server host fingerprint enrolled locally.";
            ResultTextBox.Text =
                $"Enrolled {target.Label}\r\n" +
                $"Fingerprint: {_lastScan.Fingerprint}\r\n\r\n" +
                "Future probes fail closed if this fingerprint changes.";
        }
        catch (Exception exception)
        {
            StatusText.Text = "Could not save host identity.";
            ResultTextBox.Text = exception.Message;
        }
    }

    private async void Probe_Click(object sender, RoutedEventArgs e)
    {
        if (TargetComboBox.SelectedItem is not DeploymentTarget target)
        {
            StatusText.Text = "Choose a configured target first.";
            return;
        }
        if (_identity.DeploymentKey is null)
        {
            StatusText.Text = "Approve a deployment public key before probing.";
            return;
        }
        TargetHostIdentity? hostKey = _identity.FindHostKey(target.Id);
        if (hostKey is null)
        {
            StatusText.Text = "Enroll the target host fingerprint before probing.";
            return;
        }

        SetBusy(true, $"Running non-writing probe for {target.Label}…");
        try
        {
            SshProbeResult result = await _diagnostics.ProbeAsync(
                _profile.WslDistribution,
                target,
                _identity.DeploymentKey,
                hostKey);
            StatusText.Text = result.Success ? "Authenticated probe passed." : "Probe blocked or failed.";
            ResultTextBox.Text =
                $"Status: {result.Status}\r\n" +
                $"Target: {target.Label} ({target.User}@{target.Host}:{target.Port})\r\n" +
                $"Approved deployment key: {_identity.DeploymentKey.Fingerprint}\r\n" +
                $"Enrolled host key: {hostKey.Fingerprint}\r\n" +
                (result.ObservedHostFingerprint is null ? string.Empty : $"Observed host key: {result.ObservedHostFingerprint}\r\n") +
                "\r\n" + result.Message;
        }
        catch (Exception exception)
        {
            StatusText.Text = "Probe failed.";
            ResultTextBox.Text = exception.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void RefreshIdentityPresentation()
    {
        ApprovedKeyFingerprintText.Text = _identity.DeploymentKey is null
            ? "Not selected"
            : $"{_identity.DeploymentKey.Fingerprint} ({_identity.DeploymentKey.Algorithm})";

        if (TargetComboBox.SelectedItem is not DeploymentTarget target)
        {
            TargetEndpointText.Text = string.Empty;
            EnrolledHostFingerprintText.Text = "Not enrolled";
            return;
        }

        TargetEndpointText.Text = $"{target.User}@{target.Host}:{target.Port}";
        TargetHostIdentity? hostKey = _identity.FindHostKey(target.Id);
        EnrolledHostFingerprintText.Text = hostKey is null
            ? "Not enrolled"
            : $"{hostKey.Fingerprint} ({hostKey.KeyType})";
    }

    private void SetBusy(bool busy, string? status = null)
    {
        RefreshKeysButton.IsEnabled = !busy;
        UseKeyButton.IsEnabled = !busy;
        ScanHostButton.IsEnabled = !busy;
        ProbeButton.IsEnabled = !busy;
        if (busy) EnrollHostButton.IsEnabled = false;
        else EnrollHostButton.IsEnabled = _lastScan?.Success == true;
        if (!string.IsNullOrWhiteSpace(status)) StatusText.Text = status;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
