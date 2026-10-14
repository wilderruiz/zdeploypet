using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private static readonly bool Pda8P8ActivityDensityRegistered = RegisterPda8P8ActivityDensity();
    private bool _pda8P8ActivityDensityApplied;
    private bool _pda8P8ActivityDensityRetryQueued;

    private static bool RegisterPda8P8ActivityDensity()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(MainWindowPda8P8ActivityDensityLoaded));
        return true;
    }

    private static void MainWindowPda8P8ActivityDensityLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window)
            return;

        window.Dispatcher.BeginInvoke(
            new Action(window.ApplyPda8P8ActivityDensity),
            DispatcherPriority.ContextIdle);
    }

    private void ApplyPda8P8ActivityDensity()
    {
        if (_pda8P8ActivityDensityApplied)
            return;

        if (_activityPane?.Child is not Grid root ||
            _activityConsoleTabs is null ||
            _activityConsoleTextBox is null)
        {
            QueuePda8P8ActivityDensityRetry();
            return;
        }

        // P8 is intentionally a density-only pass. The Activity pane remains the
        // same read-only companion surface; only vertical chrome and spacing are
        // tightened so it reads more like a VS Code inspector/terminal.
        root.Margin = new Thickness(12, 10, 12, 10);

        DockPanel? header = root.Children
            .OfType<DockPanel>()
            .FirstOrDefault(child => Grid.GetRow(child) == 0);
        if (header is not null)
        {
            StackPanel? heading = header.Children
                .OfType<StackPanel>()
                .FirstOrDefault();
            if (heading is not null)
            {
                TextBlock[] headingText = heading.Children.OfType<TextBlock>().ToArray();
                if (headingText.Length > 0)
                    headingText[0].FontSize = 18;
                if (headingText.Length > 1)
                    headingText[1].Margin = new Thickness(0, 2, 0, 0);
            }
        }

        Border? stateStrip = root.Children
            .OfType<Border>()
            .FirstOrDefault(child => Grid.GetRow(child) == 1);
        if (stateStrip is not null)
        {
            stateStrip.Margin = new Thickness(0, 8, 0, 7);
            stateStrip.Padding = new Thickness(9, 5, 9, 5);
        }

        _activityConsoleTabs.Margin = new Thickness(0, 0, 0, 5);
        foreach (TabItem tab in _activityConsoleTabs.Items.OfType<TabItem>())
        {
            tab.Padding = new Thickness(9, 5, 9, 5);
            tab.Margin = new Thickness(0, 0, 2, 0);
        }

        _activityConsoleTextBox.Padding = new Thickness(10, 8, 10, 8);

        Grid? footer = root.Children
            .OfType<Grid>()
            .FirstOrDefault(child => Grid.GetRow(child) == 4);
        if (footer is not null)
        {
            footer.Margin = new Thickness(0, 8, 0, 0);

            TextBlock? status = footer.Children
                .OfType<TextBlock>()
                .FirstOrDefault();
            if (status is not null)
                status.Margin = new Thickness(0, 0, 0, 4);

            WrapPanel? actions = footer.Children
                .OfType<WrapPanel>()
                .FirstOrDefault();
            if (actions is not null)
            {
                Button[] buttons = actions.Children.OfType<Button>().ToArray();
                foreach (Button button in buttons)
                    button.Padding = new Thickness(9, 5, 9, 5);

                if (buttons.Length > 0)
                {
                    buttons[0].MinWidth = 68;
                    buttons[0].Margin = new Thickness(0, 1, 7, 1);
                }

                if (buttons.Length > 1)
                {
                    buttons[1].MinWidth = 92;
                    buttons[1].Margin = new Thickness(0, 1, 0, 1);
                }
            }
        }

        _pda8P8ActivityDensityApplied = true;
    }

    private void QueuePda8P8ActivityDensityRetry()
    {
        if (_pda8P8ActivityDensityRetryQueued)
            return;

        _pda8P8ActivityDensityRetryQueued = true;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _pda8P8ActivityDensityRetryQueued = false;
            ApplyPda8P8ActivityDensity();
        }), DispatcherPriority.Background);
    }
}
