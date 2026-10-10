using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZDeployPet.Core;
using ZDeployPet.Infrastructure;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private readonly DeploymentAccessIdentityStore _operatorAccessIdentityStore = new();
    private Border? _operatorAccessCard;
    private TextBlock? _operatorAccessStateText;
    private TextBlock? _operatorAccessMessageText;
    private Button? _operatorAccessButton;
    private bool _operatorAccessBusy;
    private bool _operatorAccessTargetsPassed;

    private void InitializeOperatorAccessControl()
    {
        if (_operatorAccessCard is not null) return;

        if (DiscoveryPanel.RowDefinitions.Count < 2) return;
        DiscoveryPanel.RowDefinitions.Insert(1, new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(ResultsTextBox, 2);

        Grid cardGrid = new();
        cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        StackPanel copy = new();
        copy.Children.Add(new TextBlock
        {
            Text = "Deployment access",
            FontWeight = FontWeights.SemiBold,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        });

        _operatorAccessStateText = new TextBlock
        {
            Text = "OFF",
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 3, 0, 0),
            Foreground = FindBrush("AppTextBrush", Brushes.White)
        };
        _operatorAccessMessageText = new TextBlock
        {
            Text = "Deployment access is locked.",
            Margin = new Thickness(0, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        };
        copy.Children.Add(_operatorAccessStateText);
        copy.Children.Add(_operatorAccessMessageText);
        cardGrid.Children.Add(copy);

        _operatorAccessButton = new Button
        {
            Content = "Turn on",
            MinWidth = 150,
            Padding = new Thickness(18, 10, 18, 10),
            Margin = new Thickness(18, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        _operatorAccessButton.Click += OperatorAccessButton_Click;
        Grid.SetColumn(_operatorAccessButton, 1);
        cardGrid.Children.Add(_operatorAccessButton);

        _operatorAccessCard = new Border
        {
            Margin = new Thickness(0, 0, 0, 12),
            Padding = new Thickness(14),
            Background = FindBrush("AppSurfaceRaisedBrush", Brushes.DarkSlateGray),
            BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray),
            BorderThickness = new Thickness(1),
            Child = cardGrid
        };
        Grid.SetRow(_operatorAccessCard, 1);
        DiscoveryPanel.Children.Add(_operatorAccessCard);

        Button accessHelp = HelpTipFactory.Create(new HelpTipSpec(
            "Control everyday deployment access from one place.",
            "Turn on starts the existing bounded ZDeployPet ssh-agent flow. After OpenSSH accepts the approved key, ZDeployPet verifies the exact fingerprint and automatically probes every configured target before showing ON / READY.",
            WhenToUse: "Use this instead of opening the detailed Deployment access and Session windows during normal daily deployment work.",
            WhatItDoes: "If onboarding is incomplete, this control changes to Set up deployment access. When access is ON, the same button becomes Turn off and invokes the real bounded-session lock path.",
            Safety: "ON is never cosmetic. ZDeployPet shows ON / READY only after the approved key, lease, and all configured non-writing target probes pass. Closing the app still locks deployment access."));
        _operatorAccessButton.ToolTip = accessHelp.ToolTip;

        DiscoveryPanel.IsVisibleChanged += async (_, _) =>
        {
            if (DiscoveryPanel.Visibility == Visibility.Visible)
                await RefreshOperatorAccessControlAsync();
        };

        _ = RefreshOperatorAccessControlAsync();
    }

    private async void OperatorAccessButton_Click(object sender, RoutedEventArgs e)
    {
        if (_operatorAccessBusy || _activeProfile is null) return;

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
                "Turn on requested. Waiting for the trusted OpenSSH key prompt to complete.");

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
                    "The trusted key prompt did not complete within the automatic verification window. Use Turn on again after ssh-add finishes.");
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
        if (_operatorAccessCard is null || _operatorAccessButton is null) return;
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
        if (_operatorAccessCard is null || _operatorAccessStateText is null ||
            _operatorAccessMessageText is null || _operatorAccessButton is null)
            return;

        _operatorAccessCard.Visibility = DiscoveryPanel.Visibility == Visibility.Visible && _activeProfile is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
        if (_operatorAccessCard.Visibility != Visibility.Visible) return;

        _operatorAccessButton.IsEnabled = !_operatorAccessBusy;

        if (_operatorAccessBusy)
        {
            _operatorAccessStateText.Text = "TURNING ON…";
            _operatorAccessMessageText.Text = "Complete the trusted OpenSSH prompt if it is waiting for the approved key passphrase. ZDeployPet is verifying the bounded session automatically.";
            _operatorAccessButton.Content = "Working…";
            return;
        }

        bool sessionReady = _deploymentSession.State is DeploymentAccessSessionState.Ready or DeploymentAccessSessionState.Expiring;
        if (sessionReady && _operatorAccessTargetsPassed)
        {
            _operatorAccessStateText.Text = "ON / READY";
            _operatorAccessMessageText.Text = _deploymentSession.Message;
            _operatorAccessButton.Content = "Turn off";
            return;
        }

        if (!onboardingReady)
        {
            _operatorAccessStateText.Text = "ATTENTION";
            _operatorAccessMessageText.Text = "Deployment identity or server trust is not fully configured for this profile.";
            _operatorAccessButton.Content = "Set up deployment access";
            return;
        }

        if (_deploymentSession.State is DeploymentAccessSessionState.Invalid or DeploymentAccessSessionState.Expired)
        {
            _operatorAccessStateText.Text = "ATTENTION";
            _operatorAccessMessageText.Text = _deploymentSession.Message;
            _operatorAccessButton.Content = "Turn on";
            return;
        }

        _operatorAccessStateText.Text = "OFF";
        _operatorAccessMessageText.Text = _deploymentSession.HasOwnedAgent
            ? "The bounded agent is waiting for the approved key. Use Turn on to finish verification."
            : "Deployment access is configured but locked.";
        _operatorAccessButton.Content = "Turn on";
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
