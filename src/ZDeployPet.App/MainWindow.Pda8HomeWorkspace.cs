using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZDeployPet.Core;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private enum HomeWorkspaceSection
    {
        Dry,
        Live,
        Reports
    }

    private bool _pda8HomeWorkspaceInitialized;
    private bool _homeWorkflowEmbeddingBusy;
    private bool _homeWorkflowsEmbedded;
    private Button? _homeDryTabButton;
    private Button? _homeLiveTabButton;
    private Button? _homeReportsTabButton;
    private FrameworkElement? _homeDryContent;
    private FrameworkElement? _homeLiveContent;
    private FrameworkElement? _homeReportsContent;
    private Grid? _homeDryBodyHost;
    private Grid? _homeLiveBodyHost;
    private FrameworkElement? _homeDryLockedInstructions;
    private FrameworkElement? _homeLiveLockedInstructions;
    private HomeWorkspaceSection _homeWorkspaceSection = HomeWorkspaceSection.Reports;

    private void InitializePda8HomeWorkspace()
    {
        if (_pda8HomeWorkspaceInitialized)
            return;

        if (_operatorAccessCard?.Child is not Grid ||
            _dryRunButton is null ||
            _liveReviewButton is null ||
            _latestReportCard is null ||
            _deploymentHistoryCard is null)
        {
            return;
        }

        _pda8HomeWorkspaceInitialized = true;

        // The legacy DRY/LIVE buttons remain as internal state holders for PDA-5/PDA-6
        // but are no longer part of the visible Home UI.
        DetachWorkspaceElement(_dryRunButton);
        DetachWorkspaceElement(_liveReviewButton);
        if (_deploymentActionsHelpButton is not null)
            DetachWorkspaceElement(_deploymentActionsHelpButton);
        DetachWorkspaceElement(_latestReportCard);
        DetachWorkspaceElement(_deploymentHistoryCard);

        Grid root = new();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Grid tabs = new()
        {
            Margin = new Thickness(0, 0, 0, 12)
        };
        tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _homeDryTabButton = CreateHomeWorkspaceTab("DRY", "Dry-run workspace");
        _homeDryTabButton.Click += (_, _) => SelectHomeWorkspaceSection(HomeWorkspaceSection.Dry);
        Grid.SetColumn(_homeDryTabButton, 0);
        tabs.Children.Add(_homeDryTabButton);

        _homeLiveTabButton = CreateHomeWorkspaceTab("LIVE", "Live deployment workspace");
        _homeLiveTabButton.Click += (_, _) => SelectHomeWorkspaceSection(HomeWorkspaceSection.Live);
        Grid.SetColumn(_homeLiveTabButton, 1);
        tabs.Children.Add(_homeLiveTabButton);

        _homeReportsTabButton = CreateHomeWorkspaceTab("REPORTS", "Deployment reports and history");
        _homeReportsTabButton.Click += (_, _) => SelectHomeWorkspaceSection(HomeWorkspaceSection.Reports);
        Grid.SetColumn(_homeReportsTabButton, 2);
        tabs.Children.Add(_homeReportsTabButton);

        if (_deploymentActionsHelpButton is not null)
        {
            _deploymentActionsHelpButton.Margin = new Thickness(8, 0, 0, 0);
            _deploymentActionsHelpButton.HorizontalAlignment = HorizontalAlignment.Right;
            _deploymentActionsHelpButton.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(_deploymentActionsHelpButton, 4);
            tabs.Children.Add(_deploymentActionsHelpButton);
        }

        root.Children.Add(tabs);

        Grid contentHost = new();
        Grid.SetRow(contentHost, 1);
        root.Children.Add(contentHost);

        _homeDryContent = BuildActionWorkspaceShell(isLive: false, out _homeDryBodyHost, out _homeDryLockedInstructions);
        contentHost.Children.Add(_homeDryContent);

        _homeLiveContent = BuildActionWorkspaceShell(isLive: true, out _homeLiveBodyHost, out _homeLiveLockedInstructions);
        contentHost.Children.Add(_homeLiveContent);

        StackPanel reports = new();
        _latestReportCard.Margin = new Thickness(0, 0, 0, 12);
        _deploymentHistoryCard.Margin = new Thickness(0);
        reports.Children.Add(_latestReportCard);
        reports.Children.Add(_deploymentHistoryCard);
        _homeReportsContent = reports;
        contentHost.Children.Add(_homeReportsContent);

        _operatorAccessCard.Padding = new Thickness(14);
        _operatorAccessCard.Child = root;

        SelectHomeWorkspaceSection(HomeWorkspaceSection.Reports);
        RefreshPda8HomeWorkspaceAccessState();
    }

    private FrameworkElement BuildActionWorkspaceShell(
        bool isLive,
        out Grid bodyHost,
        out FrameworkElement lockedInstructions)
    {
        Border surface = new()
        {
            Padding = new Thickness(14),
            Background = FindBrush("AppSurfaceBrush", Brushes.DarkSlateGray),
            BorderBrush = new SolidColorBrush(isLive
                ? Color.FromRgb(0x4E, 0x8B, 0x63)
                : Color.FromRgb(0x4F, 0x6F, 0x96)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4)
        };

        bodyHost = new Grid();
        lockedInstructions = BuildLockedInstructions(isLive);
        bodyHost.Children.Add(lockedInstructions);
        surface.Child = bodyHost;
        return surface;
    }

    private FrameworkElement BuildLockedInstructions(bool isLive)
    {
        StackPanel body = new();
        body.Children.Add(new TextBlock
        {
            Text = isLive ? "Live deployment" : "Dry run",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = FindBrush("AppTextBrush", Brushes.White)
        });
        body.Children.Add(new TextBlock
        {
            Text = isLive
                ? "Use this tab to review and explicitly authorize an immutable LIVE deployment. The deployment controls appear here only after ZDeploy Pet is READY."
                : "Use this tab to run the configured deployment script in allowlisted dry-run mode. The deployment controls appear here only after ZDeploy Pet is READY.",
            Margin = new Thickness(0, 6, 0, 14),
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        });

        body.Children.Add(BuildInstructionStep(
            "1",
            "Click the ZDeploy Pet card at the top-right to turn deployment access on."));
        body.Children.Add(BuildInstructionStep(
            "2",
            "If OpenSSH needs your private-key passphrase, enter it only in the trusted terminal that appears. ZDeployPet never receives it."));
        body.Children.Add(BuildInstructionStep(
            "3",
            isLive
                ? "When ZDeploy Pet shows READY, this tab will automatically show the live review controls here. Create the immutable review, type the exact confirmation phrase, validate it, then launch only if the frozen safety state still matches."
                : "When ZDeploy Pet shows READY, this tab will automatically show the approved-script and dry-run controls here. Choose the target/release and start the dry run."));

        return body;
    }

    private FrameworkElement BuildInstructionStep(string number, string text)
    {
        Grid row = new() { Margin = new Thickness(0, 0, 0, 10) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Border badge = new()
        {
            MinWidth = 28,
            MinHeight = 28,
            Margin = new Thickness(0, 0, 10, 0),
            Background = FindBrush("AppSurfaceRaisedBrush", Brushes.DimGray),
            BorderBrush = FindBrush("AppBorderBrush", Brushes.Gray),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Child = new TextBlock
            {
                Text = number,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold,
                Foreground = FindBrush("AppTextBrush", Brushes.White)
            }
        };
        row.Children.Add(badge);

        TextBlock description = new()
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        };
        Grid.SetColumn(description, 1);
        row.Children.Add(description);
        return row;
    }

    private void RefreshPda8HomeWorkspaceAccessState()
    {
        if (!_pda8HomeWorkspaceInitialized || _homeDryBodyHost is null || _homeLiveBodyHost is null)
            return;

        bool accessReady =
            _activeProfile is not null &&
            _deploymentSession.TargetsReady &&
            _deploymentSession.State is DeploymentAccessSessionState.Ready or DeploymentAccessSessionState.Expiring;

        if (!accessReady)
        {
            if (!_dryRunExecutionBusy && !_liveExecutionBusy && !_deploymentExecutionGate.IsRunning)
                ResetEmbeddedDeploymentWorkflows();
            return;
        }

        if (!_homeWorkflowsEmbedded && !_homeWorkflowEmbeddingBusy)
            _ = EmbedDeploymentWorkflowsAsync();
    }

    private async Task EmbedDeploymentWorkflowsAsync()
    {
        if (_homeWorkflowEmbeddingBusy || _homeWorkflowsEmbedded || _activeProfile is null ||
            _homeDryBodyHost is null || _homeLiveBodyHost is null)
        {
            return;
        }

        _homeWorkflowEmbeddingBusy = true;
        try
        {
            DeploymentProfile profile = _activeProfile;

            DryRunWindow dryController = new(
                profile,
                _deploymentSession,
                targetsReady: () =>
                    _deploymentSession.TargetsReady &&
                    _deploymentSession.State is DeploymentAccessSessionState.Ready or DeploymentAccessSessionState.Expiring,
                refreshReports: async () => await RefreshLatestDeploymentReportAsync(),
                executionStateChanged: busy =>
                {
                    _dryRunExecutionBusy = busy;
                    RefreshPda5DryRunVisibility();
                    RefreshPetCompanion();
                },
                executionGate: _deploymentExecutionGate);

            LiveDeploymentReviewWindow liveController = new(
                profile,
                _deploymentSession,
                targetsReady: () =>
                    _deploymentSession.TargetsReady &&
                    _deploymentSession.State is DeploymentAccessSessionState.Ready or DeploymentAccessSessionState.Expiring,
                refreshReports: async () => await RefreshLatestDeploymentReportAsync(),
                executionStateChanged: busy =>
                {
                    _liveExecutionBusy = busy;
                    RefreshPda6LiveReviewVisibility();
                    RefreshPda5DryRunVisibility();
                    RefreshPetCompanion();
                },
                executionGate: _deploymentExecutionGate);

            UIElement dryContent = dryController.TakeEmbeddedContent();
            UIElement liveContent = liveController.TakeEmbeddedContent();

            _dryRunWindow = dryController;
            _liveReviewWindow = liveController;

            _homeDryBodyHost.Children.Clear();
            _homeDryBodyHost.Children.Add(dryContent);
            _homeLiveBodyHost.Children.Clear();
            _homeLiveBodyHost.Children.Add(liveContent);

            await dryController.PrepareEmbeddedAsync();
            _homeWorkflowsEmbedded = true;
        }
        catch (Exception exception)
        {
            ShellRuntime.Activity.Add(
                ShellActivityLevel.Error,
                "Home",
                "Could not embed the deployment workspaces: " + exception.Message);
            ResetEmbeddedDeploymentWorkflows();
        }
        finally
        {
            _homeWorkflowEmbeddingBusy = false;
        }
    }

    private void ResetEmbeddedDeploymentWorkflows()
    {
        if (_homeDryBodyHost is null || _homeLiveBodyHost is null)
            return;

        _homeDryBodyHost.Children.Clear();
        if (_homeDryLockedInstructions is not null)
            _homeDryBodyHost.Children.Add(_homeDryLockedInstructions);

        _homeLiveBodyHost.Children.Clear();
        if (_homeLiveLockedInstructions is not null)
            _homeLiveBodyHost.Children.Add(_homeLiveLockedInstructions);

        _dryRunWindow = null;
        _liveReviewWindow = null;
        _homeWorkflowsEmbedded = false;
    }

    private Button CreateHomeWorkspaceTab(string label, string toolTip)
    {
        return new Button
        {
            Content = label,
            ToolTip = toolTip,
            MinWidth = 104,
            MinHeight = 40,
            Padding = new Thickness(18, 8, 18, 8),
            Margin = new Thickness(0, 0, 8, 0),
            FontWeight = FontWeights.SemiBold,
            Foreground = FindBrush("AppTextBrush", Brushes.White),
            Background = FindBrush("AppSurfaceBrush", Brushes.DarkSlateGray),
            BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand
        };
    }

    private void SelectHomeWorkspaceSection(HomeWorkspaceSection section)
    {
        _homeWorkspaceSection = section;

        if (_homeDryContent is not null)
            _homeDryContent.Visibility = section == HomeWorkspaceSection.Dry ? Visibility.Visible : Visibility.Collapsed;
        if (_homeLiveContent is not null)
            _homeLiveContent.Visibility = section == HomeWorkspaceSection.Live ? Visibility.Visible : Visibility.Collapsed;
        if (_homeReportsContent is not null)
            _homeReportsContent.Visibility = section == HomeWorkspaceSection.Reports ? Visibility.Visible : Visibility.Collapsed;

        ApplyHomeWorkspaceTabVisual(_homeDryTabButton, section == HomeWorkspaceSection.Dry, HomeWorkspaceSection.Dry);
        ApplyHomeWorkspaceTabVisual(_homeLiveTabButton, section == HomeWorkspaceSection.Live, HomeWorkspaceSection.Live);
        ApplyHomeWorkspaceTabVisual(_homeReportsTabButton, section == HomeWorkspaceSection.Reports, HomeWorkspaceSection.Reports);
    }

    private void ApplyHomeWorkspaceTabVisual(Button? button, bool selected, HomeWorkspaceSection section)
    {
        if (button is null)
            return;

        if (!selected)
        {
            button.Background = FindBrush("AppSurfaceBrush", Brushes.DarkSlateGray);
            button.BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray);
            button.Opacity = 0.82;
            return;
        }

        (Color background, Color border) = section switch
        {
            HomeWorkspaceSection.Dry => (Color.FromRgb(0x24, 0x36, 0x4A), Color.FromRgb(0x4F, 0x6F, 0x96)),
            HomeWorkspaceSection.Live => (Color.FromRgb(0x24, 0x3A, 0x2C), Color.FromRgb(0x4E, 0x8B, 0x63)),
            _ => (Color.FromRgb(0x3B, 0x32, 0x20), Color.FromRgb(0x8B, 0x73, 0x3C))
        };

        button.Background = new SolidColorBrush(background);
        button.BorderBrush = new SolidColorBrush(border);
        button.Opacity = 1.0;
    }

    private static void DetachWorkspaceElement(UIElement element)
    {
        DependencyObject? parent = LogicalTreeHelper.GetParent(element);
        parent ??= VisualTreeHelper.GetParent(element);

        switch (parent)
        {
            case Panel panel:
                panel.Children.Remove(element);
                break;
            case Decorator decorator when ReferenceEquals(decorator.Child, element):
                decorator.Child = null;
                break;
            case ContentControl contentControl when ReferenceEquals(contentControl.Content, element):
                contentControl.Content = null;
                break;
        }
    }
}
