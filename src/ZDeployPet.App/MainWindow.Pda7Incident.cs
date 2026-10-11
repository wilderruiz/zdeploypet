using System.Windows;
using ZDeployPet.Core;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private IncidentBundlePreviewWindow? _incidentPreviewWindow;

    private void InitializePda7IncidentPreview()
    {
        // Incident bundle preview is now an on-demand diagnostics/support tool
        // exposed from Help rather than a permanent Home footer action.
    }

    private void RefreshPda7IncidentPreviewVisibility()
    {
        // No persistent Home control remains to refresh.
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
