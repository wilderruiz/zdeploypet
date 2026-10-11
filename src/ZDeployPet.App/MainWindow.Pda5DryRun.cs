using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZDeployPet.Core;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private readonly DeploymentExecutionGate _deploymentExecutionGate = new();
    private Button? _dryRunButton;
    private Button? _deploymentActionsHelpButton;
    private DryRunWindow? _dryRunWindow;
    private bool _dryRunExecutionBusy;

    private void InitializePda5DryRun()
    {
        if (_dryRunButton is not null || _operatorAccessCard?.Child is not Grid accessGrid)
            return;

        EnsureDeploymentActionCardLayout(accessGrid);
        EnsureDeploymentActionsHelp(accessGrid);

        _dryRunButton = CreateDeploymentActionCard("DRY", "Dry run only");
        _dryRunButton.IsEnabled = false;
        _dryRunButton.Click += DryRun_Click;
        Grid.SetRow(_dryRunButton, 0);
        Grid.SetColumn(_dryRunButton, 0);
        accessGrid.Children.Add(_dryRunButton);

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
        _dryRunButton.Content = BuildDeploymentActionCardContent(_dryRunExecutionBusy ? "DRY…" : "DRY");
        if (_deploymentActionsHelpButton is not null)
            _deploymentActionsHelpButton.Visibility = shellVisible ? Visibility.Visible : Visibility.Collapsed;
        RefreshPda6LiveReviewVisibility();
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
        // and watch/copy Output / Reports while deploy_millenova.sh is running.
        window.Show();
    }

    private void EnsureDeploymentActionCardLayout(Grid accessGrid)
    {
        if (accessGrid.RowDefinitions.Count == 0)
            accessGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        if (accessGrid.ColumnDefinitions.Count < 3)
        {
            accessGrid.ColumnDefinitions.Clear();
            accessGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            accessGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            accessGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }
    }

    private void EnsureDeploymentActionsHelp(Grid accessGrid)
    {
        if (_deploymentActionsHelpButton is not null)
            return;

        _deploymentActionsHelpButton = HelpTipFactory.Create(new HelpTipSpec(
            "Choose a deployment action.",
            "DRY opens the allowlisted dry-run flow. LIVE opens the immutable live-deployment review. Both remain unavailable until ZDeploy Pet is READY and the bounded deployment-access checks have passed.",
            WhenToUse: "Use DRY to verify the configured deployment script without live deployment. Use LIVE only when you intend to review and explicitly authorize a live deployment.",
            WhatItDoes: "The two compact launchers reuse the existing PDA-5/PDA-6 execution paths; this Home card only starts those guarded workflows.",
            Safety: "ZDeploy Pet owns access state. DRY cannot select live mode, and LIVE still requires immutable review, exact confirmation, script fingerprint revalidation, a valid READY session, and the existing execution gate."));
        _deploymentActionsHelpButton.HorizontalAlignment = HorizontalAlignment.Right;
        _deploymentActionsHelpButton.VerticalAlignment = VerticalAlignment.Top;
        _deploymentActionsHelpButton.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetRow(_deploymentActionsHelpButton, 0);
        Grid.SetColumn(_deploymentActionsHelpButton, 2);
        accessGrid.Children.Add(_deploymentActionsHelpButton);
    }

    private Button CreateDeploymentActionCard(string label, string toolTip)
    {
        Button button = new()
        {
            Content = BuildDeploymentActionCardContent(label),
            ToolTip = toolTip,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 10, 0),
            MinWidth = 112,
            MinHeight = 58,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Background = FindBrush("AppSurfaceBrush", Brushes.DarkSlateGray),
            BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            Visibility = Visibility.Visible
        };

        return button;
    }

    private FrameworkElement BuildDeploymentActionCardContent(string label)
        => new Border
        {
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(20, 12, 20, 12),
            Child = new TextBlock
            {
                Text = label,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = FindBrush("AppTextBrush", Brushes.White)
            }
        };
}
