using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ZDeployPet.App;

public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Local\Zomniverse.ZDeployPet.SingleInstance";
    private const string SingleInstanceActivationEventName = @"Local\Zomniverse.ZDeployPet.Activate";

    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _singleInstanceActivationEvent;
    private CancellationTokenSource? _singleInstanceListenerCancellation;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool isPrimaryInstance);
        if (!isPrimaryInstance)
        {
            SignalPrimaryInstance();
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            Shutdown(0);
            return;
        }

        _singleInstanceActivationEvent = new EventWaitHandle(
            initialState: false,
            EventResetMode.AutoReset,
            SingleInstanceActivationEventName);
        _singleInstanceListenerCancellation = new CancellationTokenSource();
        StartSingleInstanceActivationListener();

        EventManager.RegisterClassHandler(
            typeof(Window),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) =>
            {
                if (sender is not Window window) return;
                window.Icon = ProductIconFactory.CreateWindowIcon();
                ThemeRuntime.Apply(window);
                AccessibilityRuntime.Apply(window);
            }));

        EventManager.RegisterClassHandler(
            typeof(ToolTip),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) =>
            {
                if (sender is ToolTip toolTip) ThemeRuntime.ApplyToolTip(toolTip);
            }));

        // The custom dark ComboBox template deliberately owns its own chrome. Keep the
        // entire non-editable field clickable, not only the narrow arrow button. The
        // arrow itself is excluded here so its normal ToggleButton behavior is not
        // double-toggled by this class handler.
        EventManager.RegisterClassHandler(
            typeof(ComboBox),
            UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler((sender, args) =>
            {
                if (sender is not ComboBox comboBox ||
                    !comboBox.IsEnabled ||
                    comboBox.IsEditable ||
                    comboBox.IsDropDownOpen)
                    return;

                if (args.OriginalSource is DependencyObject source &&
                    IsInsideToggleButton(source, comboBox))
                    return;

                comboBox.Focus();
                comboBox.IsDropDownOpen = true;
                args.Handled = true;
            }));

        ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Application", "ZDeployPet started.");
        base.OnStartup(e);
    }

    private static bool IsInsideToggleButton(DependencyObject source, ComboBox owner)
    {
        DependencyObject? current = source;
        while (current is not null && !ReferenceEquals(current, owner))
        {
            if (current is ToggleButton) return true;
            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private static void SignalPrimaryInstance()
    {
        try
        {
            using EventWaitHandle activationEvent = EventWaitHandle.OpenExisting(SingleInstanceActivationEventName);
            activationEvent.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // The first instance may still be in its earliest startup window. The mutex remains
            // authoritative, so the second process exits rather than creating another shell.
        }
    }

    private void StartSingleInstanceActivationListener()
    {
        EventWaitHandle? activationEvent = _singleInstanceActivationEvent;
        CancellationTokenSource? cancellation = _singleInstanceListenerCancellation;
        if (activationEvent is null || cancellation is null) return;

        _ = Task.Run(() =>
        {
            WaitHandle[] handles = [activationEvent, cancellation.Token.WaitHandle];
            while (WaitHandle.WaitAny(handles) == 0)
            {
                Dispatcher.BeginInvoke(() => ActivatePrimaryWindow());
            }
        });
    }

    private void ActivatePrimaryWindow()
    {
        Window? window = MainWindow;
        if (window is null)
        {
            foreach (Window candidate in Windows)
            {
                if (!candidate.IsVisible) continue;
                window = candidate;
                break;
            }
        }

        if (window is null) return;

        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;

        if (!window.IsVisible)
            window.Show();

        window.Activate();
        window.Topmost = true;
        window.Topmost = false;
        window.Focus();

        ShellRuntime.Activity.Add(
            ShellActivityLevel.Info,
            "Application",
            "Existing ZDeployPet window activated from a second launch request.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Application", "ZDeployPet exited.");

        _singleInstanceListenerCancellation?.Cancel();
        _singleInstanceActivationEvent?.Set();
        _singleInstanceActivationEvent?.Dispose();
        _singleInstanceActivationEvent = null;
        _singleInstanceListenerCancellation?.Dispose();
        _singleInstanceListenerCancellation = null;

        if (_singleInstanceMutex is not null)
        {
            try
            {
                _singleInstanceMutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Shutdown must not fail if ownership was already relinquished during teardown.
            }

            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }

        base.OnExit(e);
    }
}
