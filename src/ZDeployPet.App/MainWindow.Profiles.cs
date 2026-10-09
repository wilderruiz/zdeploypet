using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ZDeployPet.Core;
using ZDeployPet.Infrastructure;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _refreshingSavedProfiles;
    private bool _managedGridRowsConfigured;

    private async void ProfileNameTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        ConfigureManagedGridRows();
        await RefreshSavedProfileChoicesAsync();

        // Window_Loaded may still be restoring a draft/profile. Run once that load cycle is idle
        // so both grids end with one real, persistable blank row rather than WPF's transient new-item row.
        Dispatcher.BeginInvoke(EnsureManagedGridRows, DispatcherPriority.ContextIdle);
    }

    private async void ProfileNameTextBox_DropDownOpened(object? sender, EventArgs e) =>
        await RefreshSavedProfileChoicesAsync();

    private void ConfigureManagedGridRows()
    {
        if (_managedGridRowsConfigured) return;
        _managedGridRowsConfigured = true;

        // Never let the operator type into DataGrid's NewItemPlaceholder. That temporary item is visible
        // but is not guaranteed to exist in _targetDrafts/_destinationDrafts when Save is clicked.
        TargetsGrid.CanUserAddRows = false;
        DestinationsGrid.CanUserAddRows = false;

        TargetsGrid.RowEditEnding += (_, _) =>
            Dispatcher.BeginInvoke(EnsureManagedGridRows, DispatcherPriority.Background);
        DestinationsGrid.RowEditEnding += (_, _) =>
            Dispatcher.BeginInvoke(EnsureManagedGridRows, DispatcherPriority.Background);
    }

    private void EnsureManagedGridRows()
    {
        if (_targetDrafts.Count == 0 || !_targetDrafts[^1].IsBlank)
            _targetDrafts.Add(new TargetDraft { Port = 22 });

        if (_destinationDrafts.Count == 0 || !_destinationDrafts[^1].IsBlank)
            _destinationDrafts.Add(new DestinationDraft());
    }

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
                EnsureManagedGridRows();
                ShowSetup($"Saved profile '{snapshot.Draft.ProfileName}' loaded from local JSON. Incomplete and complete settings are restored exactly as last saved.");
                return;
            }

            // Backward compatibility for profiles created before setup snapshots existed.
            DeploymentProfile? saved = await _profileStore.LoadByNameAsync(profileName);
            if (saved is null)
            {
                StatusText.Text = $"No saved JSON profile named '{profileName}' was found.";
                return;
            }

            _activeProfile = saved;
            await _profileStore.SetActiveProfileAsync(saved.Id);
            PopulateEditor(saved);
            EnsureManagedGridRows();
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
