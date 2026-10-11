using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda3MenuInitialized;
    private Button? _homeToolbarButton;
    private Button? _profileToolbarButton;
    private Border? _profileStageToolbar;
    private Button? _profileGitSafetyButton;
    private Button? _profileDiscoveryButton;

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

        FrameworkElement navigationBar = BuildNavigationToolbar();
        DockPanel.SetDock(navigationBar, Dock.Top);
        shell.Children.Add(navigationBar);

        _profileStageToolbar = BuildProfileStageToolbar();
        DockPanel.SetDock(_profileStageToolbar, Dock.Top);
        shell.Children.Add(_profileStageToolbar);

        Grid body = new();
        body.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star),
            MinWidth = MinimumOperatorPaneWidth
        });
        _activitySplitterColumn = new ColumnDefinition { Width = new GridLength(5) };
        _activityPaneColumn = new ColumnDefinition
        {
            Width = _rememberedActivityPaneWidth,
            MinWidth = MinimumActivityPaneWidth
        };
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

        SetupPanel.IsVisibleChanged += (_, _) => RefreshProfileStageToolbarVisibility();
        DiscoveryPanel.IsVisibleChanged += (_, _) =>
        {
            if (DiscoveryPanel.Visibility != Visibility.Visible) return;
            Dispatcher.BeginInvoke(new Action(HideLegacyProfileFooterActions));
        };
        RefreshProfileStageToolbarVisibility();
        HideLegacyProfileFooterActions();

        MinWidth = Math.Max(MinWidth, MinimumOperatorPaneWidth + MinimumActivityPaneWidth + 45);
        if (Width < 1180) Width = 1180;

        ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Shell", "Top application menu and Home/Profile navigation initialized with embedded Output / Reports pane.");
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
            panel.Children.Add(CreatePopupAction("Edit profile", popup, ShowProfileSurface));
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
            panel.Children.Add(CreatePopupAction("Show / hide Output / Reports", popup, ToggleEmbeddedActivityPane));
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

    private FrameworkElement BuildNavigationToolbar()
    {
        Border bar = new()
        {
            Background = FindBrush("AppBackgroundBrush", Brushes.Black),
            BorderBrush = AppBorder,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(10, 6, 10, 6)
        };

        StackPanel row = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _homeToolbarButton = CreateToolbarButton(
            "Home",
            "Return to the ZDeployPet Home screen.",
            BuildHomeIcon());
        _homeToolbarButton.Click += (_, _) => ShowHomeSurface();
        row.Children.Add(_homeToolbarButton);

        _profileToolbarButton = CreateToolbarButton(
            "Profile",
            "Open the active deployment profile and its setup/readiness tools.",
            BuildProfileIcon());
        _profileToolbarButton.Click += (_, _) => ShowProfileSurface();
        row.Children.Add(_profileToolbarButton);

        bar.Child = row;
        return bar;
    }

    private Border BuildProfileStageToolbar()
    {
        Border bar = new()
        {
            Background = AppSurface,
            BorderBrush = AppBorder,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(12, 5, 12, 5),
            Visibility = Visibility.Collapsed
        };

        StackPanel row = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };

        _profileGitSafetyButton = CreateProfileToolButton(
            "Git safety",
            "Git safety",
            BuildSourceControlIcon());
        _profileGitSafetyButton.Click += (_, _) => GitSafety_Click(this, new RoutedEventArgs());
        row.Children.Add(_profileGitSafetyButton);

        Button gitInfo = HelpTipFactory.Create(new HelpTipSpec(
            "Check repository Git safety.",
            "Scans the active project for private-key material that could accidentally enter Git and provides the existing narrow ZDeployPet .gitignore safety option.",
            WhenToUse: "Use after changing deployment credentials or repository files, and before release-oriented work.",
            WhatItDoes: "The scan is read-only unless you explicitly choose the managed .gitignore action inside Git safety.",
            Safety: "It never makes an already tracked secret safe automatically and does not delete repository files."));
        gitInfo.Margin = new Thickness(0, 0, 14, 0);
        row.Children.Add(gitInfo);

        _profileDiscoveryButton = CreateProfileToolButton(
            "Run discovery",
            "Run discovery",
            BuildDiscoveryIcon());
        _profileDiscoveryButton.Click += (_, _) => DiscoverButton_Click(this, new RoutedEventArgs());
        row.Children.Add(_profileDiscoveryButton);

        Button discoveryInfo = HelpTipFactory.Create(new HelpTipSpec(
            "Inspect the profile environment without changing it.",
            "Discovery checks the active Windows project, WSL environment, deployment runner and report location against the saved profile.",
            WhenToUse: "Run after profile changes, WSL/project-path changes, or when checking why a deployment capability is unavailable.",
            WhatItDoes: "Performs local and WSL read-only capability checks and writes the result to the profile/discovery surface.",
            Safety: "Discovery does not upload files, run the deployment script, modify remote folders or perform a deployment."));
        discoveryInfo.Margin = new Thickness(0);
        row.Children.Add(discoveryInfo);

        bar.Child = row;
        return bar;
    }

    private Button CreateToolbarButton(string automationName, string toolTip, UIElement icon)
    {
        Button button = new()
        {
            Width = 38,
            Height = 34,
            Margin = new Thickness(0, 0, 6, 0),
            Padding = new Thickness(7),
            Background = AppSurface,
            BorderBrush = FindBrush("AppAccentBrush", Brushes.IndianRed),
            BorderThickness = new Thickness(1),
            Foreground = AppText,
            Content = icon,
            ToolTip = toolTip,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(button, automationName);
        AutomationProperties.SetHelpText(button, toolTip);
        return button;
    }

    private Button CreateProfileToolButton(string automationName, string toolTip, UIElement icon)
    {
        Button button = new()
        {
            Width = 34,
            Height = 32,
            Margin = new Thickness(0, 0, 5, 0),
            Padding = new Thickness(6),
            Background = AppSurfaceRaised,
            BorderBrush = AppBorder,
            BorderThickness = new Thickness(1),
            Foreground = AppText,
            Content = icon,
            ToolTip = toolTip,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(button, automationName);
        AutomationProperties.SetHelpText(button, toolTip);
        return button;
    }

    private UIElement BuildHomeIcon()
    {
        Grid icon = new() { Width = 18, Height = 18 };

        System.Windows.Shapes.Path roof = new()
        {
            Data = Geometry.Parse("M 2,8 L 9,2 L 16,8"),
            Stroke = AppText,
            StrokeThickness = 1.7,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Fill = Brushes.Transparent
        };
        icon.Children.Add(roof);

        System.Windows.Shapes.Path house = new()
        {
            Data = Geometry.Parse("M 4,7 L 4,16 L 14,16 L 14,7 M 8,16 L 8,11 L 11,11 L 11,16"),
            Stroke = AppText,
            StrokeThickness = 1.7,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Fill = Brushes.Transparent
        };
        icon.Children.Add(house);

        return icon;
    }

    private UIElement BuildProfileIcon()
    {
        Grid icon = new() { Width = 18, Height = 18 };
        icon.Children.Add(new Ellipse
        {
            Width = 6,
            Height = 6,
            Stroke = AppText,
            StrokeThickness = 1.6,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 1, 0, 0)
        });
        icon.Children.Add(new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse("M 3,17 C 3,12 6,9 9,9 C 12,9 15,12 15,17"),
            Stroke = AppText,
            StrokeThickness = 1.6,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Fill = Brushes.Transparent
        });
        return icon;
    }

    private UIElement BuildSourceControlIcon()
    {
        Grid icon = new() { Width = 18, Height = 18 };
        Line trunk = new()
        {
            X1 = 5, Y1 = 4, X2 = 5, Y2 = 14,
            Stroke = AppText,
            StrokeThickness = 1.5
        };
        Line branch = new()
        {
            X1 = 5, Y1 = 8, X2 = 13, Y2 = 8,
            Stroke = AppText,
            StrokeThickness = 1.5
        };
        icon.Children.Add(trunk);
        icon.Children.Add(branch);
        icon.Children.Add(BuildNode(5, 3));
        icon.Children.Add(BuildNode(5, 14));
        icon.Children.Add(BuildNode(13, 8));
        return icon;
    }

    private UIElement BuildDiscoveryIcon()
    {
        Grid icon = new() { Width = 18, Height = 18 };
        icon.Children.Add(new Ellipse
        {
            Width = 9,
            Height = 9,
            Stroke = AppText,
            StrokeThickness = 1.6,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(2, 2, 0, 0)
        });
        icon.Children.Add(new Line
        {
            X1 = 10,
            Y1 = 10,
            X2 = 16,
            Y2 = 16,
            Stroke = AppText,
            StrokeThickness = 1.8,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        });
        return icon;
    }

    private Ellipse BuildNode(double centerX, double centerY) => new()
    {
        Width = 4,
        Height = 4,
        Fill = AppSurface,
        Stroke = AppText,
        StrokeThickness = 1.4,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        Margin = new Thickness(centerX - 2, centerY - 2, 0, 0)
    };

    private void ShowHomeSurface()
    {
        if (_activeProfile is null)
        {
            ShowSetup("Create or load a local profile before opening Home.");
            ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Shell", "Home requested without an active profile; setup remains visible.");
            return;
        }

        ShowDiscovery(_activeProfile, "Home");
        HideLegacyProfileFooterActions();
        RefreshProfileStageToolbarVisibility();
        RefreshPetCompanion();
        RefreshPda5DryRunVisibility();
        RefreshPda6LiveReviewVisibility();
        _ = RefreshOperatorAccessControlAsync();
        _ = RefreshLatestDeploymentReportAsync();
        ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Shell", "Home opened.");
    }

    private void ShowProfileSurface()
    {
        if (_activeProfile is not null)
            PopulateEditor(_activeProfile);

        ShowSetup(_activeProfile is null
            ? "Create or load a deployment profile."
            : "Profile setup and readiness tools. Changes remain local until saved.");
        RefreshProfileStageToolbarVisibility();
        ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Shell", "Profile opened.");
    }

    private void RefreshProfileStageToolbarVisibility()
    {
        if (_profileStageToolbar is null) return;
        bool visible = SetupPanel.Visibility == Visibility.Visible;
        _profileStageToolbar.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        bool hasProfile = _activeProfile is not null;
        if (_profileGitSafetyButton is not null) _profileGitSafetyButton.IsEnabled = hasProfile;
        if (_profileDiscoveryButton is not null) _profileDiscoveryButton.IsEnabled = hasProfile;
    }

    private void HideLegacyProfileFooterActions()
    {
        EditProfileButton.Visibility = Visibility.Collapsed;
        DiscoverButton.Visibility = Visibility.Collapsed;
        if (_gitSafetyButton is not null)
            _gitSafetyButton.Visibility = Visibility.Collapsed;
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
