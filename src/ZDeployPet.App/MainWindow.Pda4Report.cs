using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ZDeployPet.Core;
using ZDeployPet.WslBridge;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private const int MaximumHistoryRows = 30;
    private static readonly TimeSpan DeploymentReportRefreshInterval = TimeSpan.FromSeconds(15);

    private readonly WslDeploymentReportReader _deploymentReportReader = new();
    private bool _pda4ReportInitialized;
    private Border? _latestReportCard;
    private TextBlock? _latestReportStateText;
    private TextBlock? _latestReportDetailsText;
    private Button? _latestReportRefreshButton;
    private Button? _latestReportSummaryButton;
    private Button? _latestReportFullLogButton;
    private Border? _deploymentHistoryCard;
    private TextBlock? _deploymentHistoryStatusText;
    private StackPanel? _deploymentHistoryRowsPanel;
    private DispatcherTimer? _deploymentReportRefreshTimer;
    private int _latestReportRefreshGeneration;
    private int _historyRefreshGeneration;
    private string? _latestReportCanonicalRoot;
    private DeploymentReport? _latestDeploymentReport;

    private void InitializePda4ReportMonitor()
    {
        if (_pda4ReportInitialized) return;
        _pda4ReportInitialized = true;

        if (DiscoveryPanel.RowDefinitions.Count < 2) return;

        DiscoveryPanel.RowDefinitions.Insert(1, new RowDefinition { Height = GridLength.Auto });
        DiscoveryPanel.RowDefinitions.Insert(2, new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(ResultsTextBox, 3);

        _latestReportCard = BuildLatestReportCard();
        Grid.SetRow(_latestReportCard, 1);
        DiscoveryPanel.Children.Add(_latestReportCard);

        _deploymentHistoryCard = BuildDeploymentHistoryCard();
        Grid.SetRow(_deploymentHistoryCard, 2);
        DiscoveryPanel.Children.Add(_deploymentHistoryCard);

        _deploymentReportRefreshTimer = new DispatcherTimer
        {
            Interval = DeploymentReportRefreshInterval
        };
        _deploymentReportRefreshTimer.Tick += async (_, _) => await RefreshReportsFromTimerAsync();

        DiscoveryPanel.IsVisibleChanged += (_, _) =>
        {
            if (DiscoveryPanel.Visibility == Visibility.Visible)
            {
                _deploymentReportRefreshTimer.Start();
                _ = RefreshLatestDeploymentReportAsync();
            }
            else
            {
                _deploymentReportRefreshTimer.Stop();
            }
        };

        Closed += (_, _) => _deploymentReportRefreshTimer?.Stop();

        if (DiscoveryPanel.Visibility == Visibility.Visible)
            _deploymentReportRefreshTimer.Start();

        _ = RefreshLatestDeploymentReportAsync();
    }

    private async Task RefreshReportsFromTimerAsync()
    {
        DispatcherTimer? timer = _deploymentReportRefreshTimer;
        if (timer is null || DiscoveryPanel.Visibility != Visibility.Visible)
            return;

        DeploymentProfile? profile = _activeProfile;
        if (profile is null || string.IsNullOrWhiteSpace(profile.ReportRoot))
            return;

        timer.Stop();
        try
        {
            await RefreshLatestDeploymentReportAsync();
        }
        finally
        {
            if (DiscoveryPanel.Visibility == Visibility.Visible)
                timer.Start();
        }
    }

    private Border BuildLatestReportCard()
    {
        Border card = new()
        {
            Margin = new Thickness(0, 0, 0, 12),
            Padding = new Thickness(14, 12, 14, 12),
            Background = FindBrush("AppSurfaceBrush", Brushes.DimGray),
            BorderBrush = FindBrush("AppBorderBrush", Brushes.Gray),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4)
        };

        Grid grid = new();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        StackPanel heading = new() { Orientation = Orientation.Vertical };
        heading.Children.Add(new TextBlock
        {
            Text = "Latest deployment report",
            FontWeight = FontWeights.SemiBold,
            FontSize = 15,
            Foreground = FindBrush("AppTextBrush", Brushes.White)
        });
        _latestReportStateText = new TextBlock
        {
            Text = "WAITING",
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 3, 0, 0),
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        };
        heading.Children.Add(_latestReportStateText);
        Grid.SetColumn(heading, 0);
        grid.Children.Add(heading);

        _latestReportRefreshButton = new Button
        {
            Content = "Refresh",
            Padding = new Thickness(12, 6, 12, 6),
            VerticalAlignment = VerticalAlignment.Top
        };
        _latestReportRefreshButton.Click += async (_, _) => await RefreshLatestDeploymentReportAsync();
        Grid.SetColumn(_latestReportRefreshButton, 1);
        grid.Children.Add(_latestReportRefreshButton);

        _latestReportDetailsText = new TextBlock
        {
            Margin = new Thickness(0, 10, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray),
            Text = "No report has been loaded yet."
        };
        Grid.SetRow(_latestReportDetailsText, 1);
        Grid.SetColumnSpan(_latestReportDetailsText, 2);
        grid.Children.Add(_latestReportDetailsText);

        WrapPanel reportActions = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 10, 0, 0)
        };

        _latestReportSummaryButton = new Button
        {
            Content = "View summary",
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 10, 0),
            Visibility = Visibility.Collapsed
        };
        _latestReportSummaryButton.Click += async (_, _) => await OpenLatestReportArtifactAsync(summary: true);
        reportActions.Children.Add(_latestReportSummaryButton);

        _latestReportFullLogButton = new Button
        {
            Content = "View full log",
            Padding = new Thickness(12, 6, 12, 6),
            Visibility = Visibility.Collapsed
        };
        _latestReportFullLogButton.Click += async (_, _) => await OpenLatestReportArtifactAsync(summary: false);
        reportActions.Children.Add(_latestReportFullLogButton);

        Grid.SetRow(reportActions, 2);
        Grid.SetColumnSpan(reportActions, 2);
        grid.Children.Add(reportActions);

        card.Child = grid;

        HelpTipFactory.AttachToButton(
            _latestReportRefreshButton,
            new HelpTipSpec(
                "Refresh deployment reports.",
                "ZDeployPet reads latest.json and the bounded deployment history from the configured WSL report root, validates reporter schema and checks declared artifact paths before showing trusted results.",
                WhenToUse: "Use this after a deployment attempt or whenever you want to re-read reporter truth. While this surface is visible, ZDeployPet also performs the same read-only refresh every 15 seconds.",
                WhatItDoes: "Refreshes both the latest card and the read-only history table. Reporter result is authoritative; process exit status or UI state cannot upgrade a failed/cancelled report to success.",
                Safety: "This action and the visible-only timer are read-only. They do not modify the report directory and they do not deploy anything."));

        return card;
    }

    private Border BuildDeploymentHistoryCard()
    {
        Border card = new()
        {
            Margin = new Thickness(0, 0, 0, 12),
            Padding = new Thickness(14, 12, 14, 12),
            Background = FindBrush("AppSurfaceBrush", Brushes.DimGray),
            BorderBrush = FindBrush("AppBorderBrush", Brushes.Gray),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4)
        };

        StackPanel body = new();
        body.Children.Add(new TextBlock
        {
            Text = "Deployment history",
            FontWeight = FontWeights.SemiBold,
            FontSize = 15,
            Foreground = FindBrush("AppTextBrush", Brushes.White)
        });

        _deploymentHistoryStatusText = new TextBlock
        {
            Text = "WAITING",
            Margin = new Thickness(0, 3, 0, 10),
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        };
        body.Children.Add(_deploymentHistoryStatusText);

        StackPanel table = new();
        table.Children.Add(BuildHistoryHeaderRow());
        _deploymentHistoryRowsPanel = new StackPanel();
        table.Children.Add(_deploymentHistoryRowsPanel);

        ScrollViewer scroll = new()
        {
            Content = table,
            MaxHeight = 320,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        body.Children.Add(scroll);
        card.Child = body;
        return card;
    }

    private Grid BuildHistoryHeaderRow()
    {
        Grid row = CreateHistoryRowGrid();
        string[] labels = ["Started", "Release", "Mode", "Target", "Result", "Duration", "Summary", "Full log"];
        for (int index = 0; index < labels.Length; index++)
        {
            TextBlock text = new()
            {
                Text = labels[index],
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(4, 2, 4, 6),
                Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
            };
            Grid.SetColumn(text, index);
            row.Children.Add(text);
        }
        return row;
    }

    private static Grid CreateHistoryRowGrid()
    {
        Grid row = new() { MinWidth = 680 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(75) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        return row;
    }

    private async Task RefreshLatestDeploymentReportAsync()
    {
        int generation = ++_latestReportRefreshGeneration;

        if (_latestReportStateText is null || _latestReportDetailsText is null || _latestReportRefreshButton is null)
            return;

        ClearLatestReportArtifactState();

        DeploymentProfile? profile = _activeProfile;
        if (profile is null || DiscoveryPanel.Visibility != Visibility.Visible)
        {
            _latestReportCard!.Visibility = Visibility.Collapsed;
            if (_deploymentHistoryCard is not null) _deploymentHistoryCard.Visibility = Visibility.Collapsed;
            return;
        }

        _latestReportCard!.Visibility = Visibility.Visible;
        if (_deploymentHistoryCard is not null) _deploymentHistoryCard.Visibility = Visibility.Visible;

        if (string.IsNullOrWhiteSpace(profile.ReportRoot))
        {
            SetLatestReportCard(
                "NOT CONFIGURED",
                "This profile does not define a deployment report root.",
                FindBrush("AppMutedTextBrush", Brushes.LightGray));
            SetDeploymentHistoryStatus("NOT CONFIGURED — add a deployment report root to this profile.");
            ClearDeploymentHistoryRows();
            return;
        }

        _ = RefreshDeploymentHistoryAsync(profile);

        _latestReportRefreshButton.IsEnabled = false;
        SetLatestReportCard(
            "READING…",
            "Reading and validating latest.json from the configured WSL report root.",
            FindBrush("AppMutedTextBrush", Brushes.LightGray));

        try
        {
            WslDeploymentReportReadResult read = await _deploymentReportReader.ReadLatestAsync(
                profile.WslDistribution,
                profile.ReportRoot);

            if (generation != _latestReportRefreshGeneration) return;

            if (!read.Success || read.Json is null || read.CanonicalReportRoot is null)
            {
                SetLatestReportCard(
                    "UNAVAILABLE",
                    read.Error ?? "latest.json could not be read safely.",
                    FindBrush("AppWarningBorderBrush", Brushes.Goldenrod));
                return;
            }

            DeploymentReportParseResult parsed = DeploymentReportParser.Parse(read.Json);
            if (!parsed.Success || parsed.Report is null)
            {
                SetLatestReportCard(
                    "INVALID",
                    string.Join(" ", parsed.Errors),
                    FindBrush("AppAccentBrush", Brushes.IndianRed));
                return;
            }

            DeploymentReport report = parsed.Report;
            if (!ArtifactsStayInsideRoot(read.CanonicalReportRoot, report.Artifacts))
            {
                SetLatestReportCard(
                    "BLOCKED",
                    "The report declares an artifact path outside the configured report root. ZDeployPet will not trust this report.",
                    FindBrush("AppAccentBrush", Brushes.IndianRed));
                return;
            }

            string details =
                $"Release {report.Release}  •  {report.Mode.ToUpperInvariant()}  •  Target {report.Target}  •  " +
                $"Started {report.StartedAt:yyyy-MM-dd HH:mm:ss zzz}  •  Finished {report.FinishedAt:yyyy-MM-dd HH:mm:ss zzz}  •  " +
                $"{report.DurationSeconds}s  •  ID {report.DeploymentId}";

            _latestReportCanonicalRoot = read.CanonicalReportRoot;
            _latestDeploymentReport = report;
            SetLatestReportCard(report.Outcome.ToString().ToUpperInvariant(), details, OutcomeBrush(report.Outcome));
            SetLatestReportArtifactButtonsVisible(true);

            ShellRuntime.Activity.Add(
                ShellActivityLevel.Info,
                "Report",
                $"Latest deployment report loaded for '{profile.Name}': {report.Outcome}, release {report.Release}, mode {report.Mode}, target {report.Target}.");
        }
        catch (Exception exception)
        {
            if (generation != _latestReportRefreshGeneration) return;
            SetLatestReportCard(
                "ERROR",
                exception.Message,
                FindBrush("AppAccentBrush", Brushes.IndianRed));
            ShellRuntime.Activity.Add(ShellActivityLevel.Error, "Report", "Latest deployment report refresh failed.");
        }
        finally
        {
            if (generation == _latestReportRefreshGeneration)
                _latestReportRefreshButton.IsEnabled = true;
        }
    }

    private async Task RefreshDeploymentHistoryAsync(DeploymentProfile profile)
    {
        int generation = ++_historyRefreshGeneration;
        ClearDeploymentHistoryRows();
        SetDeploymentHistoryStatus("READING… Enumerating trusted historical report JSON.");

        try
        {
            WslDeploymentHistoryListResult list = await _deploymentReportReader.ListHistoryJsonAsync(
                profile.WslDistribution,
                profile.ReportRoot);

            if (generation != _historyRefreshGeneration) return;
            if (!list.Success || list.CanonicalReportRoot is null)
            {
                SetDeploymentHistoryStatus("UNAVAILABLE — " + (list.Error ?? "Deployment history could not be read safely."));
                return;
            }

            List<DeploymentReport> trusted = [];
            HashSet<string> deploymentIds = new(StringComparer.Ordinal);
            int rejected = 0;

            foreach (string jsonPath in list.JsonPaths)
            {
                if (trusted.Count >= MaximumHistoryRows) break;
                if (generation != _historyRefreshGeneration) return;

                WslDeploymentArtifactReadResult read = await _deploymentReportReader.ReadTextArtifactAsync(
                    profile.WslDistribution,
                    list.CanonicalReportRoot,
                    jsonPath,
                    WslDeploymentReportReader.MaximumReportBytes);

                if (!read.Success || read.Text is null)
                {
                    rejected++;
                    continue;
                }

                DeploymentReportParseResult parsed = DeploymentReportParser.Parse(read.Text);
                if (!parsed.Success || parsed.Report is null ||
                    !ArtifactsStayInsideRoot(list.CanonicalReportRoot, parsed.Report.Artifacts))
                {
                    rejected++;
                    continue;
                }

                if (!deploymentIds.Add(parsed.Report.DeploymentId))
                    continue;

                trusted.Add(parsed.Report);
            }

            trusted.Sort((left, right) => right.StartedAt.CompareTo(left.StartedAt));
            RenderDeploymentHistory(profile, list.CanonicalReportRoot, trusted);

            string status = trusted.Count == 0
                ? "No trusted historical deployment reports were found."
                : $"Showing {trusted.Count} trusted run{(trusted.Count == 1 ? string.Empty : "s")}, newest first.";
            if (rejected > 0)
                status += $" {rejected} malformed or untrusted candidate{(rejected == 1 ? string.Empty : "s")} skipped.";
            if (list.Truncated)
                status += $" History enumeration was bounded to the newest {WslDeploymentReportReader.MaximumHistoryCandidates} of {list.CandidateCount} JSON candidates.";
            SetDeploymentHistoryStatus(status);

            ShellRuntime.Activity.Add(
                ShellActivityLevel.Info,
                "Report",
                $"Deployment history loaded for '{profile.Name}': {trusted.Count} trusted rows, {rejected} rejected candidates.");
        }
        catch (Exception exception)
        {
            if (generation != _historyRefreshGeneration) return;
            SetDeploymentHistoryStatus("ERROR — " + exception.Message);
            ShellRuntime.Activity.Add(ShellActivityLevel.Error, "Report", "Deployment history refresh failed.");
        }
    }

    private void RenderDeploymentHistory(
        DeploymentProfile profile,
        string canonicalRoot,
        IReadOnlyList<DeploymentReport> reports)
    {
        if (_deploymentHistoryRowsPanel is null) return;
        _deploymentHistoryRowsPanel.Children.Clear();

        foreach (DeploymentReport report in reports)
        {
            Grid row = CreateHistoryRowGrid();
            row.Margin = new Thickness(0, 0, 0, 4);

            AddHistoryText(row, 0, report.StartedAt.ToString("yyyy-MM-dd HH:mm"));
            AddHistoryText(row, 1, report.Release);
            AddHistoryText(row, 2, report.Mode.ToUpperInvariant());
            AddHistoryText(row, 3, report.Target);
            AddHistoryText(row, 4, report.Outcome.ToString().ToUpperInvariant(), OutcomeBrush(report.Outcome), FontWeights.SemiBold);
            AddHistoryText(row, 5, $"{report.DurationSeconds}s");

            Button summaryButton = BuildHistoryActionButton("Summary");
            summaryButton.Click += async (_, _) => await OpenReportArtifactAsync(
                profile,
                canonicalRoot,
                report,
                summary: true,
                summaryButton);
            Grid.SetColumn(summaryButton, 6);
            row.Children.Add(summaryButton);

            Button fullLogButton = BuildHistoryActionButton("Full log");
            fullLogButton.Click += async (_, _) => await OpenReportArtifactAsync(
                profile,
                canonicalRoot,
                report,
                summary: false,
                fullLogButton);
            Grid.SetColumn(fullLogButton, 7);
            row.Children.Add(fullLogButton);

            _deploymentHistoryRowsPanel.Children.Add(row);
        }
    }

    private void AddHistoryText(
        Grid row,
        int column,
        string text,
        Brush? brush = null,
        FontWeight? weight = null)
    {
        TextBlock block = new()
        {
            Text = text,
            Margin = new Thickness(4, 6, 4, 6),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = brush ?? FindBrush("AppTextBrush", Brushes.White)
        };
        if (weight.HasValue) block.FontWeight = weight.Value;
        Grid.SetColumn(block, column);
        row.Children.Add(block);
    }

    private static Button BuildHistoryActionButton(string label)
        => new()
        {
            Content = label,
            Padding = new Thickness(7, 4, 7, 4),
            Margin = new Thickness(3, 2, 3, 2),
            VerticalAlignment = VerticalAlignment.Center
        };

    private async Task OpenLatestReportArtifactAsync(bool summary)
    {
        DeploymentProfile? profile = _activeProfile;
        DeploymentReport? report = _latestDeploymentReport;
        string? canonicalRoot = _latestReportCanonicalRoot;
        Button? button = summary ? _latestReportSummaryButton : _latestReportFullLogButton;

        if (profile is null || report is null || canonicalRoot is null || button is null)
            return;

        await OpenReportArtifactAsync(profile, canonicalRoot, report, summary, button);
    }

    private async Task OpenReportArtifactAsync(
        DeploymentProfile profile,
        string canonicalRoot,
        DeploymentReport report,
        bool summary,
        Button button)
    {
        string artifactPath = summary ? report.Artifacts.Summary : report.Artifacts.FullLog;
        int limit = summary
            ? WslDeploymentReportReader.MaximumSummaryBytes
            : WslDeploymentReportReader.MaximumFullLogBytes;
        string label = summary ? "Deployment summary" : "Deployment full log";

        button.IsEnabled = false;
        try
        {
            WslDeploymentArtifactReadResult read = await _deploymentReportReader.ReadTextArtifactAsync(
                profile.WslDistribution,
                canonicalRoot,
                artifactPath,
                limit);

            if (!read.Success || read.Text is null || read.CanonicalPath is null)
            {
                MessageBox.Show(
                    this,
                    read.Error ?? $"{label} could not be read safely.",
                    label,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            ReportArtifactWindow window = new(
                label,
                $"Read-only reporter artifact • {read.CanonicalPath}",
                read.Text)
            {
                Owner = this
            };
            window.ShowDialog();

            ShellRuntime.Activity.Add(
                ShellActivityLevel.Info,
                "Report",
                $"Opened read-only {label.ToLowerInvariant()} for deployment {report.DeploymentId}.");
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                label,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            ShellRuntime.Activity.Add(ShellActivityLevel.Error, "Report", $"Failed to open {label.ToLowerInvariant()}.");
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    private Brush OutcomeBrush(DeploymentReportOutcome outcome)
        => outcome switch
        {
            DeploymentReportOutcome.Pass => Brushes.LightGreen,
            DeploymentReportOutcome.Cancelled => FindBrush("AppWarningBorderBrush", Brushes.Goldenrod),
            _ => FindBrush("AppAccentBrush", Brushes.IndianRed)
        };

    private static bool ArtifactsStayInsideRoot(string canonicalRoot, DeploymentReportArtifacts artifacts)
        => DeploymentReportPathBoundary.IsWithinCanonicalRoot(canonicalRoot, artifacts.FullLog)
           && DeploymentReportPathBoundary.IsWithinCanonicalRoot(canonicalRoot, artifacts.Summary)
           && DeploymentReportPathBoundary.IsWithinCanonicalRoot(canonicalRoot, artifacts.Json);

    private void ClearLatestReportArtifactState()
    {
        _latestReportCanonicalRoot = null;
        _latestDeploymentReport = null;
        SetLatestReportArtifactButtonsVisible(false);
    }

    private void SetLatestReportArtifactButtonsVisible(bool visible)
    {
        Visibility visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (_latestReportSummaryButton is not null) _latestReportSummaryButton.Visibility = visibility;
        if (_latestReportFullLogButton is not null) _latestReportFullLogButton.Visibility = visibility;
    }

    private void ClearDeploymentHistoryRows()
    {
        _deploymentHistoryRowsPanel?.Children.Clear();
    }

    private void SetDeploymentHistoryStatus(string text)
    {
        if (_deploymentHistoryStatusText is not null)
            _deploymentHistoryStatusText.Text = text;
    }

    private void SetLatestReportCard(string state, string details, Brush stateBrush)
    {
        if (_latestReportStateText is null || _latestReportDetailsText is null) return;
        _latestReportStateText.Text = state;
        _latestReportStateText.Foreground = stateBrush;
        _latestReportDetailsText.Text = details;
    }
}
