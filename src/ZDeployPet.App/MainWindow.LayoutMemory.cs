using System.Windows;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private const double PreferredMaximumActivityPaneFraction = 0.40;

    private readonly UiLayoutStore _uiLayoutStore = new();
    private bool _uiLayoutRestored;

    private void RestoreUiLayoutMemory()
    {
        if (_uiLayoutRestored) return;
        _uiLayoutRestored = true;

        UiLayoutSnapshot? snapshot = _uiLayoutStore.Load();
        if (snapshot is null) return;

        double virtualLeft = SystemParameters.VirtualScreenLeft;
        double virtualTop = SystemParameters.VirtualScreenTop;
        double virtualWidth = Math.Max(1, SystemParameters.VirtualScreenWidth);
        double virtualHeight = Math.Max(1, SystemParameters.VirtualScreenHeight);

        double width = ClampFinite(snapshot.MainWidth, MinWidth, virtualWidth, Math.Max(MinWidth, Width));
        double height = ClampFinite(snapshot.MainHeight, MinHeight, virtualHeight, Math.Max(MinHeight, Height));

        Width = width;
        Height = height;

        if (IsFinite(snapshot.MainLeft) && IsFinite(snapshot.MainTop))
        {
            double maxLeft = virtualLeft + Math.Max(0, virtualWidth - 120);
            double maxTop = virtualTop + Math.Max(0, virtualHeight - 80);
            Left = Math.Clamp(snapshot.MainLeft, virtualLeft - Math.Max(0, width - 120), maxLeft);
            Top = Math.Clamp(snapshot.MainTop, virtualTop - Math.Max(0, height - 80), maxTop);
            WindowStartupLocation = WindowStartupLocation.Manual;
        }

        RestoreConsolePaneMemory(snapshot, width);

        if (snapshot.MainMaximized)
            WindowState = WindowState.Maximized;

        ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Shell", "Restored saved UI sizing and console-pane layout.");
    }

    private void RestoreConsolePaneMemory(UiLayoutSnapshot snapshot, double mainWindowWidth)
    {
        if (_activityPaneColumn is null || _activitySplitterColumn is null ||
            _activityPane is null || _activityPaneSplitter is null)
            return;

        // PDA-8 parity: Activity & reports is intentionally secondary to the main
        // deployment workspace. Older saved layouts could restore a nearly 50/50
        // split, so bound restored width to 40% of the remembered window width.
        // The operator can still drag the splitter wider during the current session.
        double maximumRestoredConsoleWidth = Math.Max(
            MinimumActivityPaneWidth,
            Math.Min(1200, mainWindowWidth * PreferredMaximumActivityPaneFraction));
        double fallbackConsoleWidth = Math.Min(430, maximumRestoredConsoleWidth);
        double consoleWidth = ClampFinite(
            snapshot.ConsolePaneWidth,
            MinimumActivityPaneWidth,
            maximumRestoredConsoleWidth,
            fallbackConsoleWidth);
        _rememberedActivityPaneWidth = new GridLength(consoleWidth);

        if (snapshot.ConsolePaneVisible)
        {
            _activityPaneColumn.MinWidth = MinimumActivityPaneWidth;
            _activityPaneColumn.Width = _rememberedActivityPaneWidth;
            _activitySplitterColumn.Width = new GridLength(5);
            _activityPaneSplitter.Visibility = Visibility.Visible;
            _activityPane.Visibility = Visibility.Visible;
        }
        else
        {
            _activityPaneColumn.MinWidth = 0;
            _activityPaneColumn.Width = new GridLength(0);
            _activitySplitterColumn.Width = new GridLength(0);
            _activityPaneSplitter.Visibility = Visibility.Collapsed;
            _activityPane.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveUiLayoutMemoryBestEffort()
    {
        try
        {
            Rect bounds = WindowState == WindowState.Normal
                ? new Rect(Left, Top, ActualWidth > 0 ? ActualWidth : Width, ActualHeight > 0 ? ActualHeight : Height)
                : RestoreBounds;

            double consoleWidth = _activityPaneColumn?.ActualWidth ?? _rememberedActivityPaneWidth.Value;
            if (!IsFinite(consoleWidth) || consoleWidth < MinimumActivityPaneWidth)
                consoleWidth = IsFinite(_rememberedActivityPaneWidth.Value) && _rememberedActivityPaneWidth.Value >= MinimumActivityPaneWidth
                    ? _rememberedActivityPaneWidth.Value
                    : 430;

            bool consoleVisible = _activityPane is null || _activityPane.Visibility == Visibility.Visible;

            UiLayoutSnapshot snapshot = new(
                UiLayoutSnapshot.CurrentSchemaVersion,
                MainWidth: Math.Max(MinWidth, bounds.Width),
                MainHeight: Math.Max(MinHeight, bounds.Height),
                MainLeft: bounds.Left,
                MainTop: bounds.Top,
                MainMaximized: WindowState == WindowState.Maximized,
                ConsolePaneWidth: consoleWidth,
                ConsolePaneVisible: consoleVisible);

            _uiLayoutStore.Save(snapshot);
        }
        catch
        {
            // UI memory is convenience-only and must never block shutdown.
        }
    }

    private static double ClampFinite(double value, double minimum, double maximum, double fallback)
    {
        if (!IsFinite(value)) return fallback;
        if (maximum < minimum) maximum = minimum;
        return Math.Clamp(value, minimum, maximum);
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
