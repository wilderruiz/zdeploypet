using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private static readonly bool UiPolishRuntimeRegistered = RegisterUiPolishRuntime();
    private bool _uiPolishRuntimeApplied;

    private static bool RegisterUiPolishRuntime()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(MainWindowUiPolishLoaded));
        return true;
    }

    private static void MainWindowUiPolishLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window || window._uiPolishRuntimeApplied)
            return;

        window._uiPolishRuntimeApplied = true;
        window.Dispatcher.BeginInvoke(
            new Action(window.ApplyUiPolishRuntime),
            DispatcherPriority.Loaded);
    }

    private void ApplyUiPolishRuntime()
    {
        PolishLatestReportHeaderActions();
        PolishPrimaryNavigationState();
    }

    private void PolishLatestReportHeaderActions()
    {
        Button? refresh = _latestReportRefreshButton;
        if (refresh?.Parent is not Grid grid)
            return;

        int refreshIndex = grid.Children.IndexOf(refresh);
        Button? help = refreshIndex >= 0 && refreshIndex + 1 < grid.Children.Count
            ? grid.Children[refreshIndex + 1] as Button
            : null;

        Brush normal = FindBrush("AppMutedTextBrush", Brushes.LightGray);
        Brush hover = new SolidColorBrush(Color.FromRgb(79, 111, 150));

        Grid glyph = BuildCounterClockwiseRefreshGlyph(normal);
        refresh.Content = glyph;
        refresh.Width = 28;
        refresh.Height = 28;
        refresh.Padding = new Thickness(3);
        refresh.Margin = new Thickness(0, 0, help is null ? 0 : 30, 0);
        refresh.HorizontalAlignment = HorizontalAlignment.Right;
        refresh.VerticalAlignment = VerticalAlignment.Top;
        refresh.Background = Brushes.Transparent;
        refresh.BorderBrush = Brushes.Transparent;
        refresh.BorderThickness = new Thickness(0);
        refresh.ToolTip = "Refresh deployment reports";

        refresh.MouseEnter += (_, _) => SetRefreshGlyphBrush(glyph, hover);
        refresh.MouseLeave += (_, _) => SetRefreshGlyphBrush(glyph, normal);
        refresh.IsEnabledChanged += (_, _) => glyph.Opacity = refresh.IsEnabled ? 1.0 : 0.45;
        glyph.Opacity = refresh.IsEnabled ? 1.0 : 0.45;

        Grid.SetRow(refresh, 0);
        Grid.SetColumn(refresh, 1);

        if (help is not null)
        {
            Grid.SetRow(help, 0);
            Grid.SetColumn(help, 1);
            help.HorizontalAlignment = HorizontalAlignment.Right;
            help.VerticalAlignment = VerticalAlignment.Top;
            help.Margin = new Thickness(0);
        }
    }

    private static Grid BuildCounterClockwiseRefreshGlyph(Brush brush)
    {
        Grid glyph = new() { Width = 18, Height = 18 };

        System.Windows.Shapes.Path arc = new()
        {
            Data = Geometry.Parse("M 14.8,5.2 A 6.2,6.2 0 1 0 15.0,12.7"),
            Stroke = brush,
            StrokeThickness = 1.8,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Fill = Brushes.Transparent
        };
        glyph.Children.Add(arc);

        System.Windows.Shapes.Path arrow = new()
        {
            Data = Geometry.Parse("M 14.8,2.7 L 14.8,6.6 L 10.9,6.6"),
            Stroke = brush,
            StrokeThickness = 1.8,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Fill = Brushes.Transparent
        };
        glyph.Children.Add(arrow);

        return glyph;
    }

    private static void SetRefreshGlyphBrush(Grid glyph, Brush brush)
    {
        foreach (UIElement child in glyph.Children)
        {
            if (child is System.Windows.Shapes.Path path)
                path.Stroke = brush;
        }
    }

    private void PolishPrimaryNavigationState()
    {
        if (_homeToolbarButton is null || _profileToolbarButton is null)
            return;

        _homeToolbarButton.BorderBrush = AppBorder;
        _profileToolbarButton.BorderBrush = AppBorder;

        RoutedEventHandler clickUpdate = (_, _) =>
            Dispatcher.BeginInvoke(new Action(RefreshPrimaryNavigationState), DispatcherPriority.Background);

        DependencyPropertyChangedEventHandler visibilityUpdate = (_, _) =>
            Dispatcher.BeginInvoke(new Action(RefreshPrimaryNavigationState), DispatcherPriority.Background);

        _homeToolbarButton.Click += clickUpdate;
        _profileToolbarButton.Click += clickUpdate;
        DiscoveryPanel.IsVisibleChanged += visibilityUpdate;
        SetupPanel.IsVisibleChanged += visibilityUpdate;

        RefreshPrimaryNavigationState();
    }

    private void RefreshPrimaryNavigationState()
    {
        if (_homeToolbarButton is null || _profileToolbarButton is null)
            return;

        Brush accent = FindBrush("AppAccentBrush", Brushes.IndianRed);
        Brush neutral = AppBorder;

        bool profileActive = SetupPanel.Visibility == Visibility.Visible;
        bool homeActive = !profileActive && DiscoveryPanel.Visibility == Visibility.Visible;

        _homeToolbarButton.BorderBrush = homeActive ? accent : neutral;
        _profileToolbarButton.BorderBrush = profileActive ? accent : neutral;

        _homeToolbarButton.Background = homeActive ? AppSurfaceRaised : AppSurface;
        _profileToolbarButton.Background = profileActive ? AppSurfaceRaised : AppSurface;
    }
}
