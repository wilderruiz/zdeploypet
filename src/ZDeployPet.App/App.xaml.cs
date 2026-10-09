using System.Windows;

namespace ZDeployPet.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Application", "ZDeployPet started.");
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Application", "ZDeployPet exited.");
        base.OnExit(e);
    }
}
