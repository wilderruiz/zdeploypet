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
        InitializeHelpTips();
    }

    private void InitializeHelpTips()
    {
        HelpTipFactory.AttachToButton(UseKeyButton, new HelpTipSpec(
            "Approve the deployment public key for this profile.",
            "Choose the public half of the SSH key ZDeployPet should expect when it later authenticates to deployment targets.",
            WhenToUse: "Use this once you have selected the dedicated deployment key you intend to use for this profile. Refresh keys first if the expected key is not listed.",
            WhatItDoes: "ZDeployPet records the public-key path, algorithm and SHA-256 fingerprint. Future probes re-check the fingerprint and fail closed if the key changes unexpectedly.",
            Example: "A project can use one dedicated SSH key for both a shared-hosting target and a VPS. Approve that public key here, then enroll each server's host fingerprint separately.",
            Safety: "ZDeployPet does not read, copy or store private-key contents or the private-key passphrase. This step does not contact a server and does not deploy anything.",
            Tip: "If the fingerprint changes because you intentionally replaced the key, review the new key independently and approve it again."));

        HelpTipFactory.AttachToButton(ScanHostButton, new HelpTipSpec(
            "Inspect the SSH host identity presented by the selected server.",
            "Scanning retrieves the server's public SSH host key so you can compare its fingerprint with a trusted source before enrollment.",
            WhenToUse: "Run this for each configured target before the first authenticated probe, or whenever a server was rebuilt or its SSH host keys were intentionally rotated.",
            WhatItDoes: "ZDeployPet asks the configured endpoint for its SSH host key and shows the observed fingerprint. The scan itself does not establish trust.",
            Example: "If your profile has a shared-hosting server and a VPS, scan each target separately. Compare the shown fingerprint with the hosting control panel, provider documentation, server console, or another trusted channel.",
            Safety: "A successful scan only proves what the endpoint presented at that moment. Do not enroll a fingerprint merely because the scan succeeded.",
            Tip: "The Enroll scanned key action stays separate so observing a host key never silently makes it trusted."));

        HelpTipFactory.AttachToButton(EnrollHostButton, new HelpTipSpec(
            "Trust the verified SSH host fingerprint for this target.",
            "Enroll only after you have independently confirmed that the scanned fingerprint really belongs to the intended server.",
            WhenToUse: "Use this after Scan host key and after comparing the fingerprint through a trusted source.",
            WhatItDoes: "ZDeployPet stores the expected host fingerprint locally for this profile and target. Future probes compare the observed host identity against this exact value.",
            Example: "After verifying a VPS fingerprint in its provider console, enroll it here. If the server later presents a different key, ZDeployPet blocks the probe until you explicitly re-enroll.",
            Safety: "Host-key changes are never accepted silently. Re-enrollment is an explicit trust decision and should only follow a known server rebuild or intentional key rotation.",
            Tip: "If a fingerprint changes unexpectedly, stop and investigate instead of re-enrolling it just to make the warning disappear."));

        HelpTipFactory.AttachToButton(ProbeButton, new HelpTipSpec(
            "Prove authenticated SSH reachability without deploying anything.",
            "The probe verifies the approved deployment key, the enrolled server host fingerprint and non-interactive SSH authentication for the selected target.",
            WhenToUse: "Run this after approving the deployment public key and enrolling the selected target's verified host fingerprint. Repeat it when diagnosing connection, username, key or host-identity problems.",
            WhatItDoes: "ZDeployPet performs an allowlisted non-writing SSH check and reports whether identity verification and authentication succeeded.",
            Example: "For a profile with shared hosting and a VPS, probe each target independently. One can pass while another reports a username, key, connectivity or host-key problem.",
            Safety: "The probe does not upload files, invoke the deployment script, modify remote folders or run arbitrary operator commands. Password authentication is not used.",
            Tip: "A passed probe means deployment access identity is ready for the next phase; it does not authorize or start a deployment."));
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
        _installerLaunchedTargetId = null;
        ScannedHostFingerprintText.Text = "Not scanned";
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
                StatusText.Text = "Host-key scan failed.";
                ResultTextBox.Text = _lastScan.Error ?? "The server did not return a host key.";
                return;
            }

            ScannedHostFingerprintText.Text = $"{_lastScan.Fingerprint} ({_lastScan.KeyType})";
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
            _installerLaunchedTargetId = null;
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
            if (!result.Success && result.Status == SshProbeStatus.AuthenticationRejected)
                _installerLaunchedTargetId = null;
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
            UpdateGuidedActionState();
            return;
        }

        TargetEndpointText.Text = $"{target.User}@{target.Host}:{target.Port}";
        TargetHostIdentity? hostKey = _identity.FindHostKey(target.Id);
        EnrolledHostFingerprintText.Text = hostKey is null
            ? "Not enrolled"
            : $"{hostKey.Fingerprint} ({hostKey.KeyType})";
        UpdateGuidedActionState();
    }

    private void SetBusy(bool busy, string? status = null)
    {
        _guidedBusy = busy;
        UpdateGuidedActionState();
        if (!string.IsNullOrWhiteSpace(status)) StatusText.Text = status;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
