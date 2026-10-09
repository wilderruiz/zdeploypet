using System.Windows;

namespace ZDeployPet.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        EventManager.RegisterClassHandler(
            typeof(Window),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) =>
            {
                if (sender is Window window) ThemeRuntime.Apply(window);
            }));

        ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Application", "ZDeployPet started.");
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Application", "ZDeployPet exited.");
        base.OnExit(e);
    }
}
