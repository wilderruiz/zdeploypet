using System.Windows;
using System.Windows.Controls;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda3MenuInitialized;

    private void InitializePda3MenuShell()
    {
        if (_pda3MenuInitialized) return;
        _pda3MenuInitialized = true;

        if (Content is not UIElement existingContent) return;

        Content = null;
        DockPanel shell = new();
        Menu menu = BuildTopMenu();
        DockPanel.SetDock(menu, Dock.Top);
        shell.Children.Add(menu);
        shell.Children.Add(existingContent);
        Content = shell;

        ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Shell", "Top application menu initialized.");
    }

    private Menu BuildTopMenu()
    {
        Menu menu = new()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        MenuItem project = new() { Header = "_Project / Profile", Padding = new Thickness(10, 5, 10, 5) };
        project.Items.Add(CreateMenuItem("_Edit profile", (_, _) => EditProfile_Click(this, new RoutedEventArgs())));
        project.Items.Add(new Separator());
        project.Items.Add(CreateMenuItem("Deployment _access…", (_, _) => AccessOnboarding_Click(this, new RoutedEventArgs())));
        project.Items.Add(CreateMenuItem("_Session…", (_, _) => DeploymentSession_Click(this, new RoutedEventArgs())));
        project.Items.Add(CreateMenuItem("_Git safety…", (_, _) => GitSafety_Click(this, new RoutedEventArgs())));
        project.Items.Add(new Separator());
        project.Items.Add(CreateMenuItem("E_xit", (_, _) => Close()));

        MenuItem view = new() { Header = "_View", Padding = new Thickness(10, 5, 10, 5) };
        view.Items.Add(CreateMenuItem("Console / _Activity…", (_, _) => Activity_Click(this, new RoutedEventArgs())));

        MenuItem help = new() { Header = "_Help", Padding = new Thickness(10, 5, 10, 5) };
        help.Items.Add(CreateMenuItem("_About ZDeployPet…", (_, _) => OpenAboutWindow()));

        menu.Items.Add(project);
        menu.Items.Add(view);
        menu.Items.Add(help);
        return menu;
    }

    private static MenuItem CreateMenuItem(string header, RoutedEventHandler handler)
    {
        MenuItem item = new()
        {
            Header = header,
            MinWidth = 230,
            Padding = new Thickness(14, 8, 14, 8)
        };
        item.Click += handler;
        return item;
    }

    private void OpenAboutWindow()
    {
        ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Shell", "About ZDeployPet opened.");
        AboutWindow window = new() { Owner = this };
        window.ShowDialog();
    }
}
