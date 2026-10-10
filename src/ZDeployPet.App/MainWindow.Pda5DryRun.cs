using System.Windows;
using System.Windows.Controls;
using ZDeployPet.Core;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private readonly DeploymentExecutionGate _deploymentExecutionGate = new();
    private Button? _dryRunButton;
    private bool _dryRunExecutionBusy;

    private void InitializePda5DryRun()
    {
        if (_dryRunButton is not null || AccessOnboardingButton.Parent is not Panel actions)
            return;

        _dryRunButton = new Button
        {
            Content = "Dry run…",
            Padding = new Thickness(16, 9, 16, 9),
            Visibility = Visibility.Collapsed
        };
        _dryRunButton.Click += DryRun_Click;
        actions.Children.Add(_dryRunButton);

        HelpTipFactory.AttachToButton(
            _dryRunButton,
            new HelpTipSpec(
                "Open the allowlisted dry-run executor.",
                "PDA-5 runs only the configured deployment script after its SHA-256 fingerprint is explicitly approved and rechecked, the bounded deployment-access session is still READY, and all configured target probes have passed.",
                WhenToUse: "Use this after Deployment access shows ON / READY and before any live-deployment work.",
                WhatItDoes: "Lets you select an allowlisted target and bounded release type, then invokes the approved project script in dry-run mode through WSL. After the process ends, ZDeployPet refreshes the structured deployment reporter and treats reporter truth as authoritative.",
                Safety: "There is no arbitrary command field and PDA-5 cannot select live mode. A changed script fingerprint, LOCKED/expired session, unsafe input, or concurrent execution blocks launch."));

        DiscoveryPanel.IsVisibleChanged += (_, _) => RefreshPda5DryRunVisibility();
        RefreshPda5DryRunVisibility();
    }

    private void RefreshPda5DryRunVisibility()
    {
        if (_dryRunButton is null) return;
        _dryRunButton.Visibility = DiscoveryPanel.Visibility == Visibility.Visible && _activeProfile is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
        _dryRunButton.IsEnabled = !_dryRunExecutionBusy;
    }

    private void DryRun_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null || _dryRunExecutionBusy)
            return;

        DryRunWindow window = new(
            _activeProfile,
            _deploymentSession,
            targetsReady: () =>
                _deploymentSession.TargetsReady &&
                _deploymentSession.State is DeploymentAccessSessionState.Ready or DeploymentAccessSessionState.Expiring,
            refreshReports: async () => await RefreshLatestDeploymentReportAsync(),
            executionStateChanged: busy =>
            {
                _dryRunExecutionBusy = busy;
                RefreshPda5DryRunVisibility();
                RefreshPetCompanion();
            },
            executionGate: _deploymentExecutionGate)
        {
            Owner = this
        };
        window.ShowDialog();
    }
}
