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

        accessGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _liveReviewButton = new Button
        {
            Content = "Live deploy review…",
            Padding = new Thickness(16, 9, 16, 9),
            Margin = new Thickness(0, 10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            Visibility = Visibility.Visible,
            IsEnabled = false
        };
        _liveReviewButton.Click += LiveReview_Click;
        Grid.SetRow(_liveReviewButton, 2);
        Grid.SetColumnSpan(_liveReviewButton, 2);
        accessGrid.Children.Add(_liveReviewButton);

        HelpTipFactory.AttachToButton(
            _liveReviewButton,
            new HelpTipSpec(
                "Open the immutable live-deployment review.",
                "PDA-6 freezes the exact profile, target, release choice, script path and script SHA-256 before live execution can be authorized.",
                WhenToUse: "Available only while Deployment access is ON / READY with all configured target probes passed.",
                WhatItDoes: "Creates a review snapshot, requires you to manually type the exact configured confirmation phrase, revalidates the frozen state immediately before launch, then runs only the approved project script.",
                Safety: "The confirmation field always starts blank. ZDeployPet never auto-fills or synthesizes the phrase. Any changed script/session/target/release state invalidates confirmation, and live execution is never retried automatically."));

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

        _liveReviewButton.Visibility = shellVisible ? Visibility.Visible : Visibility.Collapsed;
        _liveReviewButton.IsEnabled = shellVisible && accessReady && !_liveExecutionBusy && !_deploymentExecutionGate.IsRunning;
        _liveReviewButton.Content = _liveExecutionBusy ? "Live deploy running…" : "Live deploy review…";
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
