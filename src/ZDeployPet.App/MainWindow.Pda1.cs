using System.Windows;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda1ShellHooked;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (!_pda1ShellHooked)
        {
            _pda1ShellHooked = true;
            DiscoveryPanel.IsVisibleChanged += (_, _) => RefreshPda1ActionVisibility();
        }
        RefreshPda1ActionVisibility();
    }

    private void RefreshPda1ActionVisibility()
    {
        AccessOnboardingButton.Visibility =
            DiscoveryPanel.Visibility == Visibility.Visible && _activeProfile is not null
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    private void AccessOnboarding_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null)
        {
            StatusText.Text = "Save and validate a deployment profile before configuring deployment access.";
            return;
        }

        AccessOnboardingWindow window = new(_activeProfile)
        {
            Owner = this
        };
        window.ShowDialog();
    }
}
