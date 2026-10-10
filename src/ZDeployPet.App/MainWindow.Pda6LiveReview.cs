using System.Windows;
using System.Windows.Controls;
using ZDeployPet.Core;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private Button? _liveReviewButton;
    private LiveDeploymentReviewWindow? _liveReviewWindow;

    private void InitializePda6LiveReview()
    {
        if (_liveReviewButton is not null || _operatorAccessCard?.Child is not Grid accessGrid)
            return;

        accessGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _liveReviewButton = new Button
        {
            Content = "Live deploy review…",
            Padding = new Thickness(16, 9, 16, 9),
            Margin = new Thickness(10, 12, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            Visibility = Visibility.Visible,
            IsEnabled = false
        };
        _liveReviewButton.Click += LiveReview_Click;
        Grid.SetRow(_liveReviewButton, 1);
        Grid.SetColumn(_liveReviewButton, 1);
        accessGrid.Children.Add(_liveReviewButton);

        HelpTipFactory.AttachToButton(
            _liveReviewButton,
            new HelpTipSpec(
                "Open the immutable live-deployment review.",
                "PDA-6 freezes the exact profile, target, release choice, script path and script SHA-256 before any live execution can later be considered.",
                WhenToUse: "Available only while Deployment access is ON / READY with all configured target probes passed.",
                WhatItDoes: "Creates a review snapshot and lets you manually type the exact configured confirmation phrase. This slice validates the review and confirmation only; it cannot execute a live deployment yet.",
                Safety: "The confirmation field is always blank when a review is created. ZDeployPet never auto-fills or synthesizes the phrase. Any changed script/session/target/release state invalidates confirmation."));

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
        _liveReviewButton.IsEnabled = shellVisible && accessReady && !_deploymentExecutionGate.IsRunning;
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
