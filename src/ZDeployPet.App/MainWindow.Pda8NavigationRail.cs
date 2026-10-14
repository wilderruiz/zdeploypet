using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private static readonly bool Pda8NavigationRailRegistered = RegisterPda8NavigationRail();
    private bool _pda8NavigationRailApplied;
    private bool _pda8NavigationRailRetryQueued;

    private static bool RegisterPda8NavigationRail()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(MainWindowPda8NavigationRailLoaded));
        return true;
    }

    private static void MainWindowPda8NavigationRailLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window)
            return;

        window.Dispatcher.BeginInvoke(
            new Action(window.ApplyPda8NavigationRail),
            DispatcherPriority.Loaded);
    }

    private void ApplyPda8NavigationRail()
    {
        if (_pda8NavigationRailApplied)
            return;

        if (_homeToolbarButton is null ||
            _profileToolbarButton is null ||
            Content is not DockPanel shell ||
            _activityPane is null)
        {
            QueuePda8NavigationRailRetry();
            return;
        }

        Grid? body = null;
        foreach (UIElement child in shell.Children)
        {
            if (child is Grid candidate && candidate.Children.Contains(_activityPane))
            {
                body = candidate;
                break;
            }
        }

        if (body is null)
        {
            QueuePda8NavigationRailRetry();
            return;
        }

        FrameworkElement? legacyNavigationBar = FindDirectShellChild(_homeToolbarButton, shell);

        DetachFromParent(_homeToolbarButton);
        DetachFromParent(_profileToolbarButton);

        if (legacyNavigationBar is not null && shell.Children.Contains(legacyNavigationBar))
            shell.Children.Remove(legacyNavigationBar);

        const double navigationRailWidth = 64;

        UIElement[] existingBodyChildren = body.Children.Cast<UIElement>().ToArray();
        body.ColumnDefinitions.Insert(0, new ColumnDefinition
        {
            Width = new GridLength(navigationRailWidth),
            MinWidth = navigationRailWidth,
            MaxWidth = navigationRailWidth
        });

        foreach (UIElement child in existingBodyChildren)
            Grid.SetColumn(child, Grid.GetColumn(child) + 1);

        Border rail = new()
        {
            Width = navigationRailWidth,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = FindBrush("Pda8ShellBackgroundBrush", FindBrush("AppBackgroundBrush", Brushes.Black)),
            BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(10, 12, 10, 8)
        };

        StackPanel navigation = new()
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top
        };

        ConfigurePda8NavigationRailButton(_homeToolbarButton);
        ConfigurePda8NavigationRailButton(_profileToolbarButton);

        navigation.Children.Add(_homeToolbarButton);
        navigation.Children.Add(_profileToolbarButton);
        rail.Child = navigation;

        Grid.SetColumn(rail, 0);
        body.Children.Add(rail);

        RoutedEventHandler clickRefresh = (_, _) => QueuePda8NavigationRailStateRefresh();
        DependencyPropertyChangedEventHandler visibilityRefresh = (_, _) => QueuePda8NavigationRailStateRefresh();

        _homeToolbarButton.Click += clickRefresh;
        _profileToolbarButton.Click += clickRefresh;
        SetupPanel.IsVisibleChanged += visibilityRefresh;
        DiscoveryPanel.IsVisibleChanged += visibilityRefresh;

        _pda8NavigationRailApplied = true;
        QueuePda8NavigationRailStateRefresh();

        MinWidth = Math.Max(MinWidth, MinimumOperatorPaneWidth + MinimumActivityPaneWidth + 109);

        ShellRuntime.Activity.Add(
            ShellActivityLevel.Info,
            "Shell",
            "PDA-8 V3 left Home/Profile navigation rail initialized.");
    }

    private void QueuePda8NavigationRailRetry()
    {
        if (_pda8NavigationRailRetryQueued)
            return;

        _pda8NavigationRailRetryQueued = true;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _pda8NavigationRailRetryQueued = false;
            ApplyPda8NavigationRail();
        }), DispatcherPriority.Background);
    }

    private static FrameworkElement? FindDirectShellChild(DependencyObject element, DockPanel shell)
    {
        DependencyObject? current = element;

        while (current is not null)
        {
            DependencyObject? parent = LogicalTreeHelper.GetParent(current);
            if (parent is null)
            {
                try
                {
                    parent = VisualTreeHelper.GetParent(current);
                }
                catch (InvalidOperationException)
                {
                    parent = null;
                }
            }

            if (ReferenceEquals(parent, shell))
                return current as FrameworkElement;

            current = parent;
        }

        return null;
    }

    private static void DetachFromParent(UIElement element)
    {
        DependencyObject? parent = LogicalTreeHelper.GetParent(element);
        if (parent is null)
        {
            try
            {
                parent = VisualTreeHelper.GetParent(element);
            }
            catch (InvalidOperationException)
            {
                parent = null;
            }
        }

        switch (parent)
        {
            case Panel panel:
                panel.Children.Remove(element);
                break;
            case Border border when ReferenceEquals(border.Child, element):
                border.Child = null;
                break;
            case ContentControl contentControl when ReferenceEquals(contentControl.Content, element):
                contentControl.Content = null;
                break;
            case Decorator decorator when ReferenceEquals(decorator.Child, element):
                decorator.Child = null;
                break;
        }
    }

    private void ConfigurePda8NavigationRailButton(Button button)
    {
        if (Application.Current.TryFindResource("Pda8IconButtonStyle") is Style style)
            button.Style = style;

        button.Width = 40;
        button.Height = 40;
        button.MinHeight = 0;
        button.Padding = new Thickness(8);
        button.Margin = new Thickness(0, 0, 0, 8);
        button.Background = Brushes.Transparent;
        button.BorderBrush = Brushes.Transparent;
        button.BorderThickness = new Thickness(1);
        button.Foreground = FindBrush("AppMutedTextBrush", AppText);
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.VerticalContentAlignment = VerticalAlignment.Center;
    }

    private void QueuePda8NavigationRailStateRefresh()
    {
        Dispatcher.BeginInvoke(
            new Action(RefreshPda8NavigationRailState),
            DispatcherPriority.ContextIdle);
    }

    private void RefreshPda8NavigationRailState()
    {
        if (!_pda8NavigationRailApplied ||
            _homeToolbarButton is null ||
            _profileToolbarButton is null)
            return;

        bool profileActive = SetupPanel.Visibility == Visibility.Visible;
        bool homeActive = !profileActive && DiscoveryPanel.Visibility == Visibility.Visible;

        ApplyPda8NavigationButtonState(_homeToolbarButton, homeActive);
        ApplyPda8NavigationButtonState(_profileToolbarButton, profileActive);
    }

    private void ApplyPda8NavigationButtonState(Button button, bool active)
    {
        Brush accent = FindBrush("AppBrandAccentBrush", Brushes.Goldenrod);
        Brush activeSurface = FindBrush("AppSurfaceRaisedBrush", AppSurfaceRaised);
        Brush inactiveText = FindBrush("AppMutedTextBrush", AppText);

        button.Background = active ? activeSurface : Brushes.Transparent;
        button.BorderBrush = active ? accent : Brushes.Transparent;
        button.Foreground = active ? AppText : inactiveText;
        button.Opacity = active ? 1.0 : 0.82;
    }
}
