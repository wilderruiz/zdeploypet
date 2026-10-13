using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda8V10ProfileVisualsInitialized;
    private static ControlTemplate? _pda8ProfileGroupBoxTemplate;
    private static ControlTemplate? _pda8ProfileButtonTemplate;

    private void InitializePda8V10ProfileVisuals()
    {
        if (_pda8V10ProfileVisualsInitialized) return;
        _pda8V10ProfileVisualsInitialized = true;

        SetupPanel.IsVisibleChanged += (_, _) =>
        {
            if (SetupPanel.Visibility == Visibility.Visible)
                Dispatcher.BeginInvoke(new Action(ApplyPda8V10ProfileVisuals));
        };

        SetupPanel.Loaded += (_, _) => ApplyPda8V10ProfileVisuals();
        ApplyPda8V10ProfileVisuals();
    }

    private void ApplyPda8V10ProfileVisuals()
    {
        if (SetupPanel.Content is not StackPanel content)
            return;

        Brush shell = FindBrush("AppBackgroundBrush", Brushes.Black);
        Brush surface = FindBrush("AppSurfaceBrush", Brushes.DarkSlateGray);
        Brush raised = FindBrush("AppSurfaceRaisedBrush", surface);
        Brush border = FindBrush("AppBorderBrush", Brushes.DimGray);
        Brush text = FindBrush("AppTextBrush", Brushes.WhiteSmoke);
        Brush muted = FindBrush("AppMutedTextBrush", Brushes.LightGray);
        Brush input = FindBrush("AppInputBackgroundBrush", Brushes.Black);

        SetupPanel.Background = shell;
        SetupPanel.Padding = new Thickness(0);
        content.Background = shell;
        content.Margin = new Thickness(0, 0, 10, 0);

        // Intro copy is passive context rather than an armed/action surface.
        // P5 lets the layer contrast and spacing carry its hierarchy without
        // another visible rectangular outline.
        if (content.Children.Count > 0 && content.Children[0] is Border intro)
        {
            intro.Background = surface;
            intro.BorderBrush = Brushes.Transparent;
            intro.BorderThickness = new Thickness(0);
            intro.CornerRadius = new CornerRadius(8);
            intro.Padding = new Thickness(14, 12, 14, 12);
            intro.Margin = new Thickness(0, 0, 0, 14);

            if (intro.Child is TextBlock introText)
            {
                introText.Foreground = muted;
                introText.FontSize = 12;
                introText.LineHeight = 18;
            }
        }

        foreach (UIElement child in content.Children)
        {
            if (child is not Grid sectionGrid)
                continue;

            sectionGrid.Margin = new Thickness(0, 0, 0, 12);

            foreach (UIElement sectionChild in sectionGrid.Children)
            {
                if (sectionChild is GroupBox group)
                {
                    // Profile owns its own GroupBox template. The stock Windows
                    // fieldset template can draw bright system-theme edges even when
                    // BorderBrush is dark, so replace it with one continuous PDA-8 card.
                    group.Template = GetPda8ProfileGroupBoxTemplate();
                    group.Background = surface;
                    group.Foreground = text;
                    group.BorderBrush = border;
                    group.BorderThickness = new Thickness(1);
                    group.Padding = new Thickness(14, 8, 14, 14);
                    group.Margin = new Thickness(0);
                    group.FontSize = 13;
                    group.FontWeight = FontWeights.SemiBold;

                    ApplyPda8V10ProfileElementVisuals(group, surface, raised, border, text, muted, input);
                }
                else if (sectionChild is Button infoButton && string.Equals(infoButton.Content?.ToString(), "i", StringComparison.Ordinal))
                {
                    if (Application.Current.TryFindResource("SharedInfoButtonStyle") is Style sharedInfoStyle)
                        infoButton.Style = sharedInfoStyle;

                    infoButton.Margin = new Thickness(0, 7, 8, 0);
                    infoButton.HorizontalAlignment = HorizontalAlignment.Right;
                    infoButton.VerticalAlignment = VerticalAlignment.Top;
                }
            }
        }

        StyleProfileDataGrid(TargetsGrid, surface, raised, border, text, muted);
        StyleProfileDataGrid(DestinationsGrid, surface, raised, border, text, muted);

        // The primary profile action should read as the single intentional completion action.
        ApplyPda8ProfileButtonChrome(SaveProfileButton, text, border);
        SaveProfileButton.Padding = new Thickness(14, 7, 14, 7);
        SaveProfileButton.MinWidth = 150;
        SaveProfileButton.Background = new SolidColorBrush(Color.FromRgb(0x1D, 0x3B, 0x2A));
        SaveProfileButton.BorderBrush = new SolidColorBrush(Color.FromRgb(0x3F, 0x7D, 0x5B));

        ApplyPda8ProfileButtonChrome(ForgetSuggestionsButton, muted, border);
        ForgetSuggestionsButton.Padding = new Thickness(10, 5, 10, 5);
        ForgetSuggestionsButton.Background = surface;

        StatusText.Foreground = muted;
        SetupValidationText.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x8A, 0x8A));
    }

    private static ControlTemplate GetPda8ProfileGroupBoxTemplate()
    {
        if (_pda8ProfileGroupBoxTemplate is not null)
            return _pda8ProfileGroupBoxTemplate;

        const string templateXaml = """
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                             TargetType="{x:Type GroupBox}">
                <Border Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        CornerRadius="8">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto" />
                            <RowDefinition Height="*" />
                        </Grid.RowDefinitions>
                        <ContentPresenter Grid.Row="0"
                                          ContentSource="Header"
                                          Margin="14,11,14,0"
                                          HorizontalAlignment="Left"
                                          RecognizesAccessKey="True" />
                        <ContentPresenter Grid.Row="1"
                                          Margin="{TemplateBinding Padding}"
                                          HorizontalAlignment="Stretch"
                                          VerticalAlignment="Stretch" />
                    </Grid>
                </Border>
            </ControlTemplate>
            """;

        _pda8ProfileGroupBoxTemplate = (ControlTemplate)XamlReader.Parse(templateXaml);
        return _pda8ProfileGroupBoxTemplate;
    }

    private static ControlTemplate GetPda8ProfileButtonTemplate()
    {
        if (_pda8ProfileButtonTemplate is not null)
            return _pda8ProfileButtonTemplate;

        const string templateXaml = """
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                             TargetType="{x:Type Button}">
                <Border x:Name="Chrome"
                        Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        CornerRadius="6">
                    <ContentPresenter Margin="{TemplateBinding Padding}"
                                      HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
                                      VerticalAlignment="{TemplateBinding VerticalContentAlignment}"
                                      RecognizesAccessKey="True" />
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter TargetName="Chrome" Property="Background" Value="#30343D" />
                        <Setter TargetName="Chrome" Property="BorderBrush" Value="#B98A35" />
                    </Trigger>
                    <Trigger Property="IsKeyboardFocused" Value="True">
                        <Setter TargetName="Chrome" Property="BorderBrush" Value="#B98A35" />
                    </Trigger>
                    <Trigger Property="IsPressed" Value="True">
                        <Setter TargetName="Chrome" Property="Background" Value="#383D47" />
                    </Trigger>
                    <Trigger Property="IsEnabled" Value="False">
                        <Setter TargetName="Chrome" Property="Background" Value="#20242B" />
                        <Setter TargetName="Chrome" Property="BorderBrush" Value="#343943" />
                        <Setter Property="Opacity" Value="0.72" />
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
            """;

        _pda8ProfileButtonTemplate = (ControlTemplate)XamlReader.Parse(templateXaml);
        return _pda8ProfileButtonTemplate;
    }

    private static void ApplyPda8ProfileButtonChrome(Button button, Brush foreground, Brush border)
    {
        button.Template = GetPda8ProfileButtonTemplate();
        button.Foreground = foreground;
        button.BorderBrush = border;
        button.BorderThickness = new Thickness(1);
        button.Padding = new Thickness(11, 6, 11, 6);
        button.MinHeight = 30;
    }

    private static void ApplyPda8V10ProfileElementVisuals(
        DependencyObject parent,
        Brush surface,
        Brush raised,
        Brush border,
        Brush text,
        Brush muted,
        Brush input)
    {
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int index = 0; index < childCount; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, index);

            switch (child)
            {
                case TextBlock label:
                    label.Foreground = text;
                    if (label.FontWeight != FontWeights.Bold && label.FontWeight != FontWeights.SemiBold)
                    {
                        label.FontWeight = FontWeights.Normal;
                        label.FontSize = Math.Max(11, label.FontSize);
                    }
                    break;

                case TextBox textBox:
                    textBox.Background = input;
                    textBox.Foreground = text;
                    textBox.BorderBrush = border;
                    textBox.BorderThickness = new Thickness(1);
                    textBox.Padding = new Thickness(10, 6, 10, 6);
                    textBox.MinHeight = 31;
                    break;

                case ComboBox combo:
                    combo.Background = input;
                    combo.Foreground = text;
                    combo.BorderBrush = border;
                    combo.BorderThickness = new Thickness(1);
                    combo.Padding = new Thickness(10, 5, 30, 5);
                    combo.MinHeight = 31;
                    break;

                case Button button when !string.Equals(button.Content?.ToString(), "i", StringComparison.Ordinal):
                    ApplyPda8ProfileButtonChrome(button, text, border);
                    button.Background = raised;
                    break;

                case DataGrid grid:
                    grid.Background = surface;
                    grid.Foreground = text;
                    grid.BorderBrush = border;
                    grid.HorizontalGridLinesBrush = border;
                    grid.VerticalGridLinesBrush = Brushes.Transparent;
                    grid.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
                    break;
            }

            ApplyPda8V10ProfileElementVisuals(child, surface, raised, border, text, muted, input);
        }
    }

    private static void StyleProfileDataGrid(
        DataGrid grid,
        Brush surface,
        Brush raised,
        Brush border,
        Brush text,
        Brush muted)
    {
        grid.Background = surface;
        grid.Foreground = text;
        grid.BorderBrush = border;
        grid.BorderThickness = new Thickness(1);
        grid.RowBackground = surface;
        grid.AlternatingRowBackground = raised;
        grid.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
        grid.HorizontalGridLinesBrush = border;
        grid.VerticalGridLinesBrush = Brushes.Transparent;
        grid.HeadersVisibility = DataGridHeadersVisibility.Column;
        grid.RowHeaderWidth = 0;
        grid.ColumnHeaderHeight = 29;
        grid.RowHeight = 31;
        grid.FontSize = 11.5;

        Style headerStyle = new(typeof(DataGridColumnHeader));
        headerStyle.Setters.Add(new Setter(Control.BackgroundProperty, raised));
        headerStyle.Setters.Add(new Setter(Control.ForegroundProperty, muted));
        headerStyle.Setters.Add(new Setter(Control.BorderBrushProperty, border));
        headerStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
        headerStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(9, 5, 9, 5)));
        headerStyle.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
        grid.ColumnHeaderStyle = headerStyle;
    }
}
