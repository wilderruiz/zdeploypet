using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZDeployPet.App;

internal static class Pda8ShellRuntime
{
    private static readonly HashSet<string> TopMenuLabels = new(StringComparer.Ordinal)
    {
        "Project / Profile",
        "View",
        "Help"
    };

    private static readonly HashSet<string> PopupActionLabels = new(StringComparer.Ordinal)
    {
        "Edit profile",
        "Deployment access…",
        "Session…",
        "Git safety…",
        "Exit",
        "Show / hide Output / Reports",
        "Incident bundle preview…",
        "About ZDeployPet…"
    };

    public static void ApplyWindow(Window window)
    {
        if (window is null) return;

        if (window is MainWindow)
        {
            window.Background = FindBrush("Pda8ShellBackgroundBrush", window.Background);
            window.Dispatcher.BeginInvoke(new Action(() => ApplyToDescendants(window)));
        }
    }

    public static void ApplyButton(Button button)
    {
        if (button.Content is not string label) return;

        if (TopMenuLabels.Contains(label))
        {
            if (Application.Current.TryFindResource("Pda8ShellMenuButtonStyle") is Style style)
                button.Style = style;

            StyleTopMenuContainer(button);
            return;
        }

        if (PopupActionLabels.Contains(label))
        {
            if (Application.Current.TryFindResource("Pda8ShellPopupActionStyle") is Style popupStyle)
                button.Style = popupStyle;

            StylePopupContainer(button);
        }
    }

    private static void StyleTopMenuContainer(DependencyObject element)
    {
        Border? border = FindAncestor<Border>(element);
        if (border is null) return;

        border.Background = FindBrush("Pda8ShellMenuBrush", border.Background);
        border.BorderBrush = FindBrush("Pda8ShellBorderBrush", border.BorderBrush);
        border.BorderThickness = new Thickness(0, 0, 0, 1);
    }

    private static void StylePopupContainer(DependencyObject element)
    {
        Border? border = FindAncestor<Border>(element);
        if (border is null) return;

        border.Background = FindBrush("Pda8ShellPopupBrush", border.Background);
        border.BorderBrush = FindBrush("Pda8ShellBorderBrush", border.BorderBrush);
        border.BorderThickness = new Thickness(1);
        border.CornerRadius = new CornerRadius(7);
    }

    private static T? FindAncestor<T>(DependencyObject start) where T : DependencyObject
    {
        DependencyObject? current = VisualTreeHelper.GetParent(start);
        while (current is not null)
        {
            if (current is T match) return match;
            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static void ApplyToDescendants(DependencyObject parent)
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int index = 0; index < count; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, index);
            if (child is Button button)
                ApplyButton(button);
            ApplyToDescendants(child);
        }
    }

    private static Brush FindBrush(string key, Brush fallback) =>
        Application.Current.TryFindResource(key) as Brush ?? fallback;
}
