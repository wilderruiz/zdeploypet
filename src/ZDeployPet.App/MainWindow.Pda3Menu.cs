using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda3MenuInitialized;

    private Brush AppSurface => (Brush)Application.Current.Resources["AppSurfaceBrush"];
    private Brush AppSurfaceRaised => (Brush)Application.Current.Resources["AppSurfaceRaisedBrush"];
    private Brush AppBorder => (Brush)Application.Current.Resources["AppBorderBrush"];
    private Brush AppText => (Brush)Application.Current.Resources["AppTextBrush"];

    private void InitializePda3MenuShell()
    {
        if (_pda3MenuInitialized) return;
        _pda3MenuInitialized = true;

        if (Content is not UIElement existingContent) return;

        Content = null;
        DockPanel shell = new();
        FrameworkElement menuBar = BuildTopMenu();
        DockPanel.SetDock(menuBar, Dock.Top);
        shell.Children.Add(menuBar);

        Grid body = new();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 620 });
        _activitySplitterColumn = new ColumnDefinition { Width = new GridLength(5) };
        _activityPaneColumn = new ColumnDefinition { Width = _rememberedActivityPaneWidth, MinWidth = 300 };
        body.ColumnDefinitions.Add(_activitySplitterColumn);
        body.ColumnDefinitions.Add(_activityPaneColumn);

        Grid.SetColumn(existingContent, 0);
        body.Children.Add(existingContent);

        _activityPaneSplitter = new GridSplitter
        {
            Width = 5,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = AppBorder,
            ResizeDirection = GridResizeDirection.Columns,
            ResizeBehavior = GridResizeBehavior.PreviousAndNext,
            ShowsPreview = true
        };
        Grid.SetColumn(_activityPaneSplitter, 1);
        body.Children.Add(_activityPaneSplitter);

        _activityPane = new Border
        {
            Background = (Brush)Application.Current.Resources["AppBackgroundBrush"],
            BorderBrush = AppBorder,
            BorderThickness = new Thickness(1, 0, 0, 0),
            Child = BuildEmbeddedActivityConsole()
        };
        Grid.SetColumn(_activityPane, 2);
        body.Children.Add(_activityPane);

        shell.Children.Add(body);
        Content = shell;

        MinWidth = Math.Max(MinWidth, 1100);
        if (Width < 1320) Width = 1320;

        ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Shell", "Top application menu initialized with embedded Console / Activity pane.");
    }

    private FrameworkElement BuildTopMenu()
    {
        Border bar = new()
        {
            Background = AppSurface,
            BorderBrush = AppBorder,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(8, 2, 8, 2)
        };

        StackPanel row = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        row.Children.Add(CreatePopupMenu("Project / Profile", popup =>
        {
            StackPanel panel = CreatePopupPanel();
            panel.Children.Add(CreatePopupAction("Edit profile", popup, () => EditProfile_Click(this, new RoutedEventArgs())));
            panel.Children.Add(CreatePopupSeparator());
            panel.Children.Add(CreatePopupAction("Deployment access…", popup, () => AccessOnboarding_Click(this, new RoutedEventArgs())));
            panel.Children.Add(CreatePopupAction("Session…", popup, () => DeploymentSession_Click(this, new RoutedEventArgs())));
            panel.Children.Add(CreatePopupAction("Git safety…", popup, () => GitSafety_Click(this, new RoutedEventArgs())));
            panel.Children.Add(CreatePopupSeparator());
            panel.Children.Add(CreatePopupAction("Exit", popup, Close));
            return panel;
        }));

        row.Children.Add(CreatePopupMenu("View", popup =>
        {
            StackPanel panel = CreatePopupPanel();
            panel.Children.Add(CreatePopupAction("Show / hide Console / Activity", popup, ToggleEmbeddedActivityPane));
            return panel;
        }));

        row.Children.Add(CreatePopupMenu("Help", popup =>
        {
            StackPanel panel = CreatePopupPanel();
            panel.Children.Add(CreatePopupAction("About ZDeployPet…", popup, OpenAboutWindow));
            return panel;
        }));

        bar.Child = row;
        return bar;
    }

    private FrameworkElement CreatePopupMenu(string label, Func<Popup, UIElement> buildContent)
    {
        Button anchor = new()
        {
            Content = label,
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 2, 0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = AppText,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        Popup popup = new()
        {
            Placement = PlacementMode.Bottom,
            PlacementTarget = anchor,
            StaysOpen = false,
            AllowsTransparency = true
        };

        Border popupChrome = new()
        {
            Background = AppSurface,
            BorderBrush = AppSurface,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(3),
            Child = buildContent(popup)
        };
        popup.Child = popupChrome;

        Grid host = new();
        host.Children.Add(anchor);
        host.Children.Add(popup);

        anchor.Click += (_, _) => popup.IsOpen = !popup.IsOpen;
        return host;
    }

    private StackPanel CreatePopupPanel() => new()
    {
        Background = AppSurface,
        MinWidth = 245
    };

    private Button CreatePopupAction(string label, Popup owner, Action action)
    {
        Button item = new()
        {
            Content = label,
            MinWidth = 245,
            Padding = new Thickness(14, 9, 14, 9),
            Margin = new Thickness(0),
            Background = AppSurface,
            Foreground = AppText,
            BorderBrush = AppSurface,
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Left
        };

        item.MouseEnter += (_, _) => item.Background = AppSurfaceRaised;
        item.MouseLeave += (_, _) => item.Background = AppSurface;
        item.Click += (_, _) =>
        {
            owner.IsOpen = false;
            action();
        };
        return item;
    }

    private FrameworkElement CreatePopupSeparator() => new Border
    {
        Height = 1,
        Margin = new Thickness(8, 3, 8, 3),
        Background = AppBorder
    };

    private void OpenAboutWindow()
    {
        ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Shell", "About ZDeployPet opened.");
        AboutWindow window = new() { Owner = this };
        window.ShowDialog();
    }
}
