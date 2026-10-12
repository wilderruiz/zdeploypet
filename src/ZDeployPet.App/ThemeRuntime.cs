using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ZDeployPet.App;

internal static class ThemeRuntime
{
    public static void Apply(Window window)
    {
        // PDA-8 now owns control, scrollbar, border and information-affordance
        // presentation through shared resource dictionaries and explicit page owners.
        // Keep only the minimal standalone-window compatibility boundary here.
        window.Background = ResourceBrush("AppBackgroundBrush", Brushes.Black);
        window.Foreground = ResourceBrush("AppTextBrush", Brushes.White);
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
            // Tooltip content can still be disconnected from the visual tree when
            // this method runs. Walk the logical tree so legacy helper TextBlocks
            // inherit normal readable tooltip text.
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

    private static Brush ResourceBrush(string key, Brush fallback) =>
        Application.Current.TryFindResource(key) as Brush ?? fallback;
}
