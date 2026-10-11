using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private enum HomeWorkspaceSection
    {
        Dry,
        Live,
        Reports
    }

    private bool _pda8HomeWorkspaceInitialized;
    private Button? _homeDryTabButton;
    private Button? _homeLiveTabButton;
    private Button? _homeReportsTabButton;
    private FrameworkElement? _homeDryContent;
    private FrameworkElement? _homeLiveContent;
    private FrameworkElement? _homeReportsContent;
    private HomeWorkspaceSection _homeWorkspaceSection = HomeWorkspaceSection.Reports;

    private void InitializePda8HomeWorkspace()
    {
        if (_pda8HomeWorkspaceInitialized)
            return;

        if (_operatorAccessCard?.Child is not Grid legacyActionGrid ||
            _dryRunButton is null ||
            _liveReviewButton is null ||
            _latestReportCard is null ||
            _deploymentHistoryCard is null)
        {
            return;
        }

        _pda8HomeWorkspaceInitialized = true;

        DetachWorkspaceElement(_dryRunButton);
        DetachWorkspaceElement(_liveReviewButton);
        if (_deploymentActionsHelpButton is not null)
            DetachWorkspaceElement(_deploymentActionsHelpButton);
        DetachWorkspaceElement(_latestReportCard);
        DetachWorkspaceElement(_deploymentHistoryCard);

        Grid root = new();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Grid tabs = new()
        {
            Margin = new Thickness(0, 0, 0, 12)
        };
        tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _homeDryTabButton = CreateHomeWorkspaceTab("DRY", "Dry-run workspace");
        _homeDryTabButton.Click += (_, _) => SelectHomeWorkspaceSection(HomeWorkspaceSection.Dry);
        Grid.SetColumn(_homeDryTabButton, 0);
        tabs.Children.Add(_homeDryTabButton);

        _homeLiveTabButton = CreateHomeWorkspaceTab("LIVE", "Live deployment workspace");
        _homeLiveTabButton.Click += (_, _) => SelectHomeWorkspaceSection(HomeWorkspaceSection.Live);
        Grid.SetColumn(_homeLiveTabButton, 1);
        tabs.Children.Add(_homeLiveTabButton);

        _homeReportsTabButton = CreateHomeWorkspaceTab("REPORTS", "Deployment reports and history");
        _homeReportsTabButton.Click += (_, _) => SelectHomeWorkspaceSection(HomeWorkspaceSection.Reports);
        Grid.SetColumn(_homeReportsTabButton, 2);
        tabs.Children.Add(_homeReportsTabButton);

        if (_deploymentActionsHelpButton is not null)
        {
            _deploymentActionsHelpButton.Margin = new Thickness(8, 0, 0, 0);
            _deploymentActionsHelpButton.HorizontalAlignment = HorizontalAlignment.Right;
            _deploymentActionsHelpButton.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(_deploymentActionsHelpButton, 4);
            tabs.Children.Add(_deploymentActionsHelpButton);
        }

        root.Children.Add(tabs);

        Grid contentHost = new();
        Grid.SetRow(contentHost, 1);
        root.Children.Add(contentHost);

        _homeDryContent = BuildTransitionalActionWorkspace(
            "Dry run",
            "Open the existing guarded dry-run flow. Its full controls will be embedded here in the next PDA-8 slice without changing execution authority.",
            _dryRunButton,
            isLive: false);
        contentHost.Children.Add(_homeDryContent);

        _homeLiveContent = BuildTransitionalActionWorkspace(
            "Live deployment",
            "Open the existing immutable live-deployment review. Its full confirmation flow will be embedded here after the DRY surface is verified.",
            _liveReviewButton,
            isLive: true);
        contentHost.Children.Add(_homeLiveContent);

        StackPanel reports = new();
        _latestReportCard.Margin = new Thickness(0, 0, 0, 12);
        _deploymentHistoryCard.Margin = new Thickness(0);
        reports.Children.Add(_latestReportCard);
        reports.Children.Add(_deploymentHistoryCard);
        _homeReportsContent = reports;
        contentHost.Children.Add(_homeReportsContent);

        _operatorAccessCard.Padding = new Thickness(14);
        _operatorAccessCard.Child = root;

        SelectHomeWorkspaceSection(HomeWorkspaceSection.Reports);
    }

    private FrameworkElement BuildTransitionalActionWorkspace(
        string heading,
        string description,
        Button actionButton,
        bool isLive)
    {
        Border surface = new()
        {
            Padding = new Thickness(14),
            Background = FindBrush("AppSurfaceBrush", Brushes.DarkSlateGray),
            BorderBrush = new SolidColorBrush(isLive
                ? Color.FromRgb(0x4E, 0x8B, 0x63)
                : Color.FromRgb(0x4F, 0x6F, 0x96)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4)
        };

        StackPanel body = new();
        body.Children.Add(new TextBlock
        {
            Text = heading,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = FindBrush("AppTextBrush", Brushes.White)
        });
        body.Children.Add(new TextBlock
        {
            Text = description,
            Margin = new Thickness(0, 5, 0, 12),
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        });

        actionButton.Margin = new Thickness(0);
        body.Children.Add(actionButton);
        surface.Child = body;
        return surface;
    }

    private Button CreateHomeWorkspaceTab(string label, string toolTip)
    {
        return new Button
        {
            Content = label,
            ToolTip = toolTip,
            MinWidth = 104,
            MinHeight = 40,
            Padding = new Thickness(18, 8, 18, 8),
            Margin = new Thickness(0, 0, 8, 0),
            FontWeight = FontWeights.SemiBold,
            Foreground = FindBrush("AppTextBrush", Brushes.White),
            Background = FindBrush("AppSurfaceBrush", Brushes.DarkSlateGray),
            BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand
        };
    }

    private void SelectHomeWorkspaceSection(HomeWorkspaceSection section)
    {
        _homeWorkspaceSection = section;

        if (_homeDryContent is not null)
            _homeDryContent.Visibility = section == HomeWorkspaceSection.Dry ? Visibility.Visible : Visibility.Collapsed;
        if (_homeLiveContent is not null)
            _homeLiveContent.Visibility = section == HomeWorkspaceSection.Live ? Visibility.Visible : Visibility.Collapsed;
        if (_homeReportsContent is not null)
            _homeReportsContent.Visibility = section == HomeWorkspaceSection.Reports ? Visibility.Visible : Visibility.Collapsed;

        ApplyHomeWorkspaceTabVisual(_homeDryTabButton, section == HomeWorkspaceSection.Dry, HomeWorkspaceSection.Dry);
        ApplyHomeWorkspaceTabVisual(_homeLiveTabButton, section == HomeWorkspaceSection.Live, HomeWorkspaceSection.Live);
        ApplyHomeWorkspaceTabVisual(_homeReportsTabButton, section == HomeWorkspaceSection.Reports, HomeWorkspaceSection.Reports);
    }

    private void ApplyHomeWorkspaceTabVisual(Button? button, bool selected, HomeWorkspaceSection section)
    {
        if (button is null)
            return;

        if (!selected)
        {
            button.Background = FindBrush("AppSurfaceBrush", Brushes.DarkSlateGray);
            button.BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray);
            button.Opacity = 0.82;
            return;
        }

        (Color background, Color border) = section switch
        {
            HomeWorkspaceSection.Dry => (Color.FromRgb(0x24, 0x36, 0x4A), Color.FromRgb(0x4F, 0x6F, 0x96)),
            HomeWorkspaceSection.Live => (Color.FromRgb(0x24, 0x3A, 0x2C), Color.FromRgb(0x4E, 0x8B, 0x63)),
            _ => (Color.FromRgb(0x3B, 0x32, 0x20), Color.FromRgb(0x8B, 0x73, 0x3C))
        };

        button.Background = new SolidColorBrush(background);
        button.BorderBrush = new SolidColorBrush(border);
        button.Opacity = 1.0;
    }

    private static void DetachWorkspaceElement(UIElement element)
    {
        switch (element.Parent)
        {
            case Panel panel:
                panel.Children.Remove(element);
                break;
            case Decorator decorator when ReferenceEquals(decorator.Child, element):
                decorator.Child = null;
                break;
            case ContentControl contentControl when ReferenceEquals(contentControl.Content, element):
                contentControl.Content = null;
                break;
        }
    }
}
