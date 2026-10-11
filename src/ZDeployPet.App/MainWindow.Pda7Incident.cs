using System.Windows;
using System.Windows.Controls;
using ZDeployPet.Core;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private Button? _incidentPreviewButton;
    private IncidentBundlePreviewWindow? _incidentPreviewWindow;

    private void InitializePda7IncidentPreview()
    {
        if (_incidentPreviewButton is not null || _responsiveActionsPanel is null)
            return;

        _incidentPreviewButton = new Button
        {
            Content = "Incident preview…",
            Padding = new Thickness(16, 9, 16, 9),
            Visibility = Visibility.Collapsed
        };
        _incidentPreviewButton.Click += IncidentPreview_Click;
        _responsiveActionsPanel.Children.Add(_incidentPreviewButton);

        HelpTipFactory.AttachToButton(
            _incidentPreviewButton,
            new HelpTipSpec(
                "Build and optionally export a sanitized incident bundle.",
                "PDA-7 collects only explicitly selected trusted reporter artifacts and the existing bounded in-memory console channels, then applies deterministic redaction before showing the exact content.",
                WhenToUse: "Use this when you need diagnostic evidence for yourself or a coding agent. Deployment access does not need to be ON because collection is read-only.",
                WhatItDoes: "Lets you choose deployment JSON/summary/full-log evidence and Dry run / Live deploy / ZDeployPet console evidence. The preview shows exactly what is included, redacted, truncated, or omitted before any copy or local export.",
                Safety: "Copy/export stays disabled until a sanitized preview succeeds. Export requires an explicit local .txt destination outside the active project repository; network/UNC and alternate-data-stream destinations are rejected. No deployment authority is included."));

        DiscoveryPanel.IsVisibleChanged += (_, _) => RefreshPda7IncidentPreviewVisibility();
        RefreshPda7IncidentPreviewVisibility();
        ApplyResponsiveActionSpacing();
    }

    private void RefreshPda7IncidentPreviewVisibility()
    {
        if (_incidentPreviewButton is null) return;
        _incidentPreviewButton.Visibility = DiscoveryPanel.Visibility == Visibility.Visible && _activeProfile is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void IncidentPreview_Click(object sender, RoutedEventArgs e)
    {
        DeploymentProfile? profile = _activeProfile;
        DeploymentReport? report = _latestDeploymentReport;
        string? canonicalRoot = _latestReportCanonicalRoot;

        if (profile is null)
            return;

        if (report is null || string.IsNullOrWhiteSpace(canonicalRoot))
        {
            MessageBox.Show(
                this,
                "No trusted latest deployment report is loaded yet. Click Refresh on Latest deployment report, then try Incident preview again.",
                "Incident bundle preview",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (_incidentPreviewWindow is { IsVisible: true })
        {
            if (_incidentPreviewWindow.WindowState == WindowState.Minimized)
                _incidentPreviewWindow.WindowState = WindowState.Normal;
            _incidentPreviewWindow.Activate();
            return;
        }

        IncidentBundlePreviewWindow window = new(
            profile,
            report,
            canonicalRoot,
            new IncidentEvidenceCollector(_deploymentReportReader))
        {
            Owner = this
        };

        _incidentPreviewWindow = window;
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_incidentPreviewWindow, window))
                _incidentPreviewWindow = null;
        };
        window.Show();

        ShellRuntime.Activity.Add(
            ShellActivityLevel.Info,
            "Incident",
            $"Opened sanitized incident preview for deployment {report.DeploymentId}. Guarded local export is available only after a successful sanitized build.");
    }
}
