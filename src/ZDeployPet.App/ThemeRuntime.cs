using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZDeployPet.App;

internal static class ThemeRuntime
{
    private static readonly HashSet<string> LegacyMutedForegrounds = new(StringComparer.OrdinalIgnoreCase)
    {
        "#FF555555",
        "#FF666666"
    };

    private static readonly HashSet<string> LegacyWarningBackgrounds = new(StringComparer.OrdinalIgnoreCase)
    {
        "#FFFFF5E6",
        "#FFFFF6E5"
    };

    private static readonly HashSet<string> LegacyInfoBackgrounds = new(StringComparer.OrdinalIgnoreCase)
    {
        "#FFEEF6FF"
    };

    private static readonly HashSet<string> LegacyWarningBorders = new(StringComparer.OrdinalIgnoreCase)
    {
        "#FFE6C77A",
        "#FFE6C46A"
    };

    private static readonly HashSet<string> LegacyNeutralBorders = new(StringComparer.OrdinalIgnoreCase)
    {
        "#FFAACCE8",
        "#FFC9D7E5",
        "#FFD8D8D8"
    };

    public static void Apply(Window window)
    {
        Brush background = ResourceBrush("AppBackgroundBrush", Brushes.Black);
        Brush text = ResourceBrush("AppTextBrush", Brushes.White);
        window.Background = background;
        window.Foreground = text;

        ApplyToDescendants(window);
    }

    public static void ApplyToolTip(ToolTip toolTip)
    {
        if (Application.Current.TryFindResource("SharedToolTipStyle") is Style style)
            toolTip.Style = style;

        toolTip.Background = ResourceBrush("AppSurfaceRaisedBrush", Brushes.DarkSlateGray);
        toolTip.Foreground = ResourceBrush("AppTextBrush", Brushes.White);
        toolTip.BorderBrush = ResourceBrush("AppSurfaceRaisedBrush", Brushes.DarkSlateGray);
    }

    private static void ApplyToDescendants(DependencyObject parent)
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int index = 0; index < count; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, index);
            ApplyElement(child);
            ApplyToDescendants(child);
        }
    }

    private static void ApplyElement(DependencyObject element)
    {
        if (element is TextBlock textBlock && IsLegacyBrush(textBlock.Foreground, LegacyMutedForegrounds))
            textBlock.Foreground = ResourceBrush("AppMutedTextBrush", Brushes.LightGray);

        if (element is Button button && Equals(button.Content, "i"))
        {
            Thickness margin = button.Margin;
            HorizontalAlignment horizontalAlignment = button.HorizontalAlignment;
            VerticalAlignment verticalAlignment = button.VerticalAlignment;

            if (Application.Current.TryFindResource("SharedInfoButtonStyle") is Style style)
                button.Style = style;

            button.Margin = margin;
            button.HorizontalAlignment = horizontalAlignment;
            button.VerticalAlignment = verticalAlignment;
            button.Background = ResourceBrush("AppSurfaceRaisedBrush", Brushes.DarkSlateGray);
            button.BorderBrush = ResourceBrush("AppSurfaceRaisedBrush", Brushes.DarkSlateGray);
            button.Foreground = ResourceBrush("AppTextBrush", Brushes.White);
        }

        if (element is Border border)
        {
            if (IsLegacyBrush(border.Background, LegacyWarningBackgrounds))
                border.Background = new SolidColorBrush(Color.FromRgb(0x11, 0x13, 0x18));
            else if (IsLegacyBrush(border.Background, LegacyInfoBackgrounds))
                border.Background = ResourceBrush("AppSurfaceRaisedBrush", Brushes.DarkSlateGray);

            if (IsLegacyBrush(border.BorderBrush, LegacyWarningBorders))
                border.BorderBrush = ResourceBrush("AppBorderBrush", Brushes.DimGray);
            else if (IsLegacyBrush(border.BorderBrush, LegacyNeutralBorders))
                border.BorderBrush = ResourceBrush("AppBorderBrush", Brushes.DimGray);
        }
    }

    private static bool IsLegacyBrush(Brush? brush, HashSet<string> values) =>
        brush is SolidColorBrush solid && values.Contains(solid.Color.ToString());

    private static Brush ResourceBrush(string key, Brush fallback) =>
        Application.Current.TryFindResource(key) as Brush ?? fallback;
}
