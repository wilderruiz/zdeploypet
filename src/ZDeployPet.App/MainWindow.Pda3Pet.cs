using System.Windows;
using System.Windows.Controls;
using ZDeployPet.Core;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private PetCompanionControl? _petCompanion;

    private void InitializePetCompanion()
    {
        if (_petCompanion is not null) return;
        if (DiscoveryPanel.Parent is not Grid shellGrid) return;

        _petCompanion = new PetCompanionControl
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0),
            Visibility = Visibility.Collapsed
        };

        Grid.SetRow(_petCompanion, 0);
        shellGrid.Children.Add(_petCompanion);

        DiscoveryPanel.IsVisibleChanged += (_, _) => RefreshPetCompanion();
        RefreshPetCompanion();
    }

    private void RefreshPetCompanion()
    {
        if (_petCompanion is null) return;

        _petCompanion.Visibility = DiscoveryPanel.Visibility == Visibility.Visible && _activeProfile is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
        if (_petCompanion.Visibility != Visibility.Visible) return;

        if (_dryRunExecutionBusy)
        {
            _petCompanion.SetState(PetCompanionState.Running, "Dry run in progress.");
            return;
        }

        if (_operatorAccessBusy)
        {
            _petCompanion.SetState(PetCompanionState.Running, "Checking deployment access.");
            return;
        }

        if (_deploymentSession.State is DeploymentAccessSessionState.Invalid or DeploymentAccessSessionState.Expired)
        {
            _petCompanion.SetState(PetCompanionState.Error, "Deployment access needs attention.");
            return;
        }

        if ((_deploymentSession.State is DeploymentAccessSessionState.Ready or DeploymentAccessSessionState.Expiring) &&
            _operatorAccessTargetsPassed)
        {
            _petCompanion.SetState(PetCompanionState.Ready, "Approved targets are ready.");
            return;
        }

        _petCompanion.SetState(PetCompanionState.Locked, "Deployment access is off.");
    }
}
