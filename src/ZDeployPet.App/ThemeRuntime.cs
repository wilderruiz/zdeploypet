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

        if (element is Border border)
        {
            if (IsLegacyBrush(border.Background, LegacyWarningBackgrounds))
                border.Background = ResourceBrush("AppWarningBackgroundBrush", Brushes.DarkGoldenrod);
            else if (IsLegacyBrush(border.Background, LegacyInfoBackgrounds))
                border.Background = ResourceBrush("AppSurfaceRaisedBrush", Brushes.DarkSlateGray);

            if (IsLegacyBrush(border.BorderBrush, LegacyWarningBorders))
                border.BorderBrush = ResourceBrush("AppWarningBorderBrush", Brushes.Goldenrod);
            else if (IsLegacyBrush(border.BorderBrush, LegacyNeutralBorders))
                border.BorderBrush = ResourceBrush("AppBorderBrush", Brushes.DimGray);
        }
    }

    private static bool IsLegacyBrush(Brush? brush, HashSet<string> values) =>
        brush is SolidColorBrush solid && values.Contains(solid.Color.ToString());

    private static Brush ResourceBrush(string key, Brush fallback) =>
        Application.Current.TryFindResource(key) as Brush ?? fallback;
}
