using System.Windows;
using System.Windows.Controls;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _profileDiscoveryActivityRoutingInitialized;

    private void InitializeProfileDiscoveryActivityRouting()
    {
        if (_profileDiscoveryActivityRoutingInitialized || _profileDiscoveryButton is null)
            return;

        _profileDiscoveryActivityRoutingInitialized = true;
        _profileDiscoveryButton.Click += ProfileDiscoveryActivity_Click;
    }

    private async void ProfileDiscoveryActivity_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null)
            return;

        // Discovery is profile/application diagnostics, not a deployment report.
        // Focus the bounded ZDeployPet activity channel while the existing discovery
        // engine performs the authoritative checks in the background.
        if (_activityPane is not null && _activityPane.Visibility != Visibility.Visible)
            ToggleEmbeddedActivityPane();
        if (_activityConsoleTabs is not null && _activityConsoleTabs.Items.Count >= 3)
            _activityConsoleTabs.SelectedIndex = 2;

        ShellRuntime.Activity.Add(
            ShellActivityLevel.Info,
            "Discovery",
            $"Discovery started for '{_activeProfile.Name}'. Checking the saved Windows project, WSL environment, deployment runner and report location.");

        // The profile toolbar invokes the existing DiscoverButton_Click handler first.
        // Wait until that established engine finishes, then mirror its hidden result
        // into the user-facing ZDeployPet activity stream rather than maintaining a
        // second discovery implementation.
        for (int attempt = 0; attempt < 240 && !DiscoverButton.IsEnabled; attempt++)
            await Task.Delay(100);

        string result = ResultsTextBox.Text ?? string.Empty;
        foreach (string rawLine in result.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.Equals("WINDOWS", StringComparison.OrdinalIgnoreCase) || line.Equals("WSL", StringComparison.OrdinalIgnoreCase))
                continue;

            ShellActivityLevel level = line.Contains("[MISSING]", StringComparison.OrdinalIgnoreCase) ||
                                       line.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)
                ? ShellActivityLevel.Warning
                : line.Contains("[OK]", StringComparison.OrdinalIgnoreCase)
                    ? ShellActivityLevel.Success
                    : ShellActivityLevel.Info;

            string prose = line
                .Replace("[OK]", "available —", StringComparison.OrdinalIgnoreCase)
                .Replace("[MISSING]", "missing —", StringComparison.OrdinalIgnoreCase);

            ShellRuntime.Activity.Add(level, "Discovery", prose);
        }

        bool passed = string.Equals(StatusText.Text, "Discovery passed", StringComparison.OrdinalIgnoreCase);
        ShellRuntime.Activity.Add(
            passed ? ShellActivityLevel.Success : ShellActivityLevel.Warning,
            "Discovery",
            passed
                ? "Discovery passed. The saved profile environment is ready for the checked local and WSL capabilities."
                : "Discovery finished with one or more missing capabilities. Review the discovery messages above or update the profile.");
    }
}
