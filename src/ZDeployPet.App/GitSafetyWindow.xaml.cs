using System.Text;
using System.Windows;
using ZDeployPet.Core;
using ZDeployPet.Infrastructure;

namespace ZDeployPet.App;

public partial class GitSafetyWindow : Window
{
    private readonly DeploymentProfile _profile;
    private readonly GitSafetyService _gitSafety = new();

    public GitSafetyWindow(DeploymentProfile profile)
    {
        InitializeComponent();
        _profile = profile;
        ProfileText.Text = $"Profile: {profile.Name}   •   Repository: {profile.WindowsProjectPath}";

        HelpTipFactory.AttachToButton(ApplyIgnoreButton, new HelpTipSpec(
            "Keep deployment private keys outside Git.",
            "ZDeployPet checks tracked files and unignored untracked files for common private-key markers and for ZDeployPet-style private-key filenames.",
            WhenToUse: "Run this before release/push-oriented work and whenever deployment credentials or local project files change.",
            WhatItDoes: "The scan is read-only. The separate .gitignore action adds a narrowly scoped managed block for ZDeployPet local/private paths and managed-key filenames while preserving the rest of your .gitignore.",
            Example: "A managed key such as zdeploypet_millenova_ed25519 must remain in WSL ~/.ssh. Its .pub half may be installed on a server, but the private file must never appear in the project repository.",
            Safety: "The scanner never copies, uploads, stages or deletes keys. A finding blocks readiness; adding .gitignore does not untrack a file that Git already knows about.",
            Tip: "Public .pub files are deliberately not blanket-ignored because public keys are not secrets and some projects intentionally version them."));
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await RunScanAsync();

    private async void Scan_Click(object sender, RoutedEventArgs e) => await RunScanAsync();

    private async Task RunScanAsync()
    {
        SetBusy(true, "Scanning tracked and unignored repository files for private-key material…");
        try
        {
            GitSafetyReport report = await _gitSafety.ScanAsync(_profile.WindowsProjectPath);
            PresentReport(report);
        }
        catch (Exception exception)
        {
            StatusText.Text = "Git safety scan failed.";
            ResultTextBox.Text = exception.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void ApplyIgnore_Click(object sender, RoutedEventArgs e)
    {
        MessageBoxResult answer = MessageBox.Show(
            this,
            "ZDeployPet will add or refresh only its clearly marked safety block in the repository .gitignore. Existing project rules outside that block are preserved.\n\n" +
            "This does not remove a secret that is already tracked by Git. Continue?",
            "Update .gitignore safety block?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        SetBusy(true, "Updating the ZDeployPet .gitignore safety block…");
        try
        {
            GitIgnoreUpdateResult result = await _gitSafety.EnsureManagedIgnoreBlockAsync(_profile.WindowsProjectPath);
            StatusText.Text = result.Message;
            if (!result.Success)
            {
                ResultTextBox.Text = result.Message;
                return;
            }
            await RunScanAsync();
        }
        catch (Exception exception)
        {
            StatusText.Text = "The .gitignore safety block could not be updated.";
            ResultTextBox.Text = exception.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void PresentReport(GitSafetyReport report)
    {
        StringBuilder text = new();
        text.AppendLine($"Repository: {_profile.WindowsProjectPath}");
        text.AppendLine($"Git working tree: {(report.IsGitRepository ? "YES" : "NO")}");
        text.AppendLine($"ZDeployPet .gitignore block: {(report.ManagedIgnoreBlockPresent ? "PRESENT" : "MISSING")}");
        text.AppendLine();

        if (!string.IsNullOrWhiteSpace(report.Error))
        {
            text.AppendLine("SCAN ERROR");
            text.AppendLine(report.Error);
            StatusText.Text = "Repository safety could not be established.";
            ApplyIgnoreButton.IsEnabled = report.IsGitRepository;
        }
        else if (report.Findings.Count == 0)
        {
            text.AppendLine("PRIVATE-KEY MATERIAL: none detected in tracked or unignored files.");
            text.AppendLine();
            text.AppendLine(report.ManagedIgnoreBlockPresent
                ? "Result: PASS — repository safety check passed and the managed ignore block is present."
                : "Result: CHECK PASSED — no private-key material was detected, but the recommended .gitignore safety block is still missing.");
            StatusText.Text = report.ManagedIgnoreBlockPresent
                ? "Git safety passed."
                : "Scan passed — add the recommended .gitignore safety block.";
            ApplyIgnoreButton.IsEnabled = report.IsGitRepository && !report.ManagedIgnoreBlockPresent;
        }
        else
        {
            text.AppendLine("BLOCKED — possible private-key material is inside the repository boundary:");
            text.AppendLine();
            foreach (GitSafetyFinding finding in report.Findings)
            {
                text.Append("- ").Append(finding.RelativePath)
                    .Append(finding.IsTracked ? " [TRACKED]" : " [UNTRACKED]")
                    .AppendLine();
                text.Append("  ").AppendLine(finding.Reason);
            }
            text.AppendLine();
            text.AppendLine("Do not push or release while these findings remain. .gitignore cannot make an already tracked secret safe; remove the material from the repository and rotate/revoke exposed credentials if necessary.");
            StatusText.Text = "Git safety BLOCKED — private-key material requires attention.";
            ApplyIgnoreButton.IsEnabled = report.IsGitRepository && !report.ManagedIgnoreBlockPresent;
        }

        ResultTextBox.Text = text.ToString();
    }

    private void SetBusy(bool busy, string? status = null)
    {
        ScanButton.IsEnabled = !busy;
        if (busy) ApplyIgnoreButton.IsEnabled = false;
        if (!string.IsNullOrWhiteSpace(status)) StatusText.Text = status;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
