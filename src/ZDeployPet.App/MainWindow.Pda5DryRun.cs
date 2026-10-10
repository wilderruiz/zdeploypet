using System.Windows;
using System.Windows.Controls;
using ZDeployPet.Core;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private readonly DeploymentExecutionGate _deploymentExecutionGate = new();
    private Button? _dryRunButton;
    private DryRunWindow? _dryRunWindow;
    private bool _dryRunExecutionBusy;

    private void InitializePda5DryRun()
    {
        if (_dryRunButton is not null || _operatorAccessCard?.Child is not Grid accessGrid)
            return;

        if (accessGrid.RowDefinitions.Count == 0)
            accessGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        accessGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _dryRunButton = new Button
        {
            Content = "Dry run…",
            Padding = new Thickness(16, 9, 16, 9),
            Margin = new Thickness(0, 12, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            Visibility = Visibility.Visible,
            IsEnabled = false
        };
        _dryRunButton.Click += DryRun_Click;
        Grid.SetRow(_dryRunButton, 1);
        Grid.SetColumnSpan(_dryRunButton, 2);
        accessGrid.Children.Add(_dryRunButton);

        HelpTipFactory.AttachToButton(
            _dryRunButton,
            new HelpTipSpec(
                "Open the allowlisted dry-run executor.",
                "PDA-5 runs only the configured deployment script after its SHA-256 fingerprint is explicitly approved and rechecked, the bounded deployment-access session is still READY, and all configured target probes have passed.",
                WhenToUse: "The control stays visible below Deployment access. It becomes available only after Deployment access reaches ON / READY.",
                WhatItDoes: "Lets you select an allowlisted target and bounded release type, then invokes the approved project script in dry-run mode through WSL. The dry-run window is modeless, so Console / Activity and the rest of the main shell remain usable while it runs.",
                Safety: "There is no arbitrary command field and PDA-5 cannot select live mode. A changed script fingerprint, LOCKED/expired session, unsafe input, concurrent execution, or failed target verification blocks launch."));

        DiscoveryPanel.IsVisibleChanged += (_, _) => RefreshPda5DryRunVisibility();
        RefreshPda5DryRunVisibility();
    }

    private void RefreshPda5DryRunVisibility()
    {
        if (_dryRunButton is null) return;

        bool shellVisible = DiscoveryPanel.Visibility == Visibility.Visible && _activeProfile is not null;
        bool accessReady =
            _deploymentSession.TargetsReady &&
            _deploymentSession.State is DeploymentAccessSessionState.Ready or DeploymentAccessSessionState.Expiring;

        _dryRunButton.Visibility = shellVisible ? Visibility.Visible : Visibility.Collapsed;
        _dryRunButton.IsEnabled = shellVisible && accessReady && !_dryRunExecutionBusy;
        _dryRunButton.Content = _dryRunExecutionBusy ? "Dry run running…" : "Dry run…";
    }

    private void DryRun_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null || _dryRunExecutionBusy || !_dryRunButton!.IsEnabled)
            return;

        if (_dryRunWindow is { IsVisible: true })
        {
            if (_dryRunWindow.WindowState == WindowState.Minimized)
                _dryRunWindow.WindowState = WindowState.Normal;
            _dryRunWindow.Activate();
            return;
        }

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

        _dryRunWindow = window;
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_dryRunWindow, window))
                _dryRunWindow = null;
            RefreshPda5DryRunVisibility();
        };

        // Modeless by design: the operator must be able to keep using the main shell
        // and watch/copy Console / Activity while deploy_millenova.sh is running.
        window.Show();
    }
}
