using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda8V10ProfileVisualsInitialized;

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

        // Intro copy becomes a quiet GitHub-like context card instead of a warning banner.
        if (content.Children.Count > 0 && content.Children[0] is Border intro)
        {
            intro.Background = surface;
            intro.BorderBrush = border;
            intro.BorderThickness = new Thickness(1);
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
                    group.Background = surface;
                    group.Foreground = text;
                    group.BorderBrush = border;
                    group.BorderThickness = new Thickness(1);
                    group.Padding = new Thickness(14, 12, 14, 14);
                    group.Margin = new Thickness(0);
                    group.FontSize = 13;
                    group.FontWeight = FontWeights.SemiBold;

                    ApplyPda8V10ProfileElementVisuals(group, surface, raised, border, text, muted, input);
                }
                else if (sectionChild is Button infoButton && string.Equals(infoButton.Content?.ToString(), "i", StringComparison.Ordinal))
                {
                    infoButton.Margin = new Thickness(0, 7, 8, 0);
                }
            }
        }

        StyleProfileDataGrid(TargetsGrid, surface, raised, border, text, muted);
        StyleProfileDataGrid(DestinationsGrid, surface, raised, border, text, muted);

        // The primary profile action should read as the single intentional completion action.
        SaveProfileButton.Padding = new Thickness(14, 7, 14, 7);
        SaveProfileButton.MinWidth = 150;
        SaveProfileButton.Background = new SolidColorBrush(Color.FromRgb(0x1D, 0x3B, 0x2A));
        SaveProfileButton.BorderBrush = new SolidColorBrush(Color.FromRgb(0x3F, 0x7D, 0x5B));
        SaveProfileButton.Foreground = text;

        ForgetSuggestionsButton.Padding = new Thickness(10, 5, 10, 5);
        ForgetSuggestionsButton.Background = surface;
        ForgetSuggestionsButton.BorderBrush = border;
        ForgetSuggestionsButton.Foreground = muted;

        StatusText.Foreground = muted;
        SetupValidationText.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x8A, 0x8A));
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
                    textBox.Padding = new Thickness(8, 5, 8, 5);
                    break;

                case ComboBox combo:
                    combo.Background = input;
                    combo.Foreground = text;
                    combo.BorderBrush = border;
                    combo.BorderThickness = new Thickness(1);
                    combo.MinHeight = 30;
                    break;

                case Button button when !string.Equals(button.Content?.ToString(), "i", StringComparison.Ordinal):
                    button.Background = raised;
                    button.Foreground = text;
                    button.BorderBrush = border;
                    break;

                case DataGrid grid:
                    grid.Background = surface;
                    grid.Foreground = text;
                    grid.BorderBrush = border;
                    grid.HorizontalGridLinesBrush = border;
                    grid.VerticalGridLinesBrush = border;
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
        grid.HorizontalGridLinesBrush = border;
        grid.VerticalGridLinesBrush = border;
        grid.HeadersVisibility = DataGridHeadersVisibility.Column;
        grid.RowHeaderWidth = 0;
        grid.ColumnHeaderHeight = 28;
        grid.RowHeight = 31;
        grid.FontSize = 11.5;

        Style headerStyle = new(typeof(DataGridColumnHeader));
        headerStyle.Setters.Add(new Setter(Control.BackgroundProperty, raised));
        headerStyle.Setters.Add(new Setter(Control.ForegroundProperty, muted));
        headerStyle.Setters.Add(new Setter(Control.BorderBrushProperty, border));
        headerStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
        headerStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 5, 8, 5)));
        headerStyle.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
        grid.ColumnHeaderStyle = headerStyle;
    }
}
