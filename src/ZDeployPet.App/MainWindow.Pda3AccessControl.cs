using System.Windows;
using System.Windows.Controls;
using ZDeployPet.Core;
using ZDeployPet.Infrastructure;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private readonly DeploymentAccessIdentityStore _operatorAccessIdentityStore = new();
    private Border? _operatorAccessCard;
    private bool _operatorAccessBusy;
    private bool _operatorAccessTargetsPassed;

    private void InitializeOperatorAccessControl()
    {
        if (_operatorAccessCard is not null) return;

        if (DiscoveryPanel.RowDefinitions.Count < 2) return;
        DiscoveryPanel.RowDefinitions.Insert(1, new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(ResultsTextBox, 2);

        // This card is now only the action host for Dry run / Live deploy.
        // ZDeploy Pet itself owns access state, colour, ON/OFF interaction and guidance.
        Grid cardGrid = new();
        cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _operatorAccessCard = new Border
        {
            Margin = new Thickness(0, 0, 0, 12),
            Padding = new Thickness(14),
            Background = FindBrush("AppSurfaceRaisedBrush", System.Windows.Media.Brushes.DarkSlateGray),
            BorderBrush = FindBrush("AppBorderBrush", System.Windows.Media.Brushes.DimGray),
            BorderThickness = new Thickness(1),
            Child = cardGrid
        };
        Grid.SetRow(_operatorAccessCard, 1);
        DiscoveryPanel.Children.Add(_operatorAccessCard);

        DiscoveryPanel.IsVisibleChanged += async (_, _) =>
        {
            if (DiscoveryPanel.Visibility == Visibility.Visible)
                await RefreshOperatorAccessControlAsync();
        };

        _ = RefreshOperatorAccessControlAsync();
    }

    private async Task ToggleOperatorAccessAsync()
    {
        if (_operatorAccessBusy || _activeProfile is null) return;

        // Access/session activity belongs in the ZDeployPet channel. Keep the
        // trusted terminal reserved only for secret entry; all observable status
        // and probe progress stays in-app.
        if (_activityPane is not null && _activityPane.Visibility != Visibility.Visible)
            ToggleEmbeddedActivityPane();
        if (_activityConsoleTabs is not null && _activityConsoleTabs.Items.Count >= 3)
            _activityConsoleTabs.SelectedIndex = 2;

        if (_deploymentSession.State is DeploymentAccessSessionState.Ready or DeploymentAccessSessionState.Expiring &&
            _operatorAccessTargetsPassed)
        {
            await RunOperatorAccessBusyAsync(async () =>
            {
                await _deploymentSession.LockAsync();
                _operatorAccessTargetsPassed = false;
                ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Session", _deploymentSession.Message);
                PublishOperatorAccessShellState();
            });
            return;
        }

        if (!await IsOperatorAccessOnboardingReadyAsync(_activeProfile))
        {
            ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Access", $"Deployment access setup opened for '{_activeProfile.Name}'.");
            AccessOnboardingWindow setup = new(_activeProfile) { Owner = this };
            setup.ShowDialog();
            await RefreshOperatorAccessControlAsync();
            return;
        }

        await RunOperatorAccessBusyAsync(async () =>
        {
            _operatorAccessTargetsPassed = false;
            ShellRuntime.Activity.Add(
                ShellActivityLevel.Info,
                "Session",
                "ZDeploy Pet is preparing the bounded deployment-access session. A trusted terminal appears only if OpenSSH needs the private-key passphrase.");

            if (_deploymentSession.HasOwnedAgent)
                await _deploymentSession.CheckAsync(_activeProfile);
            else
                await _deploymentSession.BeginUnlockAsync(_activeProfile);

            PublishOperatorAccessShellState();
            RefreshOperatorAccessVisuals(onboardingReady: true);

            if (!_deploymentSession.HasOwnedAgent)
                return;

            ShellRuntime.Activity.Add(
                ShellActivityLevel.Info,
                "Session",
                "Waiting for the trusted OpenSSH key prompt to complete. ZDeployPet never receives the private-key passphrase.");

            for (int attempt = 0; attempt < 120; attempt++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                await _deploymentSession.CheckAsync(_activeProfile);
                PublishOperatorAccessShellState();

                if (_deploymentSession.State is DeploymentAccessSessionState.Ready or DeploymentAccessSessionState.Expiring)
                    break;

                if (_deploymentSession.State is DeploymentAccessSessionState.Invalid or DeploymentAccessSessionState.Expired)
                    return;
            }

            if (_deploymentSession.State is not DeploymentAccessSessionState.Ready and not DeploymentAccessSessionState.Expiring)
            {
                ShellRuntime.Activity.Add(
                    ShellActivityLevel.Warning,
                    "Session",
                    "The trusted key prompt did not complete within the automatic verification window. Click ZDeploy Pet again after ssh-add finishes.");
                return;
            }

            ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Probe", $"Automatic bounded-agent target verification started for '{_activeProfile.Name}'.");
            IReadOnlyList<DeploymentSessionProbeResult> probes = await _deploymentSession.ProbeReadyTargetsAsync(_activeProfile);
            _operatorAccessTargetsPassed = probes.Count == _activeProfile.Targets.Count && probes.Count > 0 && probes.All(item => item.Result.Success);

            foreach (DeploymentSessionProbeResult probe in probes)
            {
                ShellRuntime.Activity.Add(
                    probe.Result.Success ? ShellActivityLevel.Success : ShellActivityLevel.Warning,
                    "Probe",
                    $"{probe.Target.Label}: {probe.Result.Status} — {probe.Result.Message}");
            }

            ShellRuntime.Activity.Add(
                _operatorAccessTargetsPassed ? ShellActivityLevel.Success : ShellActivityLevel.Warning,
                "Session",
                _operatorAccessTargetsPassed
                    ? "Deployment access is ON / READY. All configured target probes passed."
                    : "Deployment access requires attention because one or more target probes did not pass.");
            PublishOperatorAccessShellState();
        });
    }

    private async Task RunOperatorAccessBusyAsync(Func<Task> action)
    {
        _operatorAccessBusy = true;
        RefreshOperatorAccessVisuals(onboardingReady: true);
        RefreshPda5DryRunVisibility();
        RefreshPetCompanion();
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            ShellRuntime.Activity.Add(ShellActivityLevel.Error, "Session", exception.Message);
            StatusText.Text = "Deployment access operation failed.";
        }
        finally
        {
            _operatorAccessBusy = false;
            await RefreshOperatorAccessControlAsync();
            RefreshPetCompanion();
        }
    }

    private async Task RefreshOperatorAccessControlAsync()
    {
        if (_operatorAccessCard is null) return;
        bool onboardingReady = _activeProfile is not null && await IsOperatorAccessOnboardingReadyAsync(_activeProfile);
        RefreshOperatorAccessVisuals(onboardingReady);
        RefreshPda5DryRunVisibility();
        RefreshPetCompanion();
    }

    private async Task<bool> IsOperatorAccessOnboardingReadyAsync(DeploymentProfile profile)
    {
        DeploymentAccessIdentity identity = await _operatorAccessIdentityStore.LoadAsync(profile.Id);
        if (identity.DeploymentKey is null || profile.Targets.Count == 0) return false;
        return profile.Targets.All(target => identity.FindHostKey(target.Id) is not null);
    }

    private void RefreshOperatorAccessVisuals(bool onboardingReady)
    {
        _ = onboardingReady;
        if (_operatorAccessCard is null) return;

        _operatorAccessCard.Visibility = DiscoveryPanel.Visibility == Visibility.Visible && _activeProfile is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void PublishOperatorAccessShellState()
    {
        if (_activeProfile is not null)
            ShellRuntime.State.UpdateProfile(_activeProfile.Name);
        ShellRuntime.State.UpdateSession(
            _deploymentSession.State,
            _deploymentSession.Message,
            _deploymentSession.ExpiresAtUtc);
    }
}
