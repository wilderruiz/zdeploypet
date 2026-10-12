using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda8V9ActivityReportsInitialized;

    private void InitializePda8V9ActivityReports()
    {
        if (_pda8V9ActivityReportsInitialized || _activityPane is null)
            return;

        _pda8V9ActivityReportsInitialized = true;

        Brush shell = FindBrush("AppBackgroundBrush", Brushes.Black);
        Brush surface = FindBrush("AppSurfaceBrush", Brushes.DarkSlateGray);
        Brush raised = FindBrush("AppSurfaceRaisedBrush", surface);
        Brush console = FindBrush("AppConsoleBrush", Brushes.Black);
        Brush border = FindBrush("AppBorderBrush", Brushes.DimGray);
        Brush borderStrong = FindBrush("AppBorderStrongBrush", border);
        Brush text = FindBrush("AppTextBrush", Brushes.White);
        Brush muted = FindBrush("AppMutedTextBrush", Brushes.LightGray);
        Brush accent = FindBrush("AppBrandAccentBrush", Brushes.Goldenrod);

        _activityPane.Background = shell;
        _activityPane.BorderBrush = border;

        if (_activityPane.Child is Grid root)
        {
            root.Margin = new Thickness(18, 16, 14, 14);
            root.Background = shell;

            TextBlock? title = FindV9TextBlock(root, "Output / Reports");
            if (title is not null)
            {
                title.Text = "Activity & reports";
                title.FontSize = 19;
                title.FontWeight = FontWeights.SemiBold;
                title.Foreground = text;
            }

            TextBlock? subtitle = FindV9TextBlock(root, "Latest Dry Run / Live deploy output, bounded ZDeployPet activity, and one shared report viewer.");
            if (subtitle is not null)
            {
                subtitle.Text = "Live deployment output, bounded ZDeployPet activity and trusted release records.";
                subtitle.FontSize = 11;
                subtitle.Foreground = muted;
                subtitle.Margin = new Thickness(0, 3, 0, 0);
            }

            Border? stateStrip = FindV9BorderAtGridRow(root, 1);
            if (stateStrip is not null)
            {
                stateStrip.Margin = new Thickness(0, 12, 0, 10);
                stateStrip.Padding = new Thickness(10, 6, 10, 6);
                stateStrip.Background = raised;
                stateStrip.BorderBrush = border;
                stateStrip.CornerRadius = new CornerRadius(8);
            }
        }

        if (_activityConsoleTabs is not null)
        {
            _activityConsoleTabs.Background = Brushes.Transparent;
            _activityConsoleTabs.BorderThickness = new Thickness(0);
            _activityConsoleTabs.Margin = new Thickness(0, 0, 0, 8);
            _activityConsoleTabs.Padding = new Thickness(0);

            Style tabStyle = BuildV9ActivityTabStyle(text, muted, accent);
            foreach (object item in _activityConsoleTabs.Items)
            {
                if (item is TabItem tab)
                    tab.Style = tabStyle;
            }
        }

        if (_activityConsoleTextBox is not null)
        {
            _activityConsoleTextBox.Background = console;
            _activityConsoleTextBox.Foreground = new SolidColorBrush(Color.FromRgb(0xD8, 0xDE, 0xE9));
            _activityConsoleTextBox.FontFamily = new FontFamily("Cascadia Mono, Consolas");
            _activityConsoleTextBox.FontSize = 11.5;
            _activityConsoleTextBox.Padding = new Thickness(12, 10, 12, 10);

            if (_activityConsoleTextBox.Parent is Border consoleBorder)
            {
                consoleBorder.Background = console;
                consoleBorder.BorderBrush = borderStrong;
                consoleBorder.BorderThickness = new Thickness(1);
                consoleBorder.CornerRadius = new CornerRadius(9);
            }
        }

        if (_activityStatusText is not null)
        {
            _activityStatusText.Foreground = muted;
            _activityStatusText.FontSize = 10.5;
            _activityStatusText.Margin = new Thickness(0, 0, 0, 5);
        }

        ApplyV9ActivityFooterButtons(_activityPane, raised, borderStrong, text);
    }

    private static Style BuildV9ActivityTabStyle(Brush text, Brush muted, Brush accent)
    {
        Style style = new(typeof(TabItem));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.ForegroundProperty, muted));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(11, 7, 11, 7)));
        style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, 3, 0)));
        style.Setters.Add(new Setter(Control.FontSizeProperty, 11.0));

        FrameworkElementFactory chrome = new(typeof(Border), "Chrome");
        chrome.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        chrome.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
        chrome.SetValue(Border.BorderThicknessProperty, new Thickness(0, 0, 0, 2));
        chrome.SetValue(Border.PaddingProperty, new Thickness(10, 7, 10, 6));

        FrameworkElementFactory content = new(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.ContentSourceProperty, "Header");
        content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        chrome.AppendChild(content);

        ControlTemplate template = new(typeof(TabItem))
        {
            VisualTree = chrome
        };

        Trigger selected = new()
        {
            Property = TabItem.IsSelectedProperty,
            Value = true
        };
        selected.Setters.Add(new Setter(Border.BorderBrushProperty, accent, "Chrome"));
        selected.Setters.Add(new Setter(Control.ForegroundProperty, text));
        template.Triggers.Add(selected);

        Trigger hover = new()
        {
            Property = UIElement.IsMouseOverProperty,
            Value = true
        };
        hover.Setters.Add(new Setter(Control.ForegroundProperty, text));
        template.Triggers.Add(hover);

        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        return style;
    }

    private static TextBlock? FindV9TextBlock(DependencyObject root, string exactText)
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int index = 0; index < count; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);
            if (child is TextBlock text && string.Equals(text.Text, exactText, StringComparison.Ordinal))
                return text;

            TextBlock? nested = FindV9TextBlock(child, exactText);
            if (nested is not null)
                return nested;
        }

        return null;
    }

    private static Border? FindV9BorderAtGridRow(Grid root, int row)
    {
        foreach (UIElement child in root.Children)
        {
            if (child is Border border && Grid.GetRow(border) == row)
                return border;
        }

        return null;
    }

    private static void ApplyV9ActivityFooterButtons(DependencyObject root, Brush background, Brush border, Brush text)
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int index = 0; index < count; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);
            if (child is Button button)
            {
                string? label = button.Content?.ToString();
                if (string.Equals(label, "Copy all", StringComparison.Ordinal) ||
                    string.Equals(label, "Copy selection", StringComparison.Ordinal))
                {
                    button.Background = background;
                    button.BorderBrush = border;
                    button.Foreground = text;
                    button.Padding = new Thickness(10, 5, 10, 5);
                    button.MinHeight = 28;
                }
            }

            ApplyV9ActivityFooterButtons(child, background, border, text);
        }
    }
}
