using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private static readonly bool Pda8P7ShellAccentRegistered = RegisterPda8P7ShellAccent();
    private bool _pda8P7ShellAccentApplied;

    private static bool RegisterPda8P7ShellAccent()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(MainWindowPda8P7ShellAccentLoaded));
        return true;
    }

    private static void MainWindowPda8P7ShellAccentLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window)
            return;

        window.Dispatcher.BeginInvoke(
            new Action(window.ApplyPda8P7ShellAccent),
            DispatcherPriority.ContextIdle);
    }

    private void ApplyPda8P7ShellAccent()
    {
        if (_pda8P7ShellAccentApplied)
            return;

        if (Content is not DependencyObject root)
            return;

        Brush accent = FindBrush("AppBrandAccentBrush", Brushes.Goldenrod);
        Brush normal = FindBrush("AppTextBrush", AppText);
        Brush hoverSurface = FindBrush("AppSurfaceRaisedBrush", AppSurfaceRaised);

        int themed = 0;
        foreach (Button button in FindDescendants<Button>(root))
        {
            if (button.Content is not string label || !IsTopMenuLabel(label))
                continue;

            themed++;
            button.Background = Brushes.Transparent;
            button.BorderBrush = Brushes.Transparent;
            button.BorderThickness = new Thickness(0, 0, 0, 1);
            button.Foreground = normal;
            button.Padding = new Thickness(10, 6, 10, 6);

            button.MouseEnter += (_, _) =>
            {
                button.Foreground = accent;
                button.Background = hoverSurface;
                button.BorderBrush = accent;
            };

            button.MouseLeave += (_, _) =>
            {
                if (!button.IsKeyboardFocusWithin)
                {
                    button.Foreground = normal;
                    button.Background = Brushes.Transparent;
                    button.BorderBrush = Brushes.Transparent;
                }
            };

            button.GotKeyboardFocus += (_, _) =>
            {
                button.Foreground = accent;
                button.Background = hoverSurface;
                button.BorderBrush = accent;
            };

            button.LostKeyboardFocus += (_, _) =>
            {
                if (!button.IsMouseOver)
                {
                    button.Foreground = normal;
                    button.Background = Brushes.Transparent;
                    button.BorderBrush = Brushes.Transparent;
                }
            };
        }

        // The Home/Profile rail already owns the accepted sparse warm active state.
        // Re-queue it here only so restored/late shell layout always ends in the
        // same active-state hierarchy after the top-menu accent pass.
        QueuePda8NavigationRailStateRefresh();

        _pda8P7ShellAccentApplied = themed > 0;
    }

    private static bool IsTopMenuLabel(string label) =>
        string.Equals(label, "Project / Profile", StringComparison.Ordinal) ||
        string.Equals(label, "View", StringComparison.Ordinal) ||
        string.Equals(label, "Help", StringComparison.Ordinal);

    private static IEnumerable<T> FindDescendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        int count;
        try
        {
            count = VisualTreeHelper.GetChildrenCount(root);
        }
        catch (InvalidOperationException)
        {
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
                yield return match;

            foreach (T descendant in FindDescendants<T>(child))
                yield return descendant;
        }
    }
}
