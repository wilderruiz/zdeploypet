using System.Windows;
using System.Windows.Threading;
using ZDeployPet.Core;
using ZDeployPet.WslBridge;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda4PollingOptimized;

    /// <summary>
    /// Replaces the first PDA-4 timer with a lightweight latest.json poll.
    /// Historical enumeration remains explicit/on-entry and is refreshed only
    /// when a genuinely new deployment id is observed.
    /// </summary>
    private void OptimizePda4ReportPolling()
    {
        if (_pda4PollingOptimized || _deploymentReportRefreshTimer is null)
            return;

        _pda4PollingOptimized = true;

        // The original timer refreshes latest + full history every tick. Stop it
        // before replacing the field so its anonymous Tick handler cannot keep
        // restarting expensive historical enumeration.
        _deploymentReportRefreshTimer.Stop();

        DispatcherTimer lightweightTimer = new()
        {
            Interval = DeploymentReportRefreshInterval
        };
        lightweightTimer.Tick += async (_, _) => await RefreshLatestReportFromTimerAsync();
        _deploymentReportRefreshTimer = lightweightTimer;

        if (DiscoveryPanel.Visibility == Visibility.Visible)
            lightweightTimer.Start();
    }

    private async Task RefreshLatestReportFromTimerAsync()
    {
        DispatcherTimer? timer = _deploymentReportRefreshTimer;
        if (timer is null || DiscoveryPanel.Visibility != Visibility.Visible)
            return;

        DeploymentProfile? profile = _activeProfile;
        if (profile is null || string.IsNullOrWhiteSpace(profile.ReportRoot))
            return;

        timer.Stop();
        try
        {
            WslDeploymentReportReadResult read = await _deploymentReportReader.ReadLatestAsync(
                profile.WslDistribution,
                profile.ReportRoot);

            // Timer polling is deliberately fail-soft. Manual Refresh remains the
            // operator-facing path that surfaces detailed read/validation errors.
            if (!read.Success || read.Json is null || read.CanonicalReportRoot is null)
                return;

            DeploymentReportParseResult parsed = DeploymentReportParser.Parse(read.Json);
            if (!parsed.Success || parsed.Report is null)
                return;

            DeploymentReport report = parsed.Report;
            if (!ArtifactsStayInsideRoot(read.CanonicalReportRoot, report.Artifacts))
                return;

            string? previousDeploymentId = _latestDeploymentReport?.DeploymentId;
            bool changed = !string.Equals(
                previousDeploymentId,
                report.DeploymentId,
                StringComparison.Ordinal);

            if (!changed)
                return;

            string details =
                $"Release {report.Release}  •  {report.Mode.ToUpperInvariant()}  •  Target {report.Target}  •  " +
                $"Started {report.StartedAt:yyyy-MM-dd HH:mm:ss zzz}  •  Finished {report.FinishedAt:yyyy-MM-dd HH:mm:ss zzz}  •  " +
                $"{report.DurationSeconds}s  •  ID {report.DeploymentId}";

            _latestReportCanonicalRoot = read.CanonicalReportRoot;
            _latestDeploymentReport = report;
            SetLatestReportCard(
                report.Outcome.ToString().ToUpperInvariant(),
                details,
                OutcomeBrush(report.Outcome));
            SetLatestReportArtifactButtonsVisible(true);

            // History is expensive because each historical report is independently
            // resolved, bounded and parsed. Refresh it only when latest.json proves
            // that a new deployment has appeared.
            await RefreshDeploymentHistoryAsync(profile);

            ShellRuntime.Activity.Add(
                ShellActivityLevel.Info,
                "Report",
                $"New deployment report detected for '{profile.Name}': {report.Outcome}, release {report.Release}, mode {report.Mode}, target {report.Target}.");
        }
        catch
        {
            // Keep the last trusted report visible. Manual Refresh exposes errors.
        }
        finally
        {
            if (DiscoveryPanel.Visibility == Visibility.Visible)
                timer.Start();
        }
    }
}
