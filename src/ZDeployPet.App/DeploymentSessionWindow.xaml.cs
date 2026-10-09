using System.Text;
using System.Windows;
using ZDeployPet.Core;

namespace ZDeployPet.App;

public partial class DeploymentSessionWindow : Window
{
    private readonly DeploymentProfile _profile;
    private readonly DeploymentSessionController _controller;
    private bool _busy;

    public DeploymentSessionWindow(
        DeploymentProfile profile,
        DeploymentSessionController controller)
    {
        InitializeComponent();
        _profile = profile;
        _controller = controller;
        ProfileSummaryText.Text = $"Profile: {profile.Name}   •   WSL: {profile.WslDistribution}   •   Lease: 8 hours";

        HelpTipFactory.AttachToButton(
            UnlockButton,
            new HelpTipSpec(
                "Start a bounded deployment-access session.",
                "Unlock creates a dedicated ZDeployPet ssh-agent and opens a trusted WSL/OpenSSH terminal that runs ssh-add with an eight-hour key lifetime.",
                WhenToUse: "Use this after PDA-1 onboarding is complete and before authenticated deployment work that should reuse the approved key without repeated account-password prompts.",
                WhatItDoes: "Only the already-approved deployment key is requested. If that private key has a passphrase, OpenSSH asks for it in the terminal; ZDeployPet never receives or stores the passphrase.",
                Example: "For Millenova, one unlock can make the approved deployment identity available for repeated Hostinger/VPS operations during the lease, while every later live deployment still keeps its own explicit confirmation gate.",
                Safety: "Unlock does not deploy anything. READY is not granted until Check session verifies the exact approved SHA-256 fingerprint inside the dedicated agent."));

        HelpTipFactory.AttachToButton(
            CheckButton,
            new HelpTipSpec(
                "Verify the running ZDeployPet ssh-agent before trusting it.",
                "Check session inspects only the app-owned agent and compares its loaded fingerprints with the profile's approved deployment key.",
                WhenToUse: "Click this after completing ssh-add in the trusted terminal, and whenever the session state needs to be revalidated.",
                WhatItDoes: "READY is shown only when the agent is reachable, the lease is valid, and the exact approved fingerprint is loaded with no foreign key present.",
                Safety: "Missing, foreign, expired, stale, or unverifiable state fails closed instead of silently becoming READY."));

        HelpTipFactory.AttachToButton(
            ProbeTargetsButton,
            new HelpTipSpec(
                "Prove target authentication through the bounded agent.",
                "Probe targets re-validates the READY session, re-checks each enrolled SSH host fingerprint, then authenticates to every configured target using only the app-owned ssh-agent.",
                WhenToUse: "Use this after the session is READY and before deployment work that will rely on the bounded agent.",
                WhatItDoes: "ZDeployPet probes Hostinger/VPS sequentially with password authentication disabled. The ssh command receives the agent socket, not a private-key file path.",
                Example: "Millenova can prove both Hostinger and the VPS are reachable through the same approved bounded identity during one eight-hour session.",
                Safety: "The probe remains non-writing. A changed host key, missing enrollment, rejected agent identity, expired lease, or vanished agent fails closed."));

        HelpTipFactory.AttachToButton(
            LockButton,
            new HelpTipSpec(
                "Immediately remove ZDeployPet deployment access.",
                "Lock verifies the app-owned ssh-agent identity and then terminates that dedicated agent.",
                WhenToUse: "Use this whenever you are finished deploying, are leaving the computer, or see an unexpected session state.",
                WhatItDoes: "The bounded agent is stopped and the current in-memory lease is discarded.",
                Safety: "Lock never deletes your SSH key files. It only removes the temporary agent session that was holding the approved key."));

        RefreshUi();
    }

    private async void Unlock_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        await RunBusyAsync(async () =>
        {
            await _controller.BeginUnlockAsync(_profile);
            ProbeResultsTextBox.Text = "No bounded-agent target probe has run yet.";
            StatusText.Text = _controller.HasOwnedAgent
                ? "Trusted unlock terminal opened — complete ssh-add, then click Check session."
                : "Unlock could not start.";
        });
    }

    private async void Check_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        await RunBusyAsync(async () =>
        {
            await _controller.CheckAsync(_profile);
            StatusText.Text = _controller.State is DeploymentAccessSessionState.Ready or DeploymentAccessSessionState.Expiring
                ? "Deployment session verified."
                : "Deployment session is not READY.";
        });
    }

    private async void ProbeTargets_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        await RunBusyAsync(async () =>
        {
            IReadOnlyList<DeploymentSessionProbeResult> results = await _controller.ProbeReadyTargetsAsync(_profile);
            if (results.Count == 0)
            {
                ProbeResultsTextBox.Text = _controller.Message;
                StatusText.Text = "Bounded-agent probe did not run.";
                return;
            }

            StringBuilder text = new();
            foreach (DeploymentSessionProbeResult item in results)
            {
                text.Append(item.Result.Success ? "PASS" : "FAIL")
                    .Append("  ")
                    .Append(item.Target.Label)
                    .Append("  ")
                    .Append(item.Target.User)
                    .Append('@')
                    .Append(item.Target.Host)
                    .Append(':')
                    .Append(item.Target.Port)
                    .Append("  —  ")
                    .Append(item.Result.Status)
                    .AppendLine();
                if (!item.Result.Success)
                    text.AppendLine("      " + item.Result.Message);
            }

            ProbeResultsTextBox.Text = text.ToString().TrimEnd();
            StatusText.Text = results.All(item => item.Result.Success)
                ? "All bounded-agent target probes passed."
                : "One or more bounded-agent target probes failed.";
        });
    }

    private async void Lock_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        await RunBusyAsync(async () =>
        {
            await _controller.LockAsync();
            ProbeResultsTextBox.Text = "No bounded-agent target probe has run yet.";
            StatusText.Text = "Deployment session locked.";
        });
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async Task RunBusyAsync(Func<Task> action)
    {
        _busy = true;
        RefreshUi();
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            StatusText.Text = "Session operation failed.";
            MessageText.Text = exception.Message;
        }
        finally
        {
            _busy = false;
            RefreshUi();
        }
    }

    private void RefreshUi()
    {
        StateText.Text = _controller.State.ToString().ToUpperInvariant();
        MessageText.Text = _controller.Message;
        ExpiryText.Text = _controller.ExpiresAtUtc is DateTimeOffset expires
            ? "Lease ends: " + expires.ToLocalTime().ToString("g")
            : "No active lease";

        bool ready = _controller.State is DeploymentAccessSessionState.Ready or DeploymentAccessSessionState.Expiring;
        UnlockButton.IsEnabled = !_busy && !ready;
        CheckButton.IsEnabled = !_busy && _controller.HasOwnedAgent;
        ProbeTargetsButton.IsEnabled = !_busy && ready;
        LockButton.IsEnabled = !_busy && _controller.HasOwnedAgent;
    }
}
