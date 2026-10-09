using System.Windows;
using System.Windows.Controls;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda3ShellHooked;
    private Button? _activityButton;

    private void InitializePda3Shell()
    {
        if (!_pda3ShellHooked)
        {
            _pda3ShellHooked = true;
            DiscoveryPanel.IsVisibleChanged += (_, _) => RefreshPda3ActionVisibility();
        }

        EnsureActivityButton();
        RefreshPda3ActionVisibility();
    }

    private void EnsureActivityButton()
    {
        if (_activityButton is not null || AccessOnboardingButton.Parent is not StackPanel actions) return;

        _activityButton = new Button
        {
            Content = "Activity…",
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(16, 9, 16, 9),
            Visibility = Visibility.Collapsed
        };
        _activityButton.Click += Activity_Click;
        actions.Children.Add(_activityButton);

        HelpTipFactory.AttachToButton(
            _activityButton,
            new HelpTipSpec(
                "Open the sanitized ZDeployPet Console / Activity view.",
                "Activity shows application, profile, deployment-session and target-probe events from the bounded in-memory shell log.",
                WhenToUse: "Use it when you want to see what ZDeployPet has done during this app session or copy diagnostics for troubleshooting.",
                WhatItDoes: "The view updates live, shows the current profile/session state, and supports copying selected rows or the complete visible history.",
                Safety: "Entries are sanitized before display. Private-key markers and common password, passphrase, token and secret assignments are redacted, and deployment authority is never exposed through the log."));
    }

    private void RefreshPda3ActionVisibility()
    {
        if (_activityButton is null) return;
        _activityButton.Visibility = DiscoveryPanel.Visibility == Visibility.Visible
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void Activity_Click(object sender, RoutedEventArgs e)
    {
        ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Shell", "Console / Activity opened.");
        ActivityWindow window = new()
        {
            Owner = this
        };
        window.Show();
    }
}
