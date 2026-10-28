using System.Windows;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using ZDeployPet.Core;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private static readonly bool Pda8DeploymentCompletionUxRegistered = RegisterPda8DeploymentCompletionUx();

    private bool _pda8DeploymentCompletionUxInitialized;
    private bool _deploymentCompletionPresentationPending;
    private bool _deploymentCompletionAttentionActive;
    private DispatcherTimer? _deploymentCompletionPulseTimer;
    private TaskbarItemProgressState _deploymentCompletionTaskbarState = TaskbarItemProgressState.None;
    private int _deploymentCompletionPulseStep;

    private static bool RegisterPda8DeploymentCompletionUx()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(MainWindowPda8DeploymentCompletionUxLoaded));
        return true;
    }

    private static void MainWindowPda8DeploymentCompletionUxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
            window.InitializePda8DeploymentCompletionUx();
    }

    private void InitializePda8DeploymentCompletionUx()
    {
        if (_pda8DeploymentCompletionUxInitialized)
            return;

        _pda8DeploymentCompletionUxInitialized = true;
        ShellRuntime.Activity.EntryAdded += DeploymentCompletionActivity_EntryAdded;
        Activated += DeploymentCompletionWindow_Activated;
        Closed += DeploymentCompletionWindow_Closed;
    }

    private void DeploymentCompletionActivity_EntryAdded(object? sender, ShellActivityEntry entry)
    {
        Dispatcher.BeginInvoke(new Action(() => ProcessDeploymentCompletionActivity(entry)));
    }

    private void ProcessDeploymentCompletionActivity(ShellActivityEntry entry)
    {
        bool runStarted =
            (string.Equals(entry.Category, "Dry run", StringComparison.OrdinalIgnoreCase) &&
             entry.Message.StartsWith("Dry run started", StringComparison.OrdinalIgnoreCase)) ||
            (string.Equals(entry.Category, "Live deploy", StringComparison.OrdinalIgnoreCase) &&
             entry.Message.StartsWith("Live deployment started", StringComparison.OrdinalIgnoreCase));

        if (runStarted)
        {
            _deploymentCompletionPresentationPending = false;
            ResetLatestReportCompletionHighlight();
            ClearDeploymentCompletionTaskbarAttention();
            return;
        }

        bool executionFinished =
            (string.Equals(entry.Category, "Dry run", StringComparison.OrdinalIgnoreCase) &&
             entry.Message.StartsWith("Dry-run process exited with code", StringComparison.OrdinalIgnoreCase)) ||
            (string.Equals(entry.Category, "Live deploy", StringComparison.OrdinalIgnoreCase) &&
             entry.Message.StartsWith("Live deployment process exited with code", StringComparison.OrdinalIgnoreCase));

        if (executionFinished)
        {
            _deploymentCompletionPresentationPending = true;
            return;
        }

        bool trustedReportLoaded =
            string.Equals(entry.Category, "Report", StringComparison.OrdinalIgnoreCase) &&
            entry.Message.StartsWith("Latest deployment report loaded for", StringComparison.OrdinalIgnoreCase);

        if (!trustedReportLoaded || !_deploymentCompletionPresentationPending || _latestDeploymentReport is null)
            return;

        _deploymentCompletionPresentationPending = false;
        PresentTrustedDeploymentCompletion(_latestDeploymentReport);
    }

    private void PresentTrustedDeploymentCompletion(DeploymentReport report)
    {
        // Change only ZDeployPet's internal surface. Never restore, activate or steal
        // the foreground window if the operator is working elsewhere.
        ShowHomeSurface();
        SelectHomeWorkspaceSection(HomeWorkspaceSection.Reports);

        bool passed = report.Outcome == DeploymentReportOutcome.Pass;
        ApplyLatestReportCompletionHighlight(passed);
        StartDeploymentCompletionTaskbarPulse(passed);

        ShellRuntime.Activity.Add(
            passed ? ShellActivityLevel.Success : ShellActivityLevel.Warning,
            "Shell",
            $"Deployment completion presented from trusted reporter truth: {report.Outcome.ToString().ToUpperInvariant()} for {report.DeploymentId}.");
    }

    private void ApplyLatestReportCompletionHighlight(bool passed)
    {
        if (_latestReportCard is null)
            return;

        Color background = passed
            ? Color.FromRgb(0x27, 0x3D, 0x31)
            : Color.FromRgb(0x44, 0x2B, 0x30);
        Color border = passed
            ? Color.FromRgb(0x6C, 0xAD, 0x80)
            : Color.FromRgb(0xCD, 0x73, 0x7E);

        _latestReportCard.Background = new SolidColorBrush(background);
        _latestReportCard.BorderBrush = new SolidColorBrush(border);
        _latestReportCard.BorderThickness = new Thickness(1.5);
    }

    private void ResetLatestReportCompletionHighlight()
    {
        if (_latestReportCard is null)
            return;

        _latestReportCard.Background = FindBrush("AppSurfaceBrush", Brushes.DimGray);
        _latestReportCard.BorderBrush = FindBrush("AppBorderBrush", Brushes.Gray);
        _latestReportCard.BorderThickness = new Thickness(1);
    }

    private void StartDeploymentCompletionTaskbarPulse(bool passed)
    {
        TaskbarItemInfo ??= new TaskbarItemInfo();
        _deploymentCompletionTaskbarState = passed
            ? TaskbarItemProgressState.Normal
            : TaskbarItemProgressState.Error;
        _deploymentCompletionPulseStep = 0;
        _deploymentCompletionAttentionActive = true;

        _deploymentCompletionPulseTimer ??= new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(420)
        };
        _deploymentCompletionPulseTimer.Tick -= DeploymentCompletionPulseTimer_Tick;
        _deploymentCompletionPulseTimer.Tick += DeploymentCompletionPulseTimer_Tick;

        ShowDeploymentCompletionTaskbarColor(show: true);
        _deploymentCompletionPulseTimer.Start();
    }

    private void DeploymentCompletionPulseTimer_Tick(object? sender, EventArgs e)
    {
        _deploymentCompletionPulseStep++;
        bool show = _deploymentCompletionPulseStep % 2 == 0;
        ShowDeploymentCompletionTaskbarColor(show);

        if (_deploymentCompletionPulseStep < 8)
            return;

        _deploymentCompletionPulseTimer?.Stop();

        // If the app is in the background, leave a solid green/red taskbar state
        // after the pulse until the operator returns. If already active, the pulse
        // itself is enough and the taskbar returns to normal.
        if (IsActive)
            ClearDeploymentCompletionTaskbarAttention();
        else
            ShowDeploymentCompletionTaskbarColor(show: true);
    }

    private void ShowDeploymentCompletionTaskbarColor(bool show)
    {
        if (TaskbarItemInfo is null)
            return;

        TaskbarItemInfo.ProgressValue = show ? 1.0 : 0.0;
        TaskbarItemInfo.ProgressState = show
            ? _deploymentCompletionTaskbarState
            : TaskbarItemProgressState.None;
    }

    private void DeploymentCompletionWindow_Activated(object? sender, EventArgs e)
    {
        if (_deploymentCompletionAttentionActive)
            ClearDeploymentCompletionTaskbarAttention();
    }

    private void ClearDeploymentCompletionTaskbarAttention()
    {
        _deploymentCompletionPulseTimer?.Stop();
        _deploymentCompletionAttentionActive = false;
        _deploymentCompletionPulseStep = 0;
        _deploymentCompletionTaskbarState = TaskbarItemProgressState.None;

        if (TaskbarItemInfo is not null)
        {
            TaskbarItemInfo.ProgressValue = 0;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
        }
    }

    private void DeploymentCompletionWindow_Closed(object? sender, EventArgs e)
    {
        ShellRuntime.Activity.EntryAdded -= DeploymentCompletionActivity_EntryAdded;
        Activated -= DeploymentCompletionWindow_Activated;
        Closed -= DeploymentCompletionWindow_Closed;
        _deploymentCompletionPulseTimer?.Stop();
    }
}
