using System.Windows;
using System.Windows.Controls;
using ZDeployPet.Core;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private Button? _liveReviewButton;
    private LiveDeploymentReviewWindow? _liveReviewWindow;
    private bool _liveExecutionBusy;

    private void InitializePda6LiveReview()
    {
        if (_liveReviewButton is not null || _operatorAccessCard?.Child is not Grid accessGrid)
            return;

        EnsureDeploymentActionCardLayout(accessGrid);

        _liveReviewButton = CreateDeploymentActionCard("LIVE", "Live deploy review");
        _liveReviewButton.Margin = new Thickness(0);
        _liveReviewButton.IsEnabled = false;
        _liveReviewButton.Click += LiveReview_Click;
        Grid.SetRow(_liveReviewButton, 0);
        Grid.SetColumn(_liveReviewButton, 1);
        accessGrid.Children.Add(_liveReviewButton);

        DiscoveryPanel.IsVisibleChanged += (_, _) => RefreshPda6LiveReviewVisibility();
        RefreshPda6LiveReviewVisibility();
    }

    private void RefreshPda6LiveReviewVisibility()
    {
        if (_liveReviewButton is null) return;

        bool shellVisible = DiscoveryPanel.Visibility == Visibility.Visible && _activeProfile is not null;
        bool accessReady =
            _deploymentSession.TargetsReady &&
            _deploymentSession.State is DeploymentAccessSessionState.Ready or DeploymentAccessSessionState.Expiring;

        bool enabled = shellVisible && accessReady && !_liveExecutionBusy && !_deploymentExecutionGate.IsRunning;
        _liveReviewButton.Visibility = shellVisible ? Visibility.Visible : Visibility.Collapsed;
        _liveReviewButton.IsEnabled = enabled;
        _liveReviewButton.Content = BuildDeploymentActionCardContent(_liveExecutionBusy ? "LIVE…" : "LIVE");
        ApplyDeploymentActionVisual(_liveReviewButton, enabled, isLive: true);
    }

    private void LiveReview_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null || _liveReviewButton is null || !_liveReviewButton.IsEnabled)
            return;

        if (_liveReviewWindow is { IsVisible: true })
        {
            if (_liveReviewWindow.WindowState == WindowState.Minimized)
                _liveReviewWindow.WindowState = WindowState.Normal;
            _liveReviewWindow.Activate();
            return;
        }

        LiveDeploymentReviewWindow window = new(
            _activeProfile,
            _deploymentSession,
            targetsReady: () =>
                _deploymentSession.TargetsReady &&
                _deploymentSession.State is DeploymentAccessSessionState.Ready or DeploymentAccessSessionState.Expiring,
            refreshReports: async () => await RefreshLatestDeploymentReportAsync(),
            executionStateChanged: busy =>
            {
                _liveExecutionBusy = busy;
                RefreshPda6LiveReviewVisibility();
                RefreshPda5DryRunVisibility();
                RefreshPetCompanion();
            },
            executionGate: _deploymentExecutionGate)
        {
            Owner = this
        };

        _liveReviewWindow = window;
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_liveReviewWindow, window))
                _liveReviewWindow = null;
            RefreshPda6LiveReviewVisibility();
        };
        window.Show();
    }
}
