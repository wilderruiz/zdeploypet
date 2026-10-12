using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

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

        // PDA-8 now owns ScrollBar/ScrollViewer and lightweight information-control
        // presentation through shared resource dictionaries. ThemeRuntime remains only
        // as a compatibility bridge for older windows that still carry legacy literal
        // brushes in their existing visual trees.
        ApplyToDescendants(window);
        window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => ApplyToDescendants(window)));
        window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => ApplyToDescendants(window)));
    }

    public static void ApplyToolTip(ToolTip toolTip)
    {
        if (Application.Current.TryFindResource("SharedToolTipStyle") is Style style)
            toolTip.Style = style;

        Brush text = ResourceBrush("AppTextBrush", Brushes.White);
        toolTip.Background = ResourceBrush("AppSurfaceRaisedBrush", Brushes.DarkSlateGray);
        toolTip.Foreground = text;
        toolTip.BorderBrush = ResourceBrush("AppSurfaceRaisedBrush", Brushes.DarkSlateGray);

        if (toolTip.Content is DependencyObject content)
        {
            // Tooltip content is often still disconnected from the visual tree when
            // this method runs. Walk the logical tree so explicitly muted TextBlocks
            // (for example old #555 helper lines) are promoted to normal tooltip text.
            ApplyToolTipLogicalTextRecursive(content, text);
            toolTip.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(() => ApplyToolTipLogicalTextRecursive(content, text)));
        }
    }

    private static void ApplyToolTipLogicalTextRecursive(DependencyObject parent, Brush text)
    {
        if (parent is TextBlock textBlock)
            textBlock.Foreground = text;

        foreach (object child in LogicalTreeHelper.GetChildren(parent))
        {
            if (child is DependencyObject dependencyObject)
                ApplyToolTipLogicalTextRecursive(dependencyObject, text);
        }
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
