using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ZDeployPet.Core;
using ZDeployPet.Infrastructure;
using ZDeployPet.WslBridge;

namespace ZDeployPet.App;

public partial class MainWindow : Window
{
    private readonly LocalCapabilityDiscovery _localDiscovery = new();
    private readonly WslCapabilityDiscovery _wslDiscovery = new();
    private readonly WslProfileDiscovery _profileDiscovery = new();
    private readonly DeploymentProfileValidator _validator = new();
    private readonly ProfileStore _profileStore = new();
    private readonly ObservableCollection<TargetDraft> _targetDrafts = [];
    private DeploymentProfile? _activeProfile;

    public MainWindow()
    {
        InitializeComponent();
        TargetsGrid.ItemsSource = _targetDrafts;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await RefreshDistributionsAsync();
            _activeProfile = await _profileStore.LoadActiveAsync();
            if (_activeProfile is null)
            {
                BeginNewProfile();
                ShowSetup("No local deployment profile exists. Complete these four non-deploying setup steps.");
            }
            else
            {
                PopulateEditor(_activeProfile);
                ShowDiscovery(_activeProfile, "Saved local profile loaded. Run discovery when ready.");
            }
        }
        catch (Exception exception)
        {
            BeginNewProfile();
            ShowSetup("The saved profile could not be loaded. Review or recreate it locally.");
            SetupValidationText.Text = exception.Message;
        }
    }

    private void BeginNewProfile()
    {
        ProfileNameTextBox.Text = string.Empty;
        ProjectPathTextBox.Text = string.Empty;
        WslPathTextBox.Text = string.Empty;
        ScriptPathTextBox.Text = string.Empty;
        ReportRootTextBox.Text = string.Empty;
        ConfirmationTextBox.Text = "DEPLOY";
        _targetDrafts.Clear();
        _targetDrafts.Add(new TargetDraft { Port = 22 });
    }

    private void PopulateEditor(DeploymentProfile profile)
    {
        ProfileNameTextBox.Text = profile.Name;
        ProjectPathTextBox.Text = profile.WindowsProjectPath;
        DistributionComboBox.SelectedItem = profile.WslDistribution;
        WslPathTextBox.Text = profile.WslProjectPath;
        ScriptPathTextBox.Text = profile.ScriptRelativePath;
        ReportRootTextBox.Text = profile.ReportRoot ?? string.Empty;
        ConfirmationTextBox.Text = profile.LiveConfirmationPhrase;
        _targetDrafts.Clear();
        foreach (DeploymentTarget target in profile.Targets)
        {
            _targetDrafts.Add(new TargetDraft
            {
                Label = target.Label,
                Host = target.Host,
                Port = target.Port,
                User = target.User,
                RemoteDestination = target.RemoteDestination
            });
        }
    }

    private void BrowseProject_Click(object sender, RoutedEventArgs e)
    {
        OpenFolderDialog dialog = new() { Title = "Choose the project ZDeployPet may inspect" };
        if (Directory.Exists(ProjectPathTextBox.Text)) dialog.InitialDirectory = ProjectPathTextBox.Text;
        if (dialog.ShowDialog(this) != true) return;

        ProjectPathTextBox.Text = dialog.FolderName;
        WslPathTextBox.Text = string.Empty;
        if (string.IsNullOrWhiteSpace(ScriptPathTextBox.Text))
        {
            string? candidate = Directory.EnumerateFiles(dialog.FolderName, "deploy*.sh", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .FirstOrDefault();
            if (candidate is not null) ScriptPathTextBox.Text = candidate;
        }
    }

    private async void RefreshDistributions_Click(object sender, RoutedEventArgs e) => await RefreshDistributionsAsync();

    private async Task RefreshDistributionsAsync()
    {
        string? selected = DistributionComboBox.SelectedItem as string;
        IReadOnlyList<string> distributions = await _profileDiscovery.ListDistributionsAsync();
        DistributionComboBox.ItemsSource = distributions;
        DistributionComboBox.SelectedItem = selected is not null && distributions.Contains(selected) ? selected : distributions.FirstOrDefault();
    }

    private async void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        SaveProfileButton.IsEnabled = false;
        SetupValidationText.Text = string.Empty;
        StatusText.Text = "Validating local profile…";
        try
        {
            TargetsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            TargetsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            string distribution = DistributionComboBox.SelectedItem as string ?? string.Empty;
            WslPathResolution resolution = await _profileDiscovery.ResolveAndVerifyProjectPathAsync(
                distribution, ProjectPathTextBox.Text.Trim());
            if (!resolution.Success || resolution.WslPath is null)
            {
                SetupValidationText.Text = resolution.Error ?? "Could not verify the project path in WSL.";
                StatusText.Text = "Profile validation failed";
                return;
            }
            WslPathTextBox.Text = resolution.WslPath;

            DeploymentProfile profile = BuildProfile(distribution, resolution.WslPath);
            ProfileValidationResult validation = _validator.Validate(profile);
            if (!validation.IsValid)
            {
                SetupValidationText.Text = string.Join(Environment.NewLine, validation.Errors.Select(error => "• " + error));
                StatusText.Text = "Profile validation failed";
                return;
            }

            WslScriptValidation scriptValidation = await _profileDiscovery.VerifyScriptContainmentAsync(
                distribution, resolution.WslPath, profile.ScriptRelativePath);
            if (!scriptValidation.Success)
            {
                SetupValidationText.Text = scriptValidation.Error;
                StatusText.Text = "Script containment failed";
                return;
            }

            await _profileStore.SaveActiveAsync(profile);
            _activeProfile = profile;
            ShowDiscovery(profile, $"Profile saved locally under {_profileStore.Root}. No server was contacted.");
        }
        catch (Exception exception)
        {
            SetupValidationText.Text = exception.Message;
            StatusText.Text = "Profile save failed";
        }
        finally
        {
            SaveProfileButton.IsEnabled = true;
        }
    }

    private DeploymentProfile BuildProfile(string distribution, string wslPath)
    {
        List<DeploymentTarget> targets = _targetDrafts
            .Where(target => !target.IsBlank)
            .Select(target => new DeploymentTarget(
                Guid.NewGuid().ToString("D"), target.Label.Trim(), target.Host.Trim(), target.Port,
                target.User.Trim(), target.RemoteDestination.Trim()))
            .ToList();

        return new DeploymentProfile(
            DeploymentProfile.CurrentSchemaVersion,
            _activeProfile?.Id ?? Guid.NewGuid().ToString("D"),
            ProfileNameTextBox.Text.Trim(),
            Path.GetFullPath(ProjectPathTextBox.Text.Trim()),
            distribution,
            wslPath,
            ScriptPathTextBox.Text.Trim().Replace('\\', '/'),
            NullIfWhiteSpace(ReportRootTextBox.Text),
            ConfirmationTextBox.Text.Trim(),
            targets);
    }

    private void EditProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is not null) PopulateEditor(_activeProfile);
        ShowSetup("Edit and revalidate the local profile. This does not contact a server or deploy.");
    }

    private async void DiscoverButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null) return;
        DiscoverButton.IsEnabled = false;
        StatusText.Text = "Checking…";
        try
        {
            DiscoveryOptions options = new(
                _activeProfile.WindowsProjectPath,
                _activeProfile.WslDistribution,
                _activeProfile.WslProjectPath,
                _activeProfile.ReportRoot ?? string.Empty,
                _activeProfile.ScriptRelativePath);
            LocalDiscoveryResult local = _localDiscovery.Discover(options.WindowsRepositoryPath, options.ScriptRelativePath);
            WslDiscoveryResult wsl = await _wslDiscovery.DiscoverAsync(options);
            ResultsTextBox.Text = FormatResults(options, local, wsl);
            StatusText.Text = local.DeployScriptExists && wsl.Available && wsl.DeployScriptExists && wsl.ReportRootExists
                ? "Discovery passed" : "Review missing capability";
        }
        finally
        {
            DiscoverButton.IsEnabled = true;
        }
    }

    private void ShowSetup(string status)
    {
        SubtitleText.Text = "PDA-0A — Create a local, validated deployment profile before authentication setup.";
        SetupPanel.Visibility = Visibility.Visible;
        DiscoveryPanel.Visibility = Visibility.Collapsed;
        SaveProfileButton.Visibility = Visibility.Visible;
        EditProfileButton.Visibility = Visibility.Collapsed;
        DiscoverButton.Visibility = Visibility.Collapsed;
        StatusText.Text = status;
    }

    private void ShowDiscovery(DeploymentProfile profile, string status)
    {
        SubtitleText.Text = "Read-only environment discovery from the active local profile.";
        SetupPanel.Visibility = Visibility.Collapsed;
        DiscoveryPanel.Visibility = Visibility.Visible;
        SaveProfileButton.Visibility = Visibility.Collapsed;
        EditProfileButton.Visibility = Visibility.Visible;
        DiscoverButton.Visibility = Visibility.Visible;
        ProfileSummaryText.Text = $"Profile: {profile.Name}   •   Project: {profile.WindowsProjectPath}   •   WSL: {profile.WslDistribution}   •   Runner: {profile.ScriptRelativePath}   •   Targets: {profile.Targets.Count}";
        StatusText.Text = status;
    }

    private static string FormatResults(DiscoveryOptions options, LocalDiscoveryResult local, WslDiscoveryResult wsl)
    {
        StringBuilder text = new();
        text.AppendLine("WINDOWS");
        text.AppendLine($"  Runtime: {local.RuntimeVersion}");
        text.AppendLine($"  OS: {local.OperatingSystem}");
        text.AppendLine($"  Repository: {Mark(local.RepositoryExists)} {options.WindowsRepositoryPath}");
        text.AppendLine($"  Deploy script: {Mark(local.DeployScriptExists)} {options.ScriptRelativePath}");
        text.AppendLine($"  Script SHA-256: {local.DeployScriptSha256 ?? "not available"}");
        text.AppendLine().AppendLine("WSL");
        text.AppendLine($"  Distribution: {wsl.Distribution}");
        text.AppendLine($"  Available: {Mark(wsl.Available)}");
        text.AppendLine($"  ssh: {wsl.SshPath ?? "not found"}");
        text.AppendLine($"  ssh version: {wsl.SshVersion ?? "not available"}");
        text.AppendLine($"  ssh-agent: {wsl.SshAgentPath ?? "not found"}");
        text.AppendLine($"  ssh-add: {wsl.SshAddPath ?? "not found"}");
        text.AppendLine($"  Repository: {Mark(wsl.RepositoryExists)} {options.WslRepositoryPath}");
        text.AppendLine($"  Deploy script: {Mark(wsl.DeployScriptExists)}");
        text.AppendLine($"  Report root: {Mark(wsl.ReportRootExists)} {options.DeploymentReportRoot}");
        if (wsl.Error is not null) text.AppendLine($"  Error: {wsl.Error}");
        return text.ToString();
    }

    private static string? NullIfWhiteSpace(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Mark(bool value) => value ? "[OK]" : "[MISSING]";

    private sealed class TargetDraft
    {
        public string Label { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 22;
        public string User { get; set; } = string.Empty;
        public string RemoteDestination { get; set; } = string.Empty;
        public bool IsBlank => string.IsNullOrWhiteSpace(Label) && string.IsNullOrWhiteSpace(Host) && string.IsNullOrWhiteSpace(User) && string.IsNullOrWhiteSpace(RemoteDestination);
    }
}
