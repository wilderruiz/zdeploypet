using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using ZDeployPet.Core;
using ZDeployPet.Infrastructure;
using ZDeployPet.WslBridge;

namespace ZDeployPet.App;

public sealed class DryRunWindow : Window
{
    private const string AllTargetsId = "all";

    private readonly DeploymentProfile _profile;
    private readonly DeploymentSessionController _sessionController;
    private readonly Func<bool> _targetsReady;
    private readonly Func<Task> _refreshReports;
    private readonly Action<bool> _executionStateChanged;
    private readonly DeploymentExecutionGate _executionGate;
    private readonly DeploymentScriptApprovalStore _approvalStore = new();
    private readonly WslDryRunExecutor _executor = new();

    private readonly TextBlock _approvalState = new();
    private readonly TextBlock _fingerprintText = new();
    private readonly ComboBox _targetCombo = new();
    private readonly ComboBox _releaseCombo = new();
    private readonly Button _approveButton = new();
    private readonly Button _startButton = new();
    private readonly TextBlock _statusText = new();

    private string? _currentSha256;

    private sealed record TargetOption(string Id, string Label, int PromptChoice);
    private sealed record ReleaseOption(string Token, string Label, int PromptChoice);

    public DryRunWindow(
        DeploymentProfile profile,
        DeploymentSessionController sessionController,
        Func<bool> targetsReady,
        Func<Task> refreshReports,
        Action<bool> executionStateChanged,
        DeploymentExecutionGate executionGate)
    {
        _profile = profile;
        _sessionController = sessionController;
        _targetsReady = targetsReady;
        _refreshReports = refreshReports;
        _executionStateChanged = executionStateChanged;
        _executionGate = executionGate;

        Title = "Dry run — ZDeployPet";
        Width = 720;
        Height = 620;
        MinWidth = 620;
        MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = FindBrush("AppBackgroundBrush", Brushes.Black);
        Foreground = FindBrush("AppTextBrush", Brushes.White);
        Content = BuildContent();

        Loaded += async (_, _) =>
        {
            ThemeRuntime.Apply(this);
            await RefreshApprovalStateAsync();
        };
    }

    private UIElement BuildContent()
    {
        ScrollViewer scroll = new()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(24)
        };

        StackPanel body = new();
        body.Children.Add(new TextBlock
        {
            Text = "Allowlisted dry run",
            FontSize = 24,
            FontWeight = FontWeights.SemiBold
        });
        body.Children.Add(new TextBlock
        {
            Text = "Runs only the configured project deployment script in dry-run mode. No arbitrary command field exists here.",
            Margin = new Thickness(0, 5, 0, 18),
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        });

        body.Children.Add(BuildApprovalCard());
        body.Children.Add(BuildRunCard());

        _statusText.Margin = new Thickness(0, 14, 0, 0);
        _statusText.TextWrapping = TextWrapping.Wrap;
        _statusText.Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray);
        AutomationProperties.SetName(_statusText, "Dry run status");
        body.Children.Add(_statusText);

        scroll.Content = body;
        return scroll;
    }

    private Border BuildApprovalCard()
    {
        StackPanel body = new();
        body.Children.Add(new TextBlock
        {
            Text = "1. Approved deployment script",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold
        });
        body.Children.Add(new TextBlock
        {
            Text = $"Configured script: {_profile.ScriptRelativePath}",
            Margin = new Thickness(0, 8, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });

        _fingerprintText.Margin = new Thickness(0, 4, 0, 0);
        _fingerprintText.TextWrapping = TextWrapping.Wrap;
        _fingerprintText.FontFamily = new FontFamily("Consolas");
        body.Children.Add(_fingerprintText);

        _approvalState.Margin = new Thickness(0, 8, 0, 0);
        _approvalState.FontWeight = FontWeights.SemiBold;
        _approvalState.TextWrapping = TextWrapping.Wrap;
        body.Children.Add(_approvalState);

        _approveButton.Content = "Approve current script fingerprint";
        _approveButton.Padding = new Thickness(14, 8, 14, 8);
        _approveButton.Margin = new Thickness(0, 12, 0, 0);
        _approveButton.HorizontalAlignment = HorizontalAlignment.Left;
        _approveButton.Click += async (_, _) => await ApproveCurrentScriptAsync();
        AutomationProperties.SetName(_approveButton, "Approve current deployment script fingerprint");
        body.Children.Add(_approveButton);

        return WrapCard(body);
    }

    private Border BuildRunCard()
    {
        StackPanel body = new();
        body.Children.Add(new TextBlock
        {
            Text = "2. Dry-run request",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold
        });

        body.Children.Add(new TextBlock
        {
            Text = "Target",
            Margin = new Thickness(0, 12, 0, 4),
            FontWeight = FontWeights.SemiBold
        });

        List<TargetOption> targets = BuildTargetOptions();
        _targetCombo.ItemsSource = targets;
        _targetCombo.DisplayMemberPath = nameof(TargetOption.Label);
        _targetCombo.SelectedIndex = targets.Count > 0 ? 0 : -1;
        _targetCombo.MinWidth = 280;
        _targetCombo.HorizontalAlignment = HorizontalAlignment.Left;
        _targetCombo.SelectionChanged += (_, _) => RefreshReleaseAvailability();
        AutomationProperties.SetName(_targetCombo, "Dry run target");
        body.Children.Add(_targetCombo);

        body.Children.Add(new TextBlock
        {
            Text = "Release type",
            Margin = new Thickness(0, 12, 0, 4),
            FontWeight = FontWeights.SemiBold
        });

        ReleaseOption[] releases =
        [
            new("patch", "PATCH", 1),
            new("minor", "MINOR", 2),
            new("major", "MAJOR", 3)
        ];
        _releaseCombo.ItemsSource = releases;
        _releaseCombo.DisplayMemberPath = nameof(ReleaseOption.Label);
        _releaseCombo.SelectedIndex = 0;
        _releaseCombo.MinWidth = 180;
        _releaseCombo.HorizontalAlignment = HorizontalAlignment.Left;
        AutomationProperties.SetName(_releaseCombo, "Dry run release type");
        body.Children.Add(_releaseCombo);

        _startButton.Content = "Start dry run";
        _startButton.Padding = new Thickness(18, 10, 18, 10);
        _startButton.Margin = new Thickness(0, 18, 0, 0);
        _startButton.HorizontalAlignment = HorizontalAlignment.Left;
        _startButton.Click += async (_, _) => await StartDryRunAsync();
        AutomationProperties.SetName(_startButton, "Start allowlisted dry run");
        body.Children.Add(_startButton);

        TextBlock safety = new()
        {
            Text = "Safety: deployment access must already be ON / READY, the current script must match the explicitly approved SHA-256, and only one execution can run at a time. PDA-5 always sends dry-run mode; live deployment is not available here.",
            Margin = new Thickness(0, 12, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        };
        body.Children.Add(safety);

        RefreshReleaseAvailability();
        return WrapCard(body);
    }

    private async Task RefreshApprovalStateAsync()
    {
        string scriptPath;
        try
        {
            scriptPath = Path.GetFullPath(Path.Combine(_profile.WindowsProjectPath, _profile.ScriptRelativePath));
        }
        catch (Exception exception)
        {
            SetApprovalBlocked("The configured deployment script path is invalid: " + exception.Message);
            return;
        }

        if (!File.Exists(scriptPath))
        {
            SetApprovalBlocked("The configured deployment script does not exist on Windows.");
            return;
        }

        try
        {
            _currentSha256 = LocalCapabilityDiscovery.ComputeSha256(scriptPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            SetApprovalBlocked("The configured deployment script could not be fingerprinted: " + exception.Message);
            return;
        }

        _fingerprintText.Text = "Current SHA-256: " + _currentSha256;
        DeploymentScriptApproval? approval = await _approvalStore.LoadAsync(_profile.Id);
        DeploymentScriptApprovalMatchResult match = DeploymentScriptApprovalValidator.Validate(
            approval,
            _profile.Id,
            _profile.ScriptRelativePath,
            _currentSha256);

        if (match.IsMatch)
        {
            _approvalState.Text = $"APPROVED — fingerprint matches the approval from {approval!.ApprovedAtUtc.ToLocalTime():g}.";
            _approvalState.Foreground = Brushes.LightGreen;
            _approveButton.IsEnabled = false;
        }
        else
        {
            _approvalState.Text = "NOT APPROVED — " + string.Join(" ", match.Errors);
            _approvalState.Foreground = FindBrush("AppWarningBorderBrush", Brushes.Goldenrod);
            _approveButton.IsEnabled = true;
        }
    }

    private async Task ApproveCurrentScriptAsync()
    {
        await RefreshApprovalStateAsync();
        if (!DeploymentScriptApprovalValidator.IsSha256(_currentSha256))
            return;

        MessageBoxResult confirmation = MessageBox.Show(
            this,
            $"Approve this exact deployment script for dry-run execution?\n\nScript: {_profile.ScriptRelativePath}\nSHA-256: {_currentSha256}\n\nAny later script change will block execution until you explicitly approve the new fingerprint.",
            "Approve deployment script",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes)
            return;

        DeploymentScriptApproval approval = new(
            DeploymentScriptApproval.CurrentSchemaVersion,
            _profile.Id,
            _profile.ScriptRelativePath,
            _currentSha256!,
            DateTimeOffset.UtcNow);
        await _approvalStore.SaveAsync(approval);
        ShellRuntime.Activity.Add(
            ShellActivityLevel.Success,
            "Dry run",
            $"Approved deployment script fingerprint for profile '{_profile.Name}': {_profile.ScriptRelativePath} / {_currentSha256}.");
        await RefreshApprovalStateAsync();
    }

    private async Task StartDryRunAsync()
    {
        if (_executionGate.IsRunning)
        {
            SetStatus("BLOCKED — another deployment execution is already running.", warning: true);
            return;
        }

        if (!_targetsReady())
        {
            SetStatus("BLOCKED — deployment access is not ON / READY with all configured target probes passed.", warning: true);
            return;
        }

        TargetOption? target = _targetCombo.SelectedItem as TargetOption;
        if (target is null)
        {
            SetStatus("BLOCKED — choose an allowlisted target.", warning: true);
            return;
        }

        ReleaseOption? release = _releaseCombo.SelectedItem as ReleaseOption;
        bool releasePromptRequired = target.PromptChoice != 2;
        if (releasePromptRequired && release is null)
        {
            SetStatus("BLOCKED — choose a release type.", warning: true);
            return;
        }

        await RefreshApprovalStateAsync();
        DeploymentScriptApproval? approval = await _approvalStore.LoadAsync(_profile.Id);
        DeploymentScriptApprovalMatchResult approvalMatch = DeploymentScriptApprovalValidator.Validate(
            approval,
            _profile.Id,
            _profile.ScriptRelativePath,
            _currentSha256 ?? string.Empty);
        if (!approvalMatch.IsMatch)
        {
            SetStatus("BLOCKED — " + string.Join(" ", approvalMatch.Errors), warning: true);
            return;
        }

        WslSshAgentRuntime? runtime = await _sessionController.GetReadyRuntimeAsync(_profile);
        if (runtime is null)
        {
            SetStatus("BLOCKED — " + _sessionController.Message, warning: true);
            return;
        }

        if (!_executionGate.TryAcquire(out IDisposable? executionLease) || executionLease is null)
        {
            SetStatus("BLOCKED — another deployment execution acquired the execution lock.", warning: true);
            return;
        }

        using (executionLease)
        {
            string releaseToken = releasePromptRequired ? release!.Token : "unchanged";
            List<string> allowlistedTargetIds = _profile.Targets.Select(item => item.Id).ToList();
            if (BuildTargetOptions().Any(option => option.Id == AllTargetsId))
                allowlistedTargetIds.Add(AllTargetsId);

            DryRunRequest request = new(_profile.Id, target.Id, releaseToken);
            DryRunExecutionGuardResult guard = DryRunExecutionGuard.Validate(
                request,
                new DryRunExecutionGuardContext(
                    _profile.ScriptRelativePath,
                    approval!.Sha256,
                    _currentSha256!,
                    allowlistedTargetIds,
                    DeploymentAccessReady: true,
                    ExecutionAlreadyRunning: false));
            if (!guard.IsAllowed)
            {
                SetStatus("BLOCKED — " + string.Join(" ", guard.Errors), warning: true);
                return;
            }

            DryRunPromptPlan plan = new(
                target.PromptChoice,
                releasePromptRequired ? release!.PromptChoice : null);

            _startButton.IsEnabled = false;
            _approveButton.IsEnabled = false;
            _targetCombo.IsEnabled = false;
            _releaseCombo.IsEnabled = false;
            _executionStateChanged(true);
            SetStatus("RUNNING — the approved script is executing in WSL dry-run mode.");
            ShellRuntime.Activity.Add(
                ShellActivityLevel.Info,
                "Dry run",
                $"Dry run started for '{_profile.Name}', target '{target.Label}', release '{releaseToken}'. Script SHA-256 {_currentSha256}.");

            try
            {
                WslDryRunExecutionResult result = await _executor.ExecuteAsync(
                    _profile.WslDistribution,
                    _profile.WslProjectPath,
                    _profile.ScriptRelativePath,
                    runtime,
                    plan);

                if (!result.Started)
                {
                    SetStatus("FAILED TO START — " + (result.Error ?? "The WSL dry-run process did not start."), warning: true);
                    ShellRuntime.Activity.Add(ShellActivityLevel.Error, "Dry run", result.Error ?? "Dry-run process did not start.");
                    return;
                }

                ShellRuntime.Activity.Add(
                    result.ExitCode == 0 ? ShellActivityLevel.Success : ShellActivityLevel.Warning,
                    "Dry run",
                    $"Dry-run process exited with code {result.ExitCode}. Reporter truth will now be refreshed and remains authoritative.");

                AddExecutionHighlights(result.StandardOutput, result.StandardError);
                SetStatus($"PROCESS COMPLETE — exit code {result.ExitCode}. Refreshing authoritative deployment report…");
                await _refreshReports();
                SetStatus($"PROCESS COMPLETE — exit code {result.ExitCode}. Review Latest deployment report / Deployment history for authoritative reporter truth.", result.ExitCode != 0);
            }
            catch (OperationCanceledException)
            {
                SetStatus("CANCELLED — the dry-run process was cancelled.", warning: true);
                ShellRuntime.Activity.Add(ShellActivityLevel.Warning, "Dry run", "Dry-run execution was cancelled.");
            }
            catch (Exception exception)
            {
                SetStatus("ERROR — " + exception.Message, warning: true);
                ShellRuntime.Activity.Add(ShellActivityLevel.Error, "Dry run", exception.Message);
            }
            finally
            {
                _executionStateChanged(false);
                _startButton.IsEnabled = true;
                _targetCombo.IsEnabled = true;
                RefreshReleaseAvailability();
                await RefreshApprovalStateAsync();
            }
        }
    }

    private void AddExecutionHighlights(string stdout, string stderr)
    {
        IEnumerable<string> lines = stdout
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Concat(stderr.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

        string[] highlights = lines
            .Select(line => line.Trim())
            .Where(line =>
                line.StartsWith("PASS", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("FAIL", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("DRY RUN COMPLETE", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("PREFLIGHT BLOCKER", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("DEPLOYMENT FAILURE", StringComparison.OrdinalIgnoreCase))
            .Take(40)
            .ToArray();

        foreach (string line in highlights)
        {
            ShellActivityLevel level = line.StartsWith("FAIL", StringComparison.OrdinalIgnoreCase) ||
                                       line.Contains("BLOCKER", StringComparison.OrdinalIgnoreCase) ||
                                       line.Contains("FAILURE", StringComparison.OrdinalIgnoreCase)
                ? ShellActivityLevel.Warning
                : line.Contains("DRY RUN COMPLETE", StringComparison.OrdinalIgnoreCase)
                    ? ShellActivityLevel.Success
                    : ShellActivityLevel.Info;
            ShellRuntime.Activity.Add(level, "Dry run", line);
        }
    }

    private List<TargetOption> BuildTargetOptions()
    {
        List<TargetOption> options = [];
        foreach (DeploymentTarget target in _profile.Targets)
        {
            int choice = string.Equals(target.Id, "hostinger", StringComparison.OrdinalIgnoreCase)
                ? 1
                : string.Equals(target.Id, "vps", StringComparison.OrdinalIgnoreCase)
                    ? 2
                    : Math.Min(options.Count + 1, 3);
            if (choice is < 1 or > 3) continue;
            options.Add(new(target.Id, target.Label, choice));
        }

        bool hasHostinger = _profile.Targets.Any(target => string.Equals(target.Id, "hostinger", StringComparison.OrdinalIgnoreCase));
        bool hasVps = _profile.Targets.Any(target => string.Equals(target.Id, "vps", StringComparison.OrdinalIgnoreCase));
        if (hasHostinger && hasVps)
            options.Add(new(AllTargetsId, "All configured targets", 3));

        return options
            .GroupBy(option => option.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private void RefreshReleaseAvailability()
    {
        TargetOption? target = _targetCombo.SelectedItem as TargetOption;
        bool releaseRequired = target is null || target.PromptChoice != 2;
        _releaseCombo.IsEnabled = releaseRequired && !_executionGate.IsRunning;
    }

    private void SetApprovalBlocked(string message)
    {
        _currentSha256 = null;
        _fingerprintText.Text = "Current SHA-256: unavailable";
        _approvalState.Text = "BLOCKED — " + message;
        _approvalState.Foreground = FindBrush("AppAccentBrush", Brushes.IndianRed);
        _approveButton.IsEnabled = false;
    }

    private void SetStatus(string message, bool warning = false)
    {
        _statusText.Text = message;
        _statusText.Foreground = warning
            ? FindBrush("AppWarningBorderBrush", Brushes.Goldenrod)
            : FindBrush("AppMutedTextBrush", Brushes.LightGray);
    }

    private static Border WrapCard(UIElement content) => new()
    {
        Margin = new Thickness(0, 0, 0, 14),
        Padding = new Thickness(16),
        Background = FindBrush("AppSurfaceBrush", Brushes.DimGray),
        BorderBrush = FindBrush("AppBorderBrush", Brushes.Gray),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(4),
        Child = content
    };

    private static Brush FindBrush(string key, Brush fallback)
        => Application.Current.TryFindResource(key) as Brush ?? fallback;
}
