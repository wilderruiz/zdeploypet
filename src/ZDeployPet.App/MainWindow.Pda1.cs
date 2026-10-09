using System.Windows;
using System.Windows.Controls;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda1ShellHooked;
    private bool _discoveryActionHelpAttached;
    private Button? _gitSafetyButton;
    private Button? _deploymentSessionButton;
    private readonly DeploymentSessionController _deploymentSession = new();

    private void InitializePda1Shell()
    {
        if (!_pda1ShellHooked)
        {
            _pda1ShellHooked = true;
            DiscoveryPanel.IsVisibleChanged += (_, _) => RefreshPda1ActionVisibility();
        }

        EnsurePdaActionButtons();
        AttachDiscoveryActionHelp();
        RefreshPda1ActionVisibility();
    }

    private void EnsurePdaActionButtons()
    {
        if (AccessOnboardingButton.Parent is not StackPanel actions) return;

        if (_deploymentSessionButton is null)
        {
            _deploymentSessionButton = new Button
            {
                Content = "Session…",
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(16, 9, 16, 9),
                Visibility = Visibility.Collapsed
            };
            _deploymentSessionButton.Click += DeploymentSession_Click;
            int accessIndex = actions.Children.IndexOf(AccessOnboardingButton);
            actions.Children.Insert(accessIndex + 1, _deploymentSessionButton);
        }

        if (_gitSafetyButton is null)
        {
            _gitSafetyButton = new Button
            {
                Content = "Git safety…",
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(16, 9, 16, 9),
                Visibility = Visibility.Collapsed
            };
            _gitSafetyButton.Click += GitSafety_Click;
            int sessionIndex = actions.Children.IndexOf(_deploymentSessionButton);
            actions.Children.Insert(sessionIndex + 1, _gitSafetyButton);
        }
    }

    private void AttachDiscoveryActionHelp()
    {
        if (_discoveryActionHelpAttached || _gitSafetyButton is null || _deploymentSessionButton is null) return;
        _discoveryActionHelpAttached = true;

        HelpTipFactory.AttachToButton(
            AccessOnboardingButton,
            new HelpTipSpec(
                "Configure deployment access without storing passwords.",
                "Use Deployment access to prepare the SSH identity and host trust that ZDeployPet will later use for safe deployments.",
                WhenToUse: "Use this after the deployment profile is validated and before the first authenticated server probe or deployment.",
                WhatItDoes: "ZDeployPet discovers public SSH keys in the selected WSL environment, shows their SHA-256 fingerprints, lets you inspect and explicitly enroll server host keys, and can run a non-writing authenticated probe.",
                Example: "A project may deploy its website to shared hosting and its backend to a VPS. Configure the deployment key and trusted host fingerprint for each server here before ZDeployPet is allowed to authenticate.",
                Safety: "ZDeployPet does not store server passwords, private-key contents, or private-key passphrases. Changed key fingerprints or changed server host keys fail closed instead of being silently accepted.",
                Tip: "This step establishes identity and trust only. It does not deploy project files."));

        HelpTipFactory.AttachToButton(
            _deploymentSessionButton,
            new HelpTipSpec(
                "Unlock the approved deployment identity for a bounded session.",
                "Session starts a dedicated ZDeployPet-owned ssh-agent and lets OpenSSH load the approved deployment key for up to eight hours.",
                WhenToUse: "Use this after Deployment access onboarding and target probes are complete, before repeated authenticated deployment operations.",
                WhatItDoes: "ZDeployPet opens a trusted WSL/OpenSSH ssh-add prompt if needed, then verifies the loaded SHA-256 fingerprint before declaring the session READY.",
                Example: "Unlock once for Millenova, then reuse that approved key for Hostinger and VPS operations during the valid lease instead of repeatedly entering account passwords.",
                Safety: "ZDeployPet never receives the private-key passphrase. Foreign keys, missing keys, expired leases, or an unverifiable agent fail closed. Closing ZDeployPet locks the session."));

        HelpTipFactory.AttachToButton(
            _gitSafetyButton,
            new HelpTipSpec(
                "Check that deployment private keys cannot accidentally enter Git.",
                "Git safety scans tracked files and unignored untracked files for common private-key material and can add a narrow ZDeployPet-managed .gitignore safety block.",
                WhenToUse: "Run this before push/release-oriented work and after changing deployment credentials or local repository files.",
                WhatItDoes: "The scan is read-only. The optional .gitignore action updates only a clearly marked ZDeployPet block and preserves the rest of the project's ignore rules.",
                Example: "The private zdeploypet_millenova_ed25519 key stays in WSL ~/.ssh. If a copy ever appears under the Git project, this check reports and blocks readiness.",
                Safety: "Adding .gitignore does not make an already tracked secret safe and does not delete files. Any detected private-key material must be removed from the repository explicitly.",
                Tip: "Public .pub files are not blanket-ignored because they are not secret material."));

        HelpTipFactory.AttachToButton(
            EditProfileButton,
            new HelpTipSpec(
                "Return to the saved deployment profile and change its layout.",
                "Use Edit profile when the local project, deployment runner, server list, or production destination mappings need to change.",
                WhenToUse: "Use this when a server address changes, you add another target, move a private file above the web root, change the approved deployment script, or otherwise change where project parts belong in production.",
                WhatItDoes: "ZDeployPet returns to the five-section profile editor. Saving again replaces that profile's local JSON snapshot with the current UI state, including incomplete work.",
                Example: "Your Git repository can stay as one clean project folder while production uses public_html for the website, a private directory for configuration, and a separate VPS path for backend services. Edit profile is where those mappings are maintained.",
                Safety: "Editing the profile does not contact any server and does not deploy anything. Live deployment remains a separate, explicitly confirmed action.",
                Tip: "Saved profile JSON is local machine state and is not added to your Git repository."));

        HelpTipFactory.AttachToButton(
            DiscoverButton,
            new HelpTipSpec(
                "Inspect the deployment environment without changing it.",
                "Run discovery checks the active profile against the local/WSL environment and gathers the information ZDeployPet needs before live deployment features are enabled.",
                WhenToUse: "Run this after saving a valid profile, after changing WSL or project paths, or whenever you want to confirm that the configured deployment runner and environment still match the profile.",
                WhatItDoes: "Discovery performs read-only checks and reports what ZDeployPet can see from the active local profile. It prepares evidence for later deployment steps rather than modifying production.",
                Example: "For a project with shared hosting plus a VPS, discovery can confirm the local project/WSL context and approved runner before you proceed to authenticated server diagnostics.",
                Safety: "Discovery is read-only. It does not upload files, execute the deployment runner, alter remote folders, or perform a production deployment.",
                Tip: "If discovery reports a mismatch, edit the profile instead of forcing past the warning."));
    }

    private void RefreshPda1ActionVisibility()
    {
        Visibility actionVisibility =
            DiscoveryPanel.Visibility == Visibility.Visible && _activeProfile is not null
                ? Visibility.Visible
                : Visibility.Collapsed;
        AccessOnboardingButton.Visibility = actionVisibility;
        if (_deploymentSessionButton is not null) _deploymentSessionButton.Visibility = actionVisibility;
        if (_gitSafetyButton is not null) _gitSafetyButton.Visibility = actionVisibility;
    }

    private void AccessOnboarding_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null)
        {
            StatusText.Text = "Save and validate a deployment profile before configuring deployment access.";
            return;
        }

        AccessOnboardingWindow window = new(_activeProfile)
        {
            Owner = this
        };
        window.ShowDialog();
    }

    private void DeploymentSession_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null)
        {
            StatusText.Text = "Save and validate a deployment profile before opening a deployment session.";
            return;
        }

        DeploymentSessionWindow window = new(_activeProfile, _deploymentSession)
        {
            Owner = this
        };
        window.ShowDialog();
    }

    private void GitSafety_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null)
        {
            StatusText.Text = "Save and validate a deployment profile before running Git safety checks.";
            return;
        }

        GitSafetyWindow window = new(_activeProfile)
        {
            Owner = this
        };
        window.ShowDialog();
    }
}
