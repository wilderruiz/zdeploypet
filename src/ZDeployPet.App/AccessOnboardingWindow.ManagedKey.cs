using System.Windows;
using ZDeployPet.Core;

namespace ZDeployPet.App;

public partial class AccessOnboardingWindow
{
    private void CreateKeyHelpButton_Loaded(object sender, RoutedEventArgs e)
    {
        HelpTipFactory.AttachToButton(CreateKeyHelpButton, new HelpTipSpec(
            "Create a dedicated ZDeployPet deployment identity for this profile.",
            "ZDeployPet creates a new Ed25519 SSH key pair inside the selected WSL user's ~/.ssh directory. It is separate from unrelated personal, tunnel or application keys.",
            WhenToUse: "Use this when the project does not already have a dedicated ZDeployPet deployment key. Existing keys for other purposes should not be reused just because they are present.",
            WhatItDoes: "The private key stays only in WSL under ~/.ssh with owner-only permissions. The public .pub half can later be installed on the configured Hostinger/VPS account for passwordless SSH deployment authentication.",
            Example: "For the Millenova profile, ZDeployPet creates ~/.ssh/zdeploypet_millenova_ed25519 and ~/.ssh/zdeploypet_millenova_ed25519.pub.",
            Safety: "The key is created outside the Git repository, so it is not committed or pushed. ZDeployPet never copies private-key contents into the profile JSON or to the remote server.",
            Tip: "After creation, review the displayed fingerprint and click Use selected key to approve this identity for the profile."));
    }

    private async void CreateKey_Click(object sender, RoutedEventArgs e)
    {
        CreateKeyButton.IsEnabled = false;
        SetBusy(true, $"Creating dedicated ZDeployPet key for {_profile.Name}…");
        try
        {
            SshKeyCreationResult result = await _diagnostics.CreateDeploymentKeyAsync(
                _profile.WslDistribution,
                _profile.Name);

            if (!result.Success || result.Candidate is null)
            {
                StatusText.Text = "Dedicated deployment key could not be created.";
                ResultTextBox.Text = result.Error ?? "No diagnostic was returned.";
                return;
            }

            await RefreshKeysAsync();
            SshPublicKeyCandidate? selected = (KeyComboBox.ItemsSource as IEnumerable<SshPublicKeyCandidate>)?
                .FirstOrDefault(candidate =>
                    string.Equals(candidate.PublicKeyPath, result.Candidate.PublicKeyPath, StringComparison.Ordinal));
            if (selected is not null) KeyComboBox.SelectedItem = selected;

            StatusText.Text = result.Created
                ? "Dedicated ZDeployPet deployment key created locally."
                : "Existing dedicated ZDeployPet deployment key found and selected.";
            ResultTextBox.Text =
                (result.Created ? "Created dedicated deployment key" : "Found existing dedicated deployment key") + "\r\n" +
                $"Public key: {result.Candidate.PublicKeyPath}\r\n" +
                $"Fingerprint: {result.Candidate.Fingerprint}\r\n" +
                $"Algorithm: {result.Candidate.Algorithm}\r\n\r\n" +
                "The matching private key remains only inside WSL ~/.ssh and is not stored in the Git repository or profile JSON. " +
                "Click Use selected key to approve this identity for the profile.";
        }
        catch (Exception exception)
        {
            StatusText.Text = "Dedicated deployment key creation failed.";
            ResultTextBox.Text = exception.Message;
        }
        finally
        {
            SetBusy(false);
            CreateKeyButton.IsEnabled = true;
        }
    }
}
