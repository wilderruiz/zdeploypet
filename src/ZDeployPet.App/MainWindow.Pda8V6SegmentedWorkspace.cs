using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda8V6SegmentedWorkspaceInitialized;
    private bool _pda8V6WorkspaceHeadingAdded;
    private static ControlTemplate? _pda8V6FirstSegmentTemplate;
    private static ControlTemplate? _pda8V6MiddleSegmentTemplate;
    private static ControlTemplate? _pda8V6LastSegmentTemplate;

    private void InitializePda8V6SegmentedWorkspace()
    {
        if (_pda8V6SegmentedWorkspaceInitialized)
            return;

        if (_homeDryTabButton is null ||
            _homeLiveTabButton is null ||
            _homeReportsTabButton is null ||
            _homeDryTabButton.Parent is not Grid tabs ||
            tabs.Parent is not Grid root)
        {
            return;
        }

        _pda8V6SegmentedWorkspaceInitialized = true;

        // V6 keeps the existing Home workspace ownership and click handlers.
        // This is presentation only: three actions now read as one compact
        // segmented selector rather than three independent command buttons.
        CompleteV6WorkspaceHeading(root, tabs);
        ApplyPda8V11WorkspaceContainment();

        tabs.Margin = new Thickness(0, 0, 0, 12);
        tabs.VerticalAlignment = VerticalAlignment.Top;

        BuildV6SegmentCapsule(tabs);

        Style? segmentedStyle = Application.Current.TryFindResource("Pda8SegmentedTabStyle") as Style;

        ApplyV6SegmentBase(_homeDryTabButton, segmentedStyle, isFirst: true, isLast: false);
        ApplyV6SegmentBase(_homeLiveTabButton, segmentedStyle, isFirst: false, isLast: false);
        ApplyV6SegmentBase(_homeReportsTabButton, segmentedStyle, isFirst: false, isLast: true);

        // The established selector method remains authoritative for which tab
        // is active. Re-run it after the V6 base styling and after each click
        // so semantic DRY/LIVE/REPORTS state colours remain current.
        _homeDryTabButton.Click += (_, _) => RefreshV6SegmentedWorkspaceVisuals();
        _homeLiveTabButton.Click += (_, _) => RefreshV6SegmentedWorkspaceVisuals();
        _homeReportsTabButton.Click += (_, _) => RefreshV6SegmentedWorkspaceVisuals();

        RefreshV6SegmentedWorkspaceVisuals();
        InitializePda8V7WorkflowVisuals();
    }

    private void CompleteV6WorkspaceHeading(Grid root, Grid tabs)
    {
        if (_pda8V6WorkspaceHeadingAdded)
            return;

        _pda8V6WorkspaceHeadingAdded = true;

        UIElement? contentHost = root.Children
            .Cast<UIElement>()
            .FirstOrDefault(child => !ReferenceEquals(child, tabs));

        root.RowDefinitions.Clear();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // V11.1: the selected DRY/LIVE/REPORTS surface owns only the remaining
        // visible workspace height. Previously this row was Auto, which let the
        // child request its full desired height and pushed the outer workspace
        // card below the visible Home viewport even though the child itself had
        // a ScrollViewer.
        root.RowDefinitions.Add(new RowDefinition
        {
            Height = new GridLength(1, GridUnitType.Star),
            MinHeight = 0
        });

        StackPanel heading = new()
        {
            Margin = new Thickness(0, 0, 0, 14)
        };
        heading.Children.Add(new TextBlock
        {
            Text = "Deployment workspace",
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Foreground = FindBrush("AppTextBrush", Brushes.White)
        });
        heading.Children.Add(new TextBlock
        {
            Text = "One place for DRY, LIVE and larger report inspection.",
            Margin = new Thickness(0, 4, 0, 0),
            FontSize = 11,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        });

        Grid.SetRow(heading, 0);
        root.Children.Add(heading);
        Grid.SetRow(tabs, 1);
        if (contentHost is FrameworkElement contentElement)
        {
            Grid.SetRow(contentElement, 2);
            contentElement.VerticalAlignment = VerticalAlignment.Stretch;
        }
    }

    private void BuildV6SegmentCapsule(Grid tabs)
    {
        if (_homeDryTabButton is null || _homeLiveTabButton is null || _homeReportsTabButton is null)
            return;

        if (!tabs.Children.Contains(_homeDryTabButton) ||
            !tabs.Children.Contains(_homeLiveTabButton) ||
            !tabs.Children.Contains(_homeReportsTabButton))
        {
            return;
        }

        tabs.Children.Remove(_homeDryTabButton);
        tabs.Children.Remove(_homeLiveTabButton);
        tabs.Children.Remove(_homeReportsTabButton);

        Grid segmentHost = new();
        segmentHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        segmentHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        segmentHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(_homeDryTabButton, 0);
        Grid.SetColumn(_homeLiveTabButton, 1);
        Grid.SetColumn(_homeReportsTabButton, 2);
        segmentHost.Children.Add(_homeDryTabButton);
        segmentHost.Children.Add(_homeLiveTabButton);
        segmentHost.Children.Add(_homeReportsTabButton);

        Border capsule = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = FindBrush("AppInputBackgroundBrush", Brushes.Black),
            BorderBrush = FindBrush("AppBorderStrongBrush", Brushes.DimGray),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Child = segmentHost
        };

        Grid.SetColumn(capsule, 0);
        Grid.SetColumnSpan(capsule, 3);
        tabs.Children.Add(capsule);
    }

    private void ApplyV6SegmentBase(Button button, Style? segmentedStyle, bool isFirst, bool isLast)
    {
        if (segmentedStyle is not null)
            button.Style = segmentedStyle;

        button.Template = GetV6SegmentTemplate(isFirst, isLast);
        button.MinWidth = 92;
        button.MinHeight = 34;
        button.Padding = new Thickness(14, 6, 14, 6);
        button.Margin = new Thickness(0);
        button.BorderThickness = isLast ? new Thickness(0) : new Thickness(0, 0, 1, 0);
        button.FontSize = 11;
        button.FontWeight = FontWeights.SemiBold;
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.VerticalContentAlignment = VerticalAlignment.Center;
        button.Opacity = 1.0;
    }

    private static ControlTemplate GetV6SegmentTemplate(bool isFirst, bool isLast)
    {
        ControlTemplate? cached = isFirst
            ? _pda8V6FirstSegmentTemplate
            : isLast
                ? _pda8V6LastSegmentTemplate
                : _pda8V6MiddleSegmentTemplate;

        if (cached is not null)
            return cached;

        string radius = isFirst ? "8,0,0,8" : isLast ? "0,8,8,0" : "0";
        string xaml = $"""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                             TargetType="{{x:Type Button}}">
                <Border x:Name="Chrome"
                        Background="{{TemplateBinding Background}}"
                        BorderBrush="{{TemplateBinding BorderBrush}}"
                        BorderThickness="{{TemplateBinding BorderThickness}}"
                        CornerRadius="{radius}"
                        SnapsToDevicePixels="True">
                    <ContentPresenter Margin="{{TemplateBinding Padding}}"
                                      HorizontalAlignment="{{TemplateBinding HorizontalContentAlignment}}"
                                      VerticalAlignment="{{TemplateBinding VerticalContentAlignment}}"
                                      RecognizesAccessKey="True" />
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter Property="Foreground" Value="#F4F1EA" />
                    </Trigger>
                    <Trigger Property="IsKeyboardFocused" Value="True">
                        <Setter Property="Foreground" Value="#DFC087" />
                    </Trigger>
                    <Trigger Property="IsPressed" Value="True">
                        <Setter TargetName="Chrome" Property="Opacity" Value="0.88" />
                    </Trigger>
                    <Trigger Property="IsEnabled" Value="False">
                        <Setter Property="Foreground" Value="#687383" />
                        <Setter TargetName="Chrome" Property="Opacity" Value="0.72" />
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
            """;

        ControlTemplate template = (ControlTemplate)XamlReader.Parse(xaml);
        if (isFirst)
            _pda8V6FirstSegmentTemplate = template;
        else if (isLast)
            _pda8V6LastSegmentTemplate = template;
        else
            _pda8V6MiddleSegmentTemplate = template;

        return template;
    }

    private void RefreshV6SegmentedWorkspaceVisuals()
    {
        ApplyV6SegmentVisual(_homeDryTabButton, _homeWorkspaceSection == HomeWorkspaceSection.Dry, HomeWorkspaceSection.Dry);
        ApplyV6SegmentVisual(_homeLiveTabButton, _homeWorkspaceSection == HomeWorkspaceSection.Live, HomeWorkspaceSection.Live);
        ApplyV6SegmentVisual(_homeReportsTabButton, _homeWorkspaceSection == HomeWorkspaceSection.Reports, HomeWorkspaceSection.Reports);
    }

    private void ApplyV6SegmentVisual(Button? button, bool selected, HomeWorkspaceSection section)
    {
        if (button is null)
            return;

        if (!selected)
        {
            button.Background = FindBrush("AppInputBackgroundBrush", Brushes.Black);
            button.BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray);
            button.Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray);
            button.Opacity = 1.0;
            return;
        }

        switch (section)
        {
            case HomeWorkspaceSection.Dry:
                button.Background = FindBrush("AppDryBrush", new SolidColorBrush(Color.FromRgb(0x17, 0x23, 0x32)));
                button.BorderBrush = FindBrush("AppDryBorderBrush", new SolidColorBrush(Color.FromRgb(0x34, 0x47, 0x5B)));
                button.Foreground = FindBrush("AppDryTextBrush", Brushes.White);
                break;

            case HomeWorkspaceSection.Live:
                button.Background = FindBrush("AppLiveBrush", new SolidColorBrush(Color.FromRgb(0x14, 0x29, 0x1F)));
                button.BorderBrush = FindBrush("AppLiveBorderBrush", new SolidColorBrush(Color.FromRgb(0x34, 0x5E, 0x49)));
                button.Foreground = FindBrush("AppLiveTextBrush", Brushes.White);
                break;

            default:
                button.Background = FindBrush("AppReportsBrush", new SolidColorBrush(Color.FromRgb(0x28, 0x21, 0x31)));
                button.BorderBrush = FindBrush("AppReportsBorderBrush", new SolidColorBrush(Color.FromRgb(0x55, 0x49, 0x67)));
                button.Foreground = FindBrush("AppReportsTextBrush", Brushes.White);
                break;
        }

        button.Opacity = 1.0;
    }
}
