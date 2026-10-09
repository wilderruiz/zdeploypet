using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using ZDeployPet.Core;

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
            IReadOnlyList<DeploymentProfile> profiles = await _profileStore.LoadAllAsync();
            ObservableCollection<string> names = (ObservableCollection<string>)ProfileNameTextBox.ItemsSource;
            names.Clear();
            foreach (string name in profiles
                         .Select(profile => profile.Name)
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
            DeploymentProfile? saved = await _profileStore.LoadByNameAsync(profileName);
            if (saved is null)
            {
                StatusText.Text = $"No saved JSON profile named '{profileName}' was found.";
                return;
            }

            _activeProfile = saved;
            await _profileStore.SetActiveProfileAsync(saved.Id);
            PopulateEditor(saved);
            ShowSetup($"Saved profile '{saved.Name}' loaded from local JSON. Review or update it, then save when ready.");
        }
        catch (Exception exception)
        {
            StatusText.Text = "Saved profile could not be loaded.";
            SetupValidationText.Text = exception.Message;
        }
        finally
        {
            LoadProfileButton.IsEnabled = true;
        }
    }
}
