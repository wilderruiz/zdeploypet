using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using ZDeployPet.Core;
using ZDeployPet.Infrastructure;
using ZDeployPet.WslBridge;

namespace ZDeployPet.App;

public sealed class LiveDeploymentReviewWindow : Window
{
    private const string AllTargetsId = "all";

    private readonly DeploymentProfile _profile;
    private readonly DeploymentSessionController _sessionController;
    private readonly Func<bool> _targetsReady;
    private readonly Func<Task> _refreshReports;
    private readonly Action<bool> _executionStateChanged;
    private readonly DeploymentExecutionGate _executionGate;
    private readonly DeploymentScriptApprovalStore _approvalStore = new();
    private readonly WslLiveDeploymentExecutor _executor = new();

    private readonly ComboBox _targetCombo = new();
    private readonly ComboBox _releaseCombo = new();
    private readonly Button _createReviewButton = new();
    private readonly Border _reviewCard = new();
    private readonly StackPanel _reviewBody = new();
    private readonly TextBox _confirmationBox = new();
    private readonly Button _validateButton = new();
    private readonly Button _startLiveButton = new();
    private readonly TextBlock _statusText = new();

    private LiveDeploymentReviewSnapshot? _snapshot;
    private string? _currentSha256;
    private bool _confirmationValidated;
    private bool _executionBusy;

    private sealed record TargetOption(string Id, string Label, int PromptChoice);
    private sealed record ReleaseOption(string Token, string Label, int PromptChoice);

    public LiveDeploymentReviewWindow(
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

        Title = "Live deployment review — ZDeployPet";
        Width = 760;
        Height = 820;
        MinWidth = 650;
        MinHeight = 620;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = FindBrush("AppBackgroundBrush", Brushes.Black);
        Foreground = FindBrush("AppTextBrush", Brushes.White);
        Content = BuildContent();

        Loaded += (_, _) => ThemeRuntime.Apply(this);
    }

    private UIElement BuildContent()
    {
        ScrollViewer scroll = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        StackPanel body = new() { Margin = new Thickness(24) };

        body.Children.Add(new TextBlock
        {
            Text = "Live deployment review",
            FontSize = 24,
            FontWeight = FontWeights.SemiBold
        });
        body.Children.Add(new TextBlock
        {
            Text = "Review the exact live intent, type the confirmation phrase manually, then launch only if every frozen safety condition still matches.",
            Margin = new Thickness(0, 5, 0, 18),
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        });

        body.Children.Add(BuildIntentCard());

        _reviewCard.Margin = new Thickness(0, 14, 0, 0);
        _reviewCard.Padding = new Thickness(18);
        _reviewCard.BorderThickness = new Thickness(1);
        _reviewCard.Background = FindBrush("AppSurfaceBrush", Brushes.DarkSlateGray);
        _reviewCard.BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray);
        _reviewCard.Visibility = Visibility.Collapsed;
        _reviewCard.Child = _reviewBody;
        body.Children.Add(_reviewCard);

        _statusText.Margin = new Thickness(0, 14, 0, 0);
        _statusText.TextWrapping = TextWrapping.Wrap;
        _statusText.Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray);
        AutomationProperties.SetName(_statusText, "Live deployment review status");
        body.Children.Add(_statusText);

        scroll.Content = body;
        return scroll;
    }

    private Border BuildIntentCard()
    {
        StackPanel body = new();
        body.Children.Add(new TextBlock
        {
            Text = "1. Proposed live deployment",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold
        });

        body.Children.Add(Label("Target", 12));
        List<TargetOption> targets = BuildTargetOptions();
        _targetCombo.ItemsSource = targets;
        _targetCombo.DisplayMemberPath = nameof(TargetOption.Label);
        _targetCombo.SelectedIndex = targets.Count > 0 ? 0 : -1;
        _targetCombo.MinWidth = 300;
        _targetCombo.HorizontalAlignment = HorizontalAlignment.Left;
        _targetCombo.SelectionChanged += (_, _) =>
        {
            RefreshReleaseAvailability();
            InvalidateReview("Target changed. Create a new immutable review.");
        };
        body.Children.Add(_targetCombo);

        body.Children.Add(Label("Release type", 12));
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
        _releaseCombo.SelectionChanged += (_, _) => InvalidateReview("Release changed. Create a new immutable review.");
        body.Children.Add(_releaseCombo);

        _createReviewButton.Content = "Create immutable review";
        _createReviewButton.Padding = new Thickness(18, 10, 18, 10);
        _createReviewButton.Margin = new Thickness(0, 18, 0, 0);
        _createReviewButton.HorizontalAlignment = HorizontalAlignment.Left;
        _createReviewButton.Click += async (_, _) => await CreateReviewAsync();
        AutomationProperties.SetName(_createReviewButton, "Create immutable live deployment review");
        body.Children.Add(_createReviewButton);

        body.Children.Add(new TextBlock
        {
            Text = "Safety: review creation performs no deployment. Any later change to target, release, script fingerprint, configured phrase, access state or execution state invalidates authorization.",
            Margin = new Thickness(0, 12, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        });

        RefreshReleaseAvailability();
        return WrapCard(body);
    }

    private async Task CreateReviewAsync()
    {
        if (_executionBusy) return;

        if (!_targetsReady())
        {
            SetStatus("BLOCKED — deployment access must be ON / READY with all configured target probes passed.", true);
            return;
        }

        if (_executionGate.IsRunning)
        {
            SetStatus("BLOCKED — another deployment execution is already running.", true);
            return;
        }

        TargetOption? target = _targetCombo.SelectedItem as TargetOption;
        ReleaseOption? release = _releaseCombo.SelectedItem as ReleaseOption;
        if (target is null)
        {
            SetStatus("BLOCKED — choose a target.", true);
            return;
        }

        bool releaseRequired = target.PromptChoice != 2;
        if (releaseRequired && release is null)
        {
            SetStatus("BLOCKED — choose a release type.", true);
            return;
        }

        string scriptPath;
        try
        {
            scriptPath = Path.GetFullPath(Path.Combine(_profile.WindowsProjectPath, _profile.ScriptRelativePath));
        }
        catch (Exception exception)
        {
            SetStatus("BLOCKED — invalid configured script path: " + exception.Message, true);
            return;
        }

        if (!File.Exists(scriptPath))
        {
            SetStatus("BLOCKED — configured deployment script does not exist.", true);
            return;
        }

        try
        {
            _currentSha256 = LocalCapabilityDiscovery.ComputeSha256(scriptPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            SetStatus("BLOCKED — could not fingerprint the deployment script: " + exception.Message, true);
            return;
        }

        DeploymentScriptApproval? approval = await _approvalStore.LoadAsync(_profile.Id);
        DeploymentScriptApprovalMatchResult approvalMatch = DeploymentScriptApprovalValidator.Validate(
            approval,
            _profile.Id,
            _profile.ScriptRelativePath,
            _currentSha256);
        if (!approvalMatch.IsMatch)
        {
            SetStatus("BLOCKED — " + string.Join(" ", approvalMatch.Errors), true);
            return;
        }

        if (await _sessionController.GetReadyRuntimeAsync(_profile) is null)
        {
            SetStatus("BLOCKED — " + _sessionController.Message, true);
            return;
        }

        string releaseToken = releaseRequired ? release!.Token : "unchanged";
        List<string> allowedIds = _profile.Targets.Select(item => item.Id).ToList();
        if (BuildTargetOptions().Any(option => option.Id == AllTargetsId))
            allowedIds.Add(AllTargetsId);

        LiveDeploymentReviewResult review = LiveDeploymentReviewGuard.CreateReview(
            new LiveDeploymentRequest(_profile.Id, target.Id, releaseToken),
            new LiveDeploymentReviewContext(
                ProfileName: _profile.Name,
                ConfiguredScriptRelativePath: _profile.ScriptRelativePath,
                ApprovedScriptSha256: approval!.Sha256,
                CurrentScriptSha256: _currentSha256,
                AllowedTargetIds: allowedIds,
                TargetLabel: target.Label,
                LiveConfirmationPhrase: _profile.LiveConfirmationPhrase,
                DeploymentAccessReady: true,
                ExecutionAlreadyRunning: false),
            DateTimeOffset.UtcNow);

        if (!review.IsAllowed || review.Snapshot is null)
        {
            SetStatus("BLOCKED — " + string.Join(" ", review.Errors), true);
            return;
        }

        _snapshot = review.Snapshot;
        _confirmationValidated = false;
        RenderSnapshot(_snapshot, target);
        SetStatus("REVIEW CREATED — immutable snapshot captured. Type the exact confirmation phrase manually, then validate it.");
    }

    private void RenderSnapshot(LiveDeploymentReviewSnapshot snapshot, TargetOption target)
    {
        _reviewBody.Children.Clear();
        _reviewBody.Children.Add(new TextBlock
        {
            Text = "2. Immutable review",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold
        });

        AddReviewLine("Profile", snapshot.ProfileName);
        AddReviewLine("Project", _profile.WindowsProjectPath);
        AddReviewLine("Target", snapshot.TargetLabel);
        AddReviewLine("Destinations", BuildDestinationSummary(target));
        AddReviewLine("Release", BuildReleaseSummary(snapshot.Release));
        AddReviewLine("Script", snapshot.ScriptRelativePath);
        AddReviewLine("SHA-256", snapshot.ScriptSha256, monospace: true);
        AddReviewLine("Review ID", snapshot.ReviewId, monospace: true);

        _reviewBody.Children.Add(new TextBlock
        {
            Text = "Exact confirmation phrase",
            Margin = new Thickness(0, 16, 0, 4),
            FontWeight = FontWeights.SemiBold
        });
        _reviewBody.Children.Add(new TextBlock
        {
            Text = snapshot.LiveConfirmationPhrase,
            FontFamily = new FontFamily("Consolas"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });

        _reviewBody.Children.Add(new TextBlock
        {
            Text = "Type the phrase manually",
            Margin = new Thickness(0, 12, 0, 4),
            FontWeight = FontWeights.SemiBold
        });

        _confirmationBox.Text = string.Empty;
        _confirmationBox.MinWidth = 320;
        _confirmationBox.HorizontalAlignment = HorizontalAlignment.Left;
        _confirmationBox.Background = FindBrush("AppConsoleBrush", Brushes.Black);
        _confirmationBox.Foreground = FindBrush("AppTextBrush", Brushes.White);
        _confirmationBox.BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray);
        _confirmationBox.Padding = new Thickness(8, 6, 8, 6);
        _confirmationBox.TextChanged -= ConfirmationBox_TextChanged;
        _confirmationBox.TextChanged += ConfirmationBox_TextChanged;
        AutomationProperties.SetName(_confirmationBox, "Exact live deployment confirmation phrase");
        _reviewBody.Children.Add(_confirmationBox);

        _validateButton.Content = "Validate typed confirmation";
        _validateButton.Padding = new Thickness(16, 9, 16, 9);
        _validateButton.Margin = new Thickness(0, 12, 0, 0);
        _validateButton.HorizontalAlignment = HorizontalAlignment.Left;
        _validateButton.Click -= ValidateConfirmation_Click;
        _validateButton.Click += ValidateConfirmation_Click;
        _reviewBody.Children.Add(_validateButton);

        _startLiveButton.Content = "Start LIVE deployment";
        _startLiveButton.Padding = new Thickness(18, 10, 18, 10);
        _startLiveButton.Margin = new Thickness(0, 12, 0, 0);
        _startLiveButton.HorizontalAlignment = HorizontalAlignment.Left;
        _startLiveButton.IsEnabled = false;
        _startLiveButton.Click -= StartLiveDeployment_Click;
        _startLiveButton.Click += StartLiveDeployment_Click;
        AutomationProperties.SetName(_startLiveButton, "Start live deployment after exact confirmation");
        _reviewBody.Children.Add(_startLiveButton);

        _reviewBody.Children.Add(new TextBlock
        {
            Text = "The LIVE button stays disabled until the exact phrase is manually typed and revalidated. Immediately before launch, ZDeployPet rechecks the script fingerprint, approved script, deployment-access runtime, frozen target/release/phrase and global execution lock. Live runs are never retried automatically.",
            Margin = new Thickness(0, 10, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        });

        _reviewCard.Visibility = Visibility.Visible;
    }

    private void ConfirmationBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _confirmationValidated = false;
        _startLiveButton.IsEnabled = false;
    }

    private async void ValidateConfirmation_Click(object sender, RoutedEventArgs e)
    {
        if (_snapshot is null || _executionBusy) return;

        LiveValidationState validation = await RevalidateAsync();
        if (!validation.Result.IsConfirmed)
        {
            _confirmationValidated = false;
            _startLiveButton.IsEnabled = false;
            SetStatus("CONFIRMATION INVALID — " + string.Join(" ", validation.Result.Errors), true);
            return;
        }

        _confirmationValidated = true;
        _startLiveButton.IsEnabled = true;
        SetStatus("CONFIRMATION VALID — exact human-entered phrase matches this unchanged immutable review. LIVE launch is now armed for this unchanged state only.");
    }

    private async void StartLiveDeployment_Click(object sender, RoutedEventArgs e)
    {
        if (_snapshot is null || !_confirmationValidated || _executionBusy)
            return;

        LiveValidationState validation = await RevalidateAsync();
        if (!validation.Result.IsConfirmed || validation.Runtime is null)
        {
            _confirmationValidated = false;
            _startLiveButton.IsEnabled = false;
            SetStatus("LIVE BLOCKED — " + string.Join(" ", validation.Result.Errors), true);
            return;
        }

        TargetOption? target = _targetCombo.SelectedItem as TargetOption;
        ReleaseOption? release = _releaseCombo.SelectedItem as ReleaseOption;
        if (target is null)
        {
            SetStatus("LIVE BLOCKED — target is no longer selected.", true);
            return;
        }

        bool releaseRequired = target.PromptChoice != 2;
        if (releaseRequired && release is null)
        {
            SetStatus("LIVE BLOCKED — release selection is no longer valid.", true);
            return;
        }

        if (!_executionGate.TryAcquire(out IDisposable? executionLease) || executionLease is null)
        {
            SetStatus("LIVE BLOCKED — another deployment execution acquired the global execution lock.", true);
            return;
        }

        using (executionLease)
        {
            _executionBusy = true;
            _executionStateChanged(true);
            _createReviewButton.IsEnabled = false;
            _targetCombo.IsEnabled = false;
            _releaseCombo.IsEnabled = false;
            _confirmationBox.IsEnabled = false;
            _validateButton.IsEnabled = false;
            _startLiveButton.IsEnabled = false;

            string operatorPhrase = _confirmationBox.Text;
            LiveDeploymentPromptPlan plan = new(
                TargetChoice: target.PromptChoice,
                ReleaseChoice: releaseRequired ? release!.PromptChoice : null,
                ConfirmationPhrase: _snapshot.LiveConfirmationPhrase);

            SetStatus("LIVE RUNNING — the approved script is executing through the bounded deployment-access session. Output is streaming to the Live deploy console.");
            ShellRuntime.Activity.Add(
                ShellActivityLevel.Warning,
                "Live deploy",
                $"Live deployment started for '{_profile.Name}', target '{target.Label}', release '{_snapshot.Release}', review {_snapshot.ReviewId}. No automatic retry is permitted.");
            ShellRuntime.Activity.Add(
                ShellActivityLevel.Info,
                "deploy_millenova.live.sh",
                "--- live script output begins ---");

            try
            {
                WslLiveDeploymentExecutionResult result = await _executor.ExecuteAsync(
                    _profile.WslDistribution,
                    _profile.WslProjectPath,
                    _profile.ScriptRelativePath,
                    validation.Runtime,
                    plan,
                    operatorPhrase,
                    outputLine: (line, isError) =>
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                            ShellRuntime.Activity.Add(
                                isError ? ShellActivityLevel.Warning : ShellActivityLevel.Info,
                                "deploy_millenova.live.sh",
                                line)));
                    });

                ShellRuntime.Activity.Add(
                    ShellActivityLevel.Info,
                    "deploy_millenova.live.sh",
                    "--- live script output ends ---");

                if (!result.Started)
                {
                    SetStatus("LIVE FAILED TO START — " + (result.Error ?? "The WSL live-deployment process did not start."), true);
                    ShellRuntime.Activity.Add(ShellActivityLevel.Error, "Live deploy", result.Error ?? "Live deployment process did not start.");
                    return;
                }

                ShellRuntime.Activity.Add(
                    result.ExitCode == 0 ? ShellActivityLevel.Success : ShellActivityLevel.Warning,
                    "Live deploy",
                    $"Live deployment process exited with code {result.ExitCode}. Reporter truth will now be refreshed and remains authoritative. No retry was attempted.");

                SetStatus($"LIVE PROCESS COMPLETE — exit code {result.ExitCode}. Refreshing authoritative deployment report…", result.ExitCode != 0);
                await _refreshReports();
                SetStatus($"LIVE PROCESS COMPLETE — exit code {result.ExitCode}. Review Latest deployment report / Deployment history for authoritative reporter truth. No automatic retry occurred.", result.ExitCode != 0);
            }
            catch (OperationCanceledException)
            {
                SetStatus("LIVE CANCELLED — execution was cancelled. The deployment may be partial; inspect reporter/full log before any further action. No retry was attempted.", true);
                ShellRuntime.Activity.Add(ShellActivityLevel.Warning, "Live deploy", "Live execution was cancelled. No automatic retry was attempted.");
            }
            catch (Exception exception)
            {
                SetStatus("LIVE ERROR — " + exception.Message + " No automatic retry was attempted.", true);
                ShellRuntime.Activity.Add(ShellActivityLevel.Error, "Live deploy", exception.Message);
            }
            finally
            {
                _executionBusy = false;
                _executionStateChanged(false);
                _confirmationValidated = false;
                _confirmationBox.Text = string.Empty;
                _createReviewButton.IsEnabled = true;
                _targetCombo.IsEnabled = true;
                RefreshReleaseAvailability();
                _confirmationBox.IsEnabled = true;
                _validateButton.IsEnabled = true;
                _startLiveButton.IsEnabled = false;
                _snapshot = null;
                _reviewCard.Visibility = Visibility.Collapsed;
            }
        }
    }

    private async Task<LiveValidationState> RevalidateAsync()
    {
        if (_snapshot is null)
            return new(new LiveDeploymentConfirmationResult(false, ["No immutable live review exists."]), null);

        string currentSha;
        try
        {
            string scriptPath = Path.GetFullPath(Path.Combine(_profile.WindowsProjectPath, _profile.ScriptRelativePath));
            currentSha = LocalCapabilityDiscovery.ComputeSha256(scriptPath);
        }
        catch (Exception exception)
        {
            return new(new LiveDeploymentConfirmationResult(false, ["Script fingerprint could not be revalidated: " + exception.Message]), null);
        }

        DeploymentScriptApproval? approval = await _approvalStore.LoadAsync(_profile.Id);
        DeploymentScriptApprovalMatchResult approvalMatch = DeploymentScriptApprovalValidator.Validate(
            approval,
            _profile.Id,
            _profile.ScriptRelativePath,
            currentSha);
        if (!approvalMatch.IsMatch)
            return new(new LiveDeploymentConfirmationResult(false, approvalMatch.Errors), null);

        WslSshAgentRuntime? runtime = null;
        bool accessReady = _targetsReady();
        if (accessReady)
        {
            runtime = await _sessionController.GetReadyRuntimeAsync(_profile);
            accessReady = runtime is not null;
        }

        TargetOption? selectedTarget = _targetCombo.SelectedItem as TargetOption;
        ReleaseOption? selectedRelease = _releaseCombo.SelectedItem as ReleaseOption;
        string selectedTargetId = selectedTarget?.Id ?? string.Empty;
        string selectedReleaseToken = selectedTarget?.PromptChoice == 2
            ? "unchanged"
            : selectedRelease?.Token ?? string.Empty;

        LiveDeploymentConfirmationResult result = LiveDeploymentReviewGuard.ValidateConfirmation(
            _snapshot,
            _confirmationBox.Text,
            new LiveDeploymentConfirmationContext(
                ProfileId: _profile.Id,
                ScriptRelativePath: _profile.ScriptRelativePath,
                CurrentScriptSha256: currentSha,
                TargetId: selectedTargetId,
                Release: selectedReleaseToken,
                LiveConfirmationPhrase: _profile.LiveConfirmationPhrase,
                DeploymentAccessReady: accessReady,
                ExecutionAlreadyRunning: _executionGate.IsRunning));

        return new(result, result.IsConfirmed ? runtime : null);
    }

    private sealed record LiveValidationState(
        LiveDeploymentConfirmationResult Result,
        WslSshAgentRuntime? Runtime);

    private void InvalidateReview(string reason)
    {
        if (_snapshot is null) return;
        _snapshot = null;
        _confirmationValidated = false;
        _confirmationBox.Text = string.Empty;
        _startLiveButton.IsEnabled = false;
        _reviewCard.Visibility = Visibility.Collapsed;
        SetStatus("REVIEW INVALIDATED — " + reason, true);
    }

    private List<TargetOption> BuildTargetOptions()
    {
        List<TargetOption> options = [];
        foreach (DeploymentTarget target in _profile.Targets)
        {
            int promptChoice = IsVpsTarget(target) ? 2 : 1;
            options.Add(new TargetOption(target.Id, target.Label, promptChoice));
        }

        bool hasHostinger = _profile.Targets.Any(target => !IsVpsTarget(target));
        bool hasVps = _profile.Targets.Any(IsVpsTarget);
        if (hasHostinger && hasVps)
            options.Insert(0, new TargetOption(AllTargetsId, "Both targets", 3));

        return options;
    }

    private static bool IsVpsTarget(DeploymentTarget target)
        => target.Label.Contains("VPS", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(target.User, "root", StringComparison.OrdinalIgnoreCase) ||
           target.Port == 22;

    private void RefreshReleaseAvailability()
    {
        if (_executionBusy) return;

        TargetOption? target = _targetCombo.SelectedItem as TargetOption;
        bool required = target is null || target.PromptChoice != 2;
        _releaseCombo.IsEnabled = required;
        if (!required)
            _releaseCombo.SelectedIndex = -1;
        else if (_releaseCombo.SelectedIndex < 0 && _releaseCombo.Items.Count > 0)
            _releaseCombo.SelectedIndex = 0;
    }

    private string BuildDestinationSummary(TargetOption target)
    {
        IEnumerable<DeploymentDestination> destinations = target.Id == AllTargetsId
            ? _profile.Destinations
            : _profile.Destinations.Where(item => string.Equals(item.TargetId, target.Id, StringComparison.Ordinal));

        string[] lines = destinations.Select(item => $"{item.Label}: {item.RemotePath}").ToArray();
        return lines.Length == 0 ? "No mapped destination" : string.Join("; ", lines);
    }

    private string BuildReleaseSummary(string release)
    {
        if (string.Equals(release, "unchanged", StringComparison.Ordinal))
            return "UNCHANGED (target does not use release prompt)";

        string versionPath = Path.Combine(_profile.WindowsProjectPath, "version.txt");
        try
        {
            string current = File.ReadAllText(versionPath).Trim().TrimStart('v', 'V');
            string[] parts = current.Split('.');
            if (parts.Length == 3 && int.TryParse(parts[0], out int major) && int.TryParse(parts[1], out int minor) && int.TryParse(parts[2], out int patch))
            {
                (major, minor, patch) = release switch
                {
                    "patch" => (major, minor, patch + 1),
                    "minor" => (major, minor + 1, 0),
                    "major" => (major + 1, 0, 0),
                    _ => (major, minor, patch)
                };
                return $"v{current} → v{major}.{minor}.{patch} ({release.ToUpperInvariant()})";
            }
        }
        catch
        {
            // Display fallback only; launch-time project script remains authoritative.
        }

        return release.ToUpperInvariant() + " (project script determines exact next version at launch)";
    }

    private void AddReviewLine(string label, string value, bool monospace = false)
    {
        TextBlock line = new()
        {
            Text = $"{label}: {value}",
            Margin = new Thickness(0, 6, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        if (monospace)
            line.FontFamily = new FontFamily("Consolas");
        _reviewBody.Children.Add(line);
    }

    private static TextBlock Label(string text, double topMargin) => new()
    {
        Text = text,
        Margin = new Thickness(0, topMargin, 0, 4),
        FontWeight = FontWeights.SemiBold
    };

    private Border WrapCard(UIElement content) => new()
    {
        Background = FindBrush("AppSurfaceBrush", Brushes.DarkSlateGray),
        BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray),
        BorderThickness = new Thickness(1),
        Padding = new Thickness(18),
        Child = content
    };

    private void SetStatus(string text, bool warning = false)
    {
        _statusText.Text = text;
        _statusText.Foreground = warning
            ? FindBrush("AppWarningBorderBrush", Brushes.Goldenrod)
            : FindBrush("AppMutedTextBrush", Brushes.LightGray);
    }

    private static Brush FindBrush(string key, Brush fallback)
        => Application.Current.TryFindResource(key) as Brush ?? fallback;
}
