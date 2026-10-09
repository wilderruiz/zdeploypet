using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using ZDeployPet.Core;
using ZDeployPet.Infrastructure;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _refreshingSavedProfiles;

    private async void ProfileNameTextBox_Loaded(object sender, RoutedEventArgs e) =>
        await RefreshSavedProfileChoicesAsync();

    private async void ProfileNameTextBox_DropDownOpened(object? sender, EventArgs e) =>
        await RefreshSavedProfileChoicesAsync();

    private async Task RefreshSavedProfileChoicesAsync()
    {
        if (_refreshingSavedProfiles) return;

        try
        {
            _refreshingSavedProfiles = true;
            string currentText = ProfileNameTextBox.Text;
            IReadOnlyList<string> setupNames = await _profileStore.LoadSetupProfileNamesAsync();
            IReadOnlyList<DeploymentProfile> completeProfiles = await _profileStore.LoadAllAsync();
            ObservableCollection<string> names = (ObservableCollection<string>)ProfileNameTextBox.ItemsSource;
            names.Clear();
            foreach (string name in setupNames
                         .Concat(completeProfiles.Select(profile => profile.Name))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(name);
            }
            ProfileNameTextBox.Text = currentText;
        }
        catch (Exception exception)
        {
            StatusText.Text = "Saved profile list could not be refreshed.";
            SetupValidationText.Text = exception.Message;
            ShellRuntime.Activity.Add(ShellActivityLevel.Warning, "Profile", "Saved profile list refresh failed: " + exception.Message);
        }
        finally
        {
            _refreshingSavedProfiles = false;
        }
    }

    private async void LoadProfile_Click(object sender, RoutedEventArgs e)
    {
        string profileName = ProfileNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(profileName))
        {
            StatusText.Text = "Choose a saved profile first.";
            return;
        }

        LoadProfileButton.IsEnabled = false;
        SetupValidationText.Text = string.Empty;
        try
        {
            SetupProfileSnapshot? snapshot = await _profileStore.LoadSetupSnapshotByNameAsync(profileName);
            if (snapshot is not null)
            {
                _activeProfile = await _profileStore.LoadByNameAsync(profileName);
                PopulateDraft(snapshot.Draft);
                WslPathTextBox.Text = snapshot.VerifiedWslPath;
                EnsureTrailingEntryRows();
                ShellRuntime.State.UpdateProfile(snapshot.Draft.ProfileName);
                ShellRuntime.Activity.Add(
                    ShellActivityLevel.Info,
                    "Profile",
                    $"Loaded local profile snapshot '{snapshot.Draft.ProfileName}'.");
                ShowSetup($"Saved profile '{snapshot.Draft.ProfileName}' loaded from local JSON. Incomplete and complete settings are restored exactly as last saved.");
                return;
            }

            // Backward compatibility for profiles created before setup snapshots existed.
            DeploymentProfile? saved = await _profileStore.LoadByNameAsync(profileName);
            if (saved is null)
            {
                StatusText.Text = $"No saved JSON profile named '{profileName}' was found.";
                ShellRuntime.Activity.Add(ShellActivityLevel.Warning, "Profile", $"Saved profile '{profileName}' was not found.");
                return;
            }

            _activeProfile = saved;
            await _profileStore.SetActiveProfileAsync(saved.Id);
            PopulateEditor(saved);
            EnsureTrailingEntryRows();
            ShellRuntime.State.UpdateProfile(saved.Name);
            ShellRuntime.Activity.Add(ShellActivityLevel.Success, "Profile", $"Loaded validated profile '{saved.Name}'.");
            ShowSetup($"Saved profile '{saved.Name}' loaded from local JSON. Review or update it, then save when ready.");
        }
        catch (Exception exception)
        {
            StatusText.Text = "Saved profile could not be loaded.";
            SetupValidationText.Text = exception.Message;
            ShellRuntime.Activity.Add(ShellActivityLevel.Error, "Profile", "Saved profile load failed: " + exception.Message);
        }
        finally
        {
            LoadProfileButton.IsEnabled = true;
        }
    }
}
