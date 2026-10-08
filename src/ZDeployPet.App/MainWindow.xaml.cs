using System.Text;
using System.Windows;
using ZDeployPet.Core;
using ZDeployPet.Infrastructure;
using ZDeployPet.WslBridge;

namespace ZDeployPet.App;

public partial class MainWindow : Window
{
    private static readonly DiscoveryOptions Options = new(
        @"H:\PROGRAMMING\public_html\millenova", "Ubuntu-24.04",
        "/mnt/h/PROGRAMMING/public_html/millenova", "/home/wilder/.local/state/millenova/deployments");

    private readonly LocalCapabilityDiscovery _localDiscovery = new();
    private readonly WslCapabilityDiscovery _wslDiscovery = new();

    public MainWindow() => InitializeComponent();

    private async void DiscoverButton_Click(object sender, RoutedEventArgs e)
    {
        DiscoverButton.IsEnabled = false;
        StatusText.Text = "Checking…";
        try
        {
            LocalDiscoveryResult local = _localDiscovery.Discover(Options.WindowsRepositoryPath);
            WslDiscoveryResult wsl = await _wslDiscovery.DiscoverAsync(Options);
            ResultsTextBox.Text = FormatResults(local, wsl);
            StatusText.Text = local.DeployScriptExists && wsl.Available && wsl.DeployScriptExists
                ? "Discovery passed" : "Review missing capability";
        }
        finally
        {
            DiscoverButton.IsEnabled = true;
        }
    }

    private static string FormatResults(LocalDiscoveryResult local, WslDiscoveryResult wsl)
    {
        StringBuilder text = new();
        text.AppendLine("WINDOWS");
        text.AppendLine($"  Runtime: {local.RuntimeVersion}");
        text.AppendLine($"  OS: {local.OperatingSystem}");
        text.AppendLine($"  Repository: {Mark(local.RepositoryExists)} {Options.WindowsRepositoryPath}");
        text.AppendLine($"  Deploy script: {Mark(local.DeployScriptExists)}");
        text.AppendLine($"  Script SHA-256: {local.DeployScriptSha256 ?? "not available"}");
        text.AppendLine().AppendLine("WSL");
        text.AppendLine($"  Distribution: {wsl.Distribution}");
        text.AppendLine($"  Available: {Mark(wsl.Available)}");
        text.AppendLine($"  ssh: {wsl.SshPath ?? "not found"}");
        text.AppendLine($"  ssh version: {wsl.SshVersion ?? "not available"}");
        text.AppendLine($"  ssh-agent: {wsl.SshAgentPath ?? "not found"}");
        text.AppendLine($"  ssh-add: {wsl.SshAddPath ?? "not found"}");
        text.AppendLine($"  Repository: {Mark(wsl.RepositoryExists)} {Options.WslRepositoryPath}");
        text.AppendLine($"  Deploy script: {Mark(wsl.DeployScriptExists)}");
        text.AppendLine($"  Report root: {Mark(wsl.ReportRootExists)} {Options.DeploymentReportRoot}");
        if (wsl.Error is not null) text.AppendLine($"  Error: {wsl.Error}");
        return text.ToString();
    }

    private static string Mark(bool value) => value ? "[OK]" : "[MISSING]";
}
