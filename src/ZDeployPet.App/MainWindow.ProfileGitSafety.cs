using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZDeployPet.Core;
using ZDeployPet.Infrastructure;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private readonly GitSafetyService _profileGitSafety = new();
    private ContextMenu? _profileGitSafetyMenu;
    private MenuItem? _profileGitScanItem;
    private MenuItem? _profileGitIgnoreItem;
    private bool _profileGitIgnoreAvailable;
    private bool _profileGitSafetyBusy;
    private bool _profileGitSafetyMenuInitialized;

    private void InitializeProfileGitSafetyMenu()
    {
        if (_profileGitSafetyMenuInitialized || _profileGitSafetyButton is null)
            return;

        _profileGitSafetyMenuInitialized = true;

        Style menuItemStyle = new(typeof(MenuItem));
        menuItemStyle.Setters.Add(new Setter(Control.BackgroundProperty, AppSurface));
        menuItemStyle.Setters.Add(new Setter(Control.ForegroundProperty, AppText));
        menuItemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(14, 8, 14, 8)));
        menuItemStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        menuItemStyle.Setters.Add(new Setter(Control.TemplateProperty, BuildProfileGitMenuItemTemplate()));

        Trigger highlighted = new()
        {
            Property = MenuItem.IsHighlightedProperty,
            Value = true
        };
        highlighted.Setters.Add(new Setter(Control.BackgroundProperty, AppSurfaceRaised));
        highlighted.Setters.Add(new Setter(Control.ForegroundProperty, AppText));
        menuItemStyle.Triggers.Add(highlighted);

        _profileGitSafetyMenu = new ContextMenu
        {
            Background = AppSurface,
            Foreground = AppText,
            BorderBrush = AppBorder,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
            ItemContainerStyle = menuItemStyle
        };

        _profileGitScanItem = new MenuItem { Header = "Scan repository" };
        _profileGitScanItem.Click += async (_, _) => await RunProfileGitSafetyScanAsync();
        _profileGitSafetyMenu.Items.Add(_profileGitScanItem);

        _profileGitIgnoreItem = new MenuItem
        {
            Header = "Add/update .gitignore safety block"
        };
        _profileGitIgnoreItem.Click += async (_, _) => await ApplyProfileGitIgnoreAsync();
        _profileGitSafetyMenu.Items.Add(_profileGitIgnoreItem);
        SetProfileGitIgnoreAvailable(false);

        _profileGitSafetyButton.ContextMenu = _profileGitSafetyMenu;
        _profileGitSafetyButton.PreviewMouseLeftButtonDown += ProfileGitSafetyButton_PreviewMouseLeftButtonDown;
        _profileGitSafetyButton.PreviewKeyDown += ProfileGitSafetyButton_PreviewKeyDown;

        if (_profileStageToolbar?.Child is StackPanel row && row.Children.Count > 1 && row.Children[1] is Button gitInfo)
        {
            HelpTipSpec mergedGitHelp = new(
                "Keep deployment private keys outside Git.",
                "ZDeployPet deployment private keys belong only in WSL ~/.ssh, never inside the project repository. Git safety scans tracked files and unignored untracked files for common private-key material and ZDeployPet-style private-key filenames.",
                WhenToUse: "Run the scan before release/push-oriented work and whenever deployment credentials or local repository files change.",
                WhatItDoes: "The Git icon opens a small menu. Scan repository is read-only. After a scan, Add/update .gitignore safety block becomes available only when the active repository can use the managed defense-in-depth block. That action updates only the clearly marked ZDeployPet block and preserves the rest of .gitignore.",
                Example: "A managed key such as zdeploypet_millenova_ed25519 must stay in WSL ~/.ssh. Its public .pub half may be installed on a server, but the private file must never appear inside the project repository.",
                Safety: "The scanner never copies, uploads, stages or deletes keys. Adding .gitignore does not untrack a secret that Git already knows about and cannot make an already tracked secret safe.",
                Tip: "Public .pub files are deliberately not blanket-ignored because public keys are not secrets and some projects intentionally version them.");

            Button mergedHelpButton = HelpTipFactory.Create(mergedGitHelp);
            gitInfo.ToolTip = mergedHelpButton.ToolTip;
        }
    }

    private static ControlTemplate BuildProfileGitMenuItemTemplate()
    {
        FrameworkElementFactory border = new(typeof(Border));
        border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding(nameof(Control.Background))
        {
            RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
        });

        FrameworkElementFactory presenter = new(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.MarginProperty, new Thickness(14, 8, 14, 8));
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        presenter.SetBinding(ContentPresenter.ContentProperty, new System.Windows.Data.Binding(nameof(MenuItem.Header))
        {
            RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
        });
        presenter.SetBinding(System.Windows.Documents.TextElement.ForegroundProperty, new System.Windows.Data.Binding(nameof(Control.Foreground))
        {
            RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
        });

        border.AppendChild(presenter);
        return new ControlTemplate(typeof(MenuItem)) { VisualTree = border };
    }

    private void ProfileGitSafetyButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        OpenProfileGitSafetyMenu();
    }

    private void ProfileGitSafetyButton_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter and not Key.Space)
            return;

        e.Handled = true;
        OpenProfileGitSafetyMenu();
    }

    private void OpenProfileGitSafetyMenu()
    {
        if (_activeProfile is null || _profileGitSafetyButton is null || _profileGitSafetyMenu is null)
        {
            StatusText.Text = "Save and validate a deployment profile before running Git safety checks.";
            return;
        }

        SetProfileGitIgnoreAvailable(false);
        _profileGitSafetyMenu.PlacementTarget = _profileGitSafetyButton;
        _profileGitSafetyMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        _profileGitSafetyMenu.IsOpen = true;
    }

    private async Task RunProfileGitSafetyScanAsync()
    {
        if (_activeProfile is null || _profileGitSafetyBusy)
            return;

        FocusZDeployPetActivity();
        SetProfileGitSafetyBusy(true);
        ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Git safety", $"Scanning repository '{_activeProfile.Name}' for private-key material.");

        try
        {
            GitSafetyReport report = await _profileGitSafety.ScanAsync(_activeProfile.WindowsProjectPath);

            ShellRuntime.Activity.Add(
                report.IsGitRepository ? ShellActivityLevel.Success : ShellActivityLevel.Warning,
                "Git safety",
                report.IsGitRepository
                    ? "Git working tree detected."
                    : "The configured project folder is not a Git working tree.");

            ShellRuntime.Activity.Add(
                report.ManagedIgnoreBlockPresent ? ShellActivityLevel.Success : ShellActivityLevel.Info,
                "Git safety",
                report.ManagedIgnoreBlockPresent
                    ? "The ZDeployPet .gitignore safety block is present."
                    : "The ZDeployPet .gitignore safety block is not present.");

            if (!string.IsNullOrWhiteSpace(report.Error))
            {
                ShellRuntime.Activity.Add(ShellActivityLevel.Error, "Git safety", $"Repository safety could not be established: {report.Error}");
            }
            else if (report.Findings.Count == 0)
            {
                ShellRuntime.Activity.Add(
                    report.ManagedIgnoreBlockPresent ? ShellActivityLevel.Success : ShellActivityLevel.Warning,
                    "Git safety",
                    report.ManagedIgnoreBlockPresent
                        ? "PASS — no private-key material was detected in tracked or unignored files and the managed ignore block is present."
                        : "CHECK PASSED — no private-key material was detected, but the recommended .gitignore safety block is still missing.");
            }
            else
            {
                ShellRuntime.Activity.Add(ShellActivityLevel.Error, "Git safety", $"BLOCKED — {report.Findings.Count} possible private-key finding(s) are inside the repository boundary.");
                foreach (GitSafetyFinding finding in report.Findings)
                {
                    ShellRuntime.Activity.Add(
                        ShellActivityLevel.Error,
                        "Git safety",
                        $"{finding.RelativePath} {(finding.IsTracked ? "[TRACKED]" : "[UNTRACKED]")} — {finding.Reason}");
                }
                ShellRuntime.Activity.Add(
                    ShellActivityLevel.Warning,
                    "Git safety",
                    "Do not push or release while findings remain. .gitignore cannot make an already tracked secret safe; remove the material and rotate or revoke exposed credentials when necessary.");
            }

            SetProfileGitIgnoreAvailable(report.IsGitRepository && !report.ManagedIgnoreBlockPresent);
        }
        catch (Exception exception)
        {
            ShellRuntime.Activity.Add(ShellActivityLevel.Error, "Git safety", $"Git safety scan failed: {exception.Message}");
            SetProfileGitIgnoreAvailable(false);
        }
        finally
        {
            SetProfileGitSafetyBusy(false);
        }
    }

    private async Task ApplyProfileGitIgnoreAsync()
    {
        if (_activeProfile is null || _profileGitSafetyBusy || !_profileGitIgnoreAvailable)
            return;

        MessageBoxResult answer = MessageBox.Show(
            this,
            "ZDeployPet will add or refresh only its clearly marked safety block in the repository .gitignore. Existing project rules outside that block are preserved.\n\nThis does not remove a secret that is already tracked by Git. Continue?",
            "Update .gitignore safety block?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes)
            return;

        FocusZDeployPetActivity();
        SetProfileGitSafetyBusy(true);
        ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Git safety", "Updating the managed .gitignore safety block.");

        try
        {
            GitIgnoreUpdateResult result = await _profileGitSafety.EnsureManagedIgnoreBlockAsync(_activeProfile.WindowsProjectPath);
            ShellRuntime.Activity.Add(
                result.Success ? ShellActivityLevel.Success : ShellActivityLevel.Error,
                "Git safety",
                result.Message);

            if (result.Success)
            {
                SetProfileGitSafetyBusy(false);
                await RunProfileGitSafetyScanAsync();
                return;
            }
        }
        catch (Exception exception)
        {
            ShellRuntime.Activity.Add(ShellActivityLevel.Error, "Git safety", $"The .gitignore safety block could not be updated: {exception.Message}");
        }
        finally
        {
            SetProfileGitSafetyBusy(false);
        }
    }

    private void SetProfileGitIgnoreAvailable(bool available)
    {
        _profileGitIgnoreAvailable = available;
        if (_profileGitIgnoreItem is null)
            return;

        _profileGitIgnoreItem.IsHitTestVisible = available;
        _profileGitIgnoreItem.Focusable = available;
        _profileGitIgnoreItem.Opacity = available ? 1.0 : 0.45;
        _profileGitIgnoreItem.Background = AppSurface;
        _profileGitIgnoreItem.Foreground = AppText;
    }

    private void SetProfileGitSafetyBusy(bool busy)
    {
        _profileGitSafetyBusy = busy;
        if (_profileGitScanItem is not null)
            _profileGitScanItem.IsEnabled = !busy;
        if (busy)
            SetProfileGitIgnoreAvailable(false);
        if (_profileGitSafetyButton is not null)
            _profileGitSafetyButton.IsEnabled = !busy && _activeProfile is not null;
    }

    private void FocusZDeployPetActivity()
    {
        if (_activityPane is not null && _activityPane.Visibility != Visibility.Visible)
            ToggleEmbeddedActivityPane();
        if (_activityConsoleTabs is not null && _activityConsoleTabs.Items.Count >= 3)
            _activityConsoleTabs.SelectedIndex = 2;
    }
}
