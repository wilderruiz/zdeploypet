using System.Windows;
using System.Windows.Controls;
using ZDeployPet.Core;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _loadingSavedProfileSelection;

    private async void ProfileNameTextBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingSavedProfileSelection || !IsLoaded || SetupPanel.Visibility != Visibility.Visible)
            return;

        string? selectedName = ProfileNameTextBox.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(selectedName)) return;

        try
        {
            _loadingSavedProfileSelection = true;
            DeploymentProfile? saved = await _profileStore.LoadByNameAsync(selectedName);
            if (saved is null)
            {
                StatusText.Text = $"'{selectedName}' is a remembered suggestion, not a saved profile.";
                return;
            }

            _activeProfile = saved;
            await _profileStore.SetActiveProfileAsync(saved.Id);
            PopulateEditor(saved);
            ShowSetup($"Saved profile '{saved.Name}' loaded. Review or update it, then save when ready.");
        }
        catch (Exception exception)
        {
            StatusText.Text = "Saved profile could not be loaded.";
            SetupValidationText.Text = exception.Message;
        }
        finally
        {
            _loadingSavedProfileSelection = false;
        }
    }
}
