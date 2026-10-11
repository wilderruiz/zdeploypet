using System.Windows;
using System.Windows.Controls;
using ZDeployPet.Core;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private IncidentBundlePreviewWindow? _incidentPreviewWindow;

    private void InitializePda7IncidentPreview()
    {
        // Incident bundle preview is an on-demand diagnostics/support tool exposed
        // from Help. Defensively remove any legacy Home-footer launcher left by an
        // older shell path so Help remains the single entry point.
        RemoveLegacyIncidentPreviewFooterControls();
    }

    private void RefreshPda7IncidentPreviewVisibility()
    {
        // No persistent Home control remains to refresh.
        RemoveLegacyIncidentPreviewFooterControls();
    }

    private void RemoveLegacyIncidentPreviewFooterControls()
    {
        if (_responsiveActionsPanel is null)
            return;

        for (int index = _responsiveActionsPanel.Children.Count - 1; index >= 0; index--)
        {
            if (_responsiveActionsPanel.Children[index] is not Button button)
                continue;

            string label = button.Content?.ToString() ?? string.Empty;
            if (!label.StartsWith("Incident preview", StringComparison.OrdinalIgnoreCase) &&
                !label.StartsWith("Incident bundle preview", StringComparison.OrdinalIgnoreCase))
                continue;

            _responsiveActionsPanel.Children.RemoveAt(index);

            // HelpTipFactory.AttachToButton historically inserted a sibling "i"
            // immediately after the launcher. Remove that stale helper as well.
            if (index < _responsiveActionsPanel.Children.Count &&
                _responsiveActionsPanel.Children[index] is Button info &&
                string.Equals(info.Content?.ToString(), "i", StringComparison.Ordinal))
            {
                _responsiveActionsPanel.Children.RemoveAt(index);
            }
        }
    }

    private void IncidentPreview_Click(object sender, RoutedEventArgs e)
    {
        DeploymentProfile? profile = _activeProfile;
        DeploymentReport? report = _latestDeploymentReport;
        string? canonicalRoot = _latestReportCanonicalRoot;

        if (profile is null)
        {
            MessageBox.Show(
                this,
                "Create or load a deployment profile before building an incident bundle.",
                "Incident bundle preview",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (report is null || string.IsNullOrWhiteSpace(canonicalRoot))
        {
            MessageBox.Show(
                this,
                "No trusted latest deployment report is loaded yet. Click Refresh on Latest deployment report, then try Incident bundle preview again.",
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

        AttachIncidentPreviewHelp(window);

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

    private static void AttachIncidentPreviewHelp(IncidentBundlePreviewWindow window)
    {
        if (window.Content is not Grid root)
            return;

        Button info = HelpTipFactory.Create(new HelpTipSpec(
            "Build and optionally export a sanitized incident bundle.",
            "Collects only explicitly selected trusted reporter artifacts and bounded in-memory console channels, then applies deterministic redaction before showing the exact content.",
            WhenToUse: "Use this when you need diagnostic evidence for yourself, support, or a coding agent. Deployment access does not need to be ON because collection is read-only.",
            WhatItDoes: "Lets you choose deployment JSON/summary/full-log evidence and Dry run / Live deploy / ZDeployPet console evidence. The preview shows exactly what is included, redacted, truncated, or omitted before any copy or local export.",
            Safety: "Copy/export stays disabled until a sanitized preview succeeds. Export requires an explicit local .txt destination outside the active project repository. No credentials, SSH-agent capability, or deployment authority are included."));

        info.HorizontalAlignment = HorizontalAlignment.Right;
        info.VerticalAlignment = VerticalAlignment.Top;
        info.Margin = new Thickness(0, 0, 0, 10);
        Grid.SetRow(info, 0);
        Panel.SetZIndex(info, 10);
        root.Children.Add(info);
    }
}
