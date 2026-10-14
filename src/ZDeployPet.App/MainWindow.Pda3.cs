using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda3ShellHooked;
    private WrapPanel? _responsiveActionsPanel;

    private void InitializePda3Shell()
    {
        if (!_pda3ShellHooked)
        {
            _pda3ShellHooked = true;
            ApplyMainWindowThemeFixups();
            DiscoveryPanel.IsVisibleChanged += (_, _) => RefreshLegacyFooterStatusVisibility();
            SetupPanel.IsVisibleChanged += (_, _) => RefreshLegacyFooterStatusVisibility();
        }

        InitializePda3MenuShell();
        ApplyProfileToolbarInfoPolish();
        InitializePda8V9ActivityReports();
        InitializeProfileDiscoveryActivityRouting();
        InitializeOperatorAccessControl();
        InitializePetCompanion();
        InitializePda4ReportMonitor();
        NormalizeDiscoverySurfaceRows();
        EnsureResponsiveActionPanel();
        InitializePda5DryRun();
        InitializePda6LiveReview();
        InitializePda7IncidentPreview();
        InitializePda8HomeWorkspace();
        InitializePda8ContextStrip();
        InitializePda8V6SegmentedWorkspace();
        InitializePda8V8ReportVisuals();
        InitializePda8V10ProfileVisuals();
        ApplyResponsiveActionSpacing();
        RefreshLegacyFooterStatusVisibility();
    }

    private void ApplyProfileToolbarInfoPolish()
    {
        if (_profileStageToolbar?.Child is not StackPanel row || row.Children.Count != 4)
            return;

        if (row.Children[0] is not Button gitAction ||
            row.Children[1] is not Button gitInfo ||
            row.Children[2] is not Button discoveryAction ||
            row.Children[3] is not Button discoveryInfo)
            return;

        row.Children.Clear();
        row.Children.Add(BuildProfileToolbarActionHost(gitAction, gitInfo, new Thickness(0, 0, 12, 0)));
        row.Children.Add(BuildProfileToolbarActionHost(discoveryAction, discoveryInfo, new Thickness(0)));
    }

    private static FrameworkElement BuildProfileToolbarActionHost(Button action, Button info, Thickness margin)
    {
        Grid host = new()
        {
            Width = 54,
            Height = 36,
            Margin = margin,
            VerticalAlignment = VerticalAlignment.Center
        };

        action.Margin = new Thickness(0);
        action.HorizontalAlignment = HorizontalAlignment.Left;
        action.VerticalAlignment = VerticalAlignment.Center;

        // P6.1: keep the shared info-button semantics, but present toolbar help as
        // a small raised annotation beside the command rather than a peer control.
        info.Width = 20;
        info.Height = 20;
        info.FontSize = 11;
        info.Margin = new Thickness(0, -3, 0, 0);
        info.HorizontalAlignment = HorizontalAlignment.Right;
        info.VerticalAlignment = VerticalAlignment.Top;

        host.Children.Add(action);
        host.Children.Add(info);
        return host;
    }

    private void NormalizeDiscoverySurfaceRows()
    {
        if (_operatorAccessCard is null || _latestReportCard is null || _deploymentHistoryCard is null)
            return;

        Grid.SetRow(_operatorAccessCard, 1);
        Grid.SetRow(_latestReportCard, 2);
        Grid.SetRow(_deploymentHistoryCard, 3);

        // Discovery diagnostics now belong to the ZDeployPet activity channel.
        // Keep the legacy textbox hidden as an internal handoff buffer for the
        // established discovery engine rather than exposing it on Home.
        Grid.SetRow(ResultsTextBox, 4);
        ResultsTextBox.Visibility = Visibility.Collapsed;
    }

    private void ApplyMainWindowThemeFixups()
    {
        // MainWindow.xaml still declares a local implicit ToolTip style for the
        // legacy Profile help content. That local style shadows the application-level
        // PDA-8 tooltip style, so bridge the shared dark tooltip explicitly here.
        // This is intentionally MainWindow-scoped rather than a global runtime repaint.
        if (Application.Current.TryFindResource("SharedToolTipStyle") is Style sharedToolTipStyle)
            Resources[typeof(ToolTip)] = sharedToolTipStyle;

        if (Application.Current.TryFindResource("SharedInfoButtonStyle") is Style sharedInfoButtonStyle)
        {
            Style infoButtonStyle = new(typeof(Button), sharedInfoButtonStyle);
            infoButtonStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 5, 8, 0)));
            infoButtonStyle.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right));
            infoButtonStyle.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Top));
            Resources["InfoButtonStyle"] = infoButtonStyle;
        }

        Brush shell = FindBrush("AppBackgroundBrush", Brushes.Black);
        Brush surface = FindBrush("AppSurfaceBrush", shell);
        Brush raised = FindBrush("AppSurfaceRaisedBrush", surface);
        Brush text = FindBrush("AppTextBrush", Brushes.White);

        Resources[SystemColors.WindowBrushKey] = surface;
        Resources[SystemColors.WindowTextBrushKey] = text;
        Resources[SystemColors.ControlBrushKey] = surface;
        Resources[SystemColors.ControlTextBrushKey] = text;
        Resources[SystemColors.ControlLightBrushKey] = surface;
        Resources[SystemColors.ControlLightLightBrushKey] = surface;
        Resources[SystemColors.ControlDarkBrushKey] = raised;
        Resources[SystemColors.ControlDarkDarkBrushKey] = raised;
    }

    private static Brush FindBrush(string key, Brush fallback)
        => Application.Current.TryFindResource(key) as Brush ?? fallback;

    private void EnsureResponsiveActionPanel()
    {
        if (_responsiveActionsPanel is not null) return;
        if (AccessOnboardingButton.Parent is not StackPanel current || current.Parent is not DockPanel footer) return;

        int index = footer.Children.IndexOf(current);
        Dock dock = DockPanel.GetDock(current);

        WrapPanel responsive = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        };
        DockPanel.SetDock(responsive, dock);

        while (current.Children.Count > 0)
        {
            UIElement child = current.Children[0];
            current.Children.RemoveAt(0);
            responsive.Children.Add(child);
        }

        footer.Children.Remove(current);
        footer.Children.Insert(index, responsive);
        footer.LastChildFill = true;
        _responsiveActionsPanel = responsive;
    }

    private void ApplyResponsiveActionSpacing()
    {
        if (_responsiveActionsPanel is null) return;

        foreach (UIElement child in _responsiveActionsPanel.Children)
        {
            if (child is not FrameworkElement element) continue;
            element.Margin = child is Button button && string.Equals(button.Content?.ToString(), "i", StringComparison.Ordinal)
                ? new Thickness(0, 0, 10, 10)
                : new Thickness(0, 0, 12, 10);
        }
    }

    private void RefreshLegacyFooterStatusVisibility()
    {
        // Home has its own navigation/state surfaces now. The legacy footer
        // status line is only useful while editing or validating a profile.
        StatusText.Visibility = SetupPanel.Visibility == Visibility.Visible
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}