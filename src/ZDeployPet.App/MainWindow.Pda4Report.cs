using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZDeployPet.Core;
using ZDeployPet.WslBridge;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private readonly WslDeploymentReportReader _deploymentReportReader = new();
    private bool _pda4ReportInitialized;
    private Border? _latestReportCard;
    private TextBlock? _latestReportStateText;
    private TextBlock? _latestReportDetailsText;
    private Button? _latestReportRefreshButton;
    private int _latestReportRefreshGeneration;

    private void InitializePda4ReportMonitor()
    {
        if (_pda4ReportInitialized) return;
        _pda4ReportInitialized = true;

        if (DiscoveryPanel.RowDefinitions.Count < 2) return;

        DiscoveryPanel.RowDefinitions.Insert(1, new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(ResultsTextBox, 2);

        _latestReportCard = BuildLatestReportCard();
        Grid.SetRow(_latestReportCard, 1);
        DiscoveryPanel.Children.Add(_latestReportCard);

        DiscoveryPanel.IsVisibleChanged += (_, _) =>
        {
            if (DiscoveryPanel.Visibility == Visibility.Visible)
                _ = RefreshLatestDeploymentReportAsync();
        };

        _ = RefreshLatestDeploymentReportAsync();
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
            Grid.ColumnSpan = 2,
            Margin = new Thickness(0, 10, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray),
            Text = "No report has been loaded yet."
        };
        Grid.SetRow(_latestReportDetailsText, 1);
        Grid.SetColumnSpan(_latestReportDetailsText, 2);
        grid.Children.Add(_latestReportDetailsText);

        card.Child = grid;

        HelpTipFactory.AttachToButton(
            _latestReportRefreshButton,
            new HelpTipSpec(
                "Refresh the latest authoritative deployment report.",
                "ZDeployPet reads only latest.json from the configured WSL report root, validates the reporter schema and checks that all declared artifact paths remain inside that report root before showing the result.",
                WhenToUse: "Use this after a deployment attempt or whenever you want to re-read the latest reporter truth.",
                WhatItDoes: "The card shows reporter result, release, mode, target and timestamps. Reporter result is authoritative; process exit status or UI state cannot upgrade a failed/cancelled report to success.",
                Safety: "This action is read-only. It does not modify the report directory and it does not deploy anything."));

        return card;
    }

    private async Task RefreshLatestDeploymentReportAsync()
    {
        int generation = ++_latestReportRefreshGeneration;

        if (_latestReportStateText is null || _latestReportDetailsText is null || _latestReportRefreshButton is null)
            return;

        DeploymentProfile? profile = _activeProfile;
        if (profile is null || DiscoveryPanel.Visibility != Visibility.Visible)
        {
            _latestReportCard!.Visibility = Visibility.Collapsed;
            return;
        }

        _latestReportCard!.Visibility = Visibility.Visible;

        if (string.IsNullOrWhiteSpace(profile.ReportRoot))
        {
            SetLatestReportCard(
                "NOT CONFIGURED",
                "This profile does not define a deployment report root.",
                FindBrush("AppMutedTextBrush", Brushes.LightGray));
            return;
        }

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

            Brush outcomeBrush = report.Outcome switch
            {
                DeploymentReportOutcome.Pass => Brushes.LightGreen,
                DeploymentReportOutcome.Cancelled => FindBrush("AppWarningBorderBrush", Brushes.Goldenrod),
                _ => FindBrush("AppAccentBrush", Brushes.IndianRed)
            };

            string details =
                $"Release {report.Release}  •  {report.Mode.ToUpperInvariant()}  •  Target {report.Target}  •  " +
                $"Started {report.StartedAt:yyyy-MM-dd HH:mm:ss zzz}  •  Finished {report.FinishedAt:yyyy-MM-dd HH:mm:ss zzz}  •  " +
                $"{report.DurationSeconds}s  •  ID {report.DeploymentId}";

            SetLatestReportCard(report.Outcome.ToString().ToUpperInvariant(), details, outcomeBrush);

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

    private static bool ArtifactsStayInsideRoot(string canonicalRoot, DeploymentReportArtifacts artifacts)
        => DeploymentReportPathBoundary.IsWithinCanonicalRoot(canonicalRoot, artifacts.FullLog)
           && DeploymentReportPathBoundary.IsWithinCanonicalRoot(canonicalRoot, artifacts.Summary)
           && DeploymentReportPathBoundary.IsWithinCanonicalRoot(canonicalRoot, artifacts.Json);

    private void SetLatestReportCard(string state, string details, Brush stateBrush)
    {
        if (_latestReportStateText is null || _latestReportDetailsText is null) return;
        _latestReportStateText.Text = state;
        _latestReportStateText.Foreground = stateBrush;
        _latestReportDetailsText.Text = details;
    }
}
