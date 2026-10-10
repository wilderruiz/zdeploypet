using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using ZDeployPet.Core;
using ZDeployPet.Infrastructure;

namespace ZDeployPet.App;

public sealed class LiveDeploymentReviewWindow : Window
{
    private const string AllTargetsId = "all";

    private readonly DeploymentProfile _profile;
    private readonly DeploymentSessionController _sessionController;
    private readonly Func<bool> _targetsReady;
    private readonly DeploymentExecutionGate _executionGate;
    private readonly DeploymentScriptApprovalStore _approvalStore = new();

    private readonly ComboBox _targetCombo = new();
    private readonly ComboBox _releaseCombo = new();
    private readonly Button _createReviewButton = new();
    private readonly Border _reviewCard = new();
    private readonly StackPanel _reviewBody = new();
    private readonly TextBox _confirmationBox = new();
    private readonly Button _validateButton = new();
    private readonly TextBlock _statusText = new();

    private LiveDeploymentReviewSnapshot? _snapshot;
    private string? _currentSha256;

    private sealed record TargetOption(string Id, string Label, int PromptChoice);
    private sealed record ReleaseOption(string Token, string Label);

    public LiveDeploymentReviewWindow(
        DeploymentProfile profile,
        DeploymentSessionController sessionController,
        Func<bool> targetsReady,
        DeploymentExecutionGate executionGate)
    {
        _profile = profile;
        _sessionController = sessionController;
        _targetsReady = targetsReady;
        _executionGate = executionGate;

        Title = "Live deployment review — ZDeployPet";
        Width = 760;
        Height = 760;
        MinWidth = 650;
        MinHeight = 580;
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
            Text = "PDA-6 review only. This window cannot execute a live deployment yet.",
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
            new("patch", "PATCH"),
            new("minor", "MINOR"),
            new("major", "MAJOR")
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
            Text = "Safety: creating a review performs no deployment and sends no confirmation phrase. Any later change to the target, release, script fingerprint, configured confirmation phrase, deployment-access state, or execution state invalidates confirmation.",
            Margin = new Thickness(0, 12, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        });

        RefreshReleaseAvailability();
        return WrapCard(body);
    }

    private async Task CreateReviewAsync()
    {
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
        RenderSnapshot(_snapshot, target);
        SetStatus("REVIEW CREATED — immutable snapshot captured. Live execution remains unavailable in this PDA-6 slice.");
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
        AutomationProperties.SetName(_confirmationBox, "Exact live deployment confirmation phrase");
        _reviewBody.Children.Add(_confirmationBox);

        _validateButton.Content = "Validate typed confirmation";
        _validateButton.Padding = new Thickness(16, 9, 16, 9);
        _validateButton.Margin = new Thickness(0, 12, 0, 0);
        _validateButton.HorizontalAlignment = HorizontalAlignment.Left;
        _validateButton.Click -= ValidateConfirmation_Click;
        _validateButton.Click += ValidateConfirmation_Click;
        _reviewBody.Children.Add(_validateButton);

        _reviewBody.Children.Add(new TextBlock
        {
            Text = "This button validates only. It cannot start a live deployment. ZDeployPet never fills this field for you.",
            Margin = new Thickness(0, 10, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        });

        _reviewCard.Visibility = Visibility.Visible;
    }

    private async void ValidateConfirmation_Click(object sender, RoutedEventArgs e)
    {
        if (_snapshot is null) return;

        string currentSha;
        try
        {
            string scriptPath = Path.GetFullPath(Path.Combine(_profile.WindowsProjectPath, _profile.ScriptRelativePath));
            currentSha = LocalCapabilityDiscovery.ComputeSha256(scriptPath);
        }
        catch (Exception exception)
        {
            SetStatus("CONFIRMATION INVALID — script fingerprint could not be revalidated: " + exception.Message, true);
            return;
        }

        bool accessReady = _targetsReady() && await _sessionController.GetReadyRuntimeAsync(_profile) is not null;
        LiveDeploymentConfirmationResult result = LiveDeploymentReviewGuard.ValidateConfirmation(
            _snapshot,
            _confirmationBox.Text,
            new LiveDeploymentConfirmationContext(
                ProfileId: _profile.Id,
                ScriptRelativePath: _profile.ScriptRelativePath,
                CurrentScriptSha256: currentSha,
                TargetId: _snapshot.TargetId,
                Release: _snapshot.Release,
                LiveConfirmationPhrase: _profile.LiveConfirmationPhrase,
                DeploymentAccessReady: accessReady,
                ExecutionAlreadyRunning: _executionGate.IsRunning));

        if (result.IsConfirmed)
        {
            SetStatus("CONFIRMATION VALID — exact human-entered phrase matches this unchanged review. Live execution is intentionally not enabled yet.");
        }
        else
        {
            SetStatus("CONFIRMATION INVALID — " + string.Join(" ", result.Errors), true);
        }
    }

    private void InvalidateReview(string reason)
    {
        if (_snapshot is null) return;
        _snapshot = null;
        _confirmationBox.Text = string.Empty;
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
