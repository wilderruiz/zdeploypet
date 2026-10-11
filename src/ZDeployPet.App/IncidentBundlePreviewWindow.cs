using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using ZDeployPet.Core;

namespace ZDeployPet.App;

internal sealed class IncidentBundlePreviewWindow : Window
{
    private readonly DeploymentProfile _profile;
    private readonly DeploymentReport _report;
    private readonly string _canonicalReportRoot;
    private readonly IncidentEvidenceCollector _collector;

    private readonly CheckBox _jsonCheck;
    private readonly CheckBox _summaryCheck;
    private readonly CheckBox _fullLogCheck;
    private readonly CheckBox _dryRunCheck;
    private readonly CheckBox _liveDeployCheck;
    private readonly CheckBox _zdeployPetCheck;
    private readonly Button _buildPreviewButton;
    private readonly Button _copyAllButton;
    private readonly Button _exportButton;
    private readonly TextBlock _statusText;
    private readonly TextBox _previewText;

    private bool _hasBuiltSanitizedPreview;

    public IncidentBundlePreviewWindow(
        DeploymentProfile profile,
        DeploymentReport report,
        string canonicalReportRoot,
        IncidentEvidenceCollector collector)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _report = report ?? throw new ArgumentNullException(nameof(report));
        _canonicalReportRoot = canonicalReportRoot ?? throw new ArgumentNullException(nameof(canonicalReportRoot));
        _collector = collector ?? throw new ArgumentNullException(nameof(collector));

        Title = "Incident bundle preview — ZDeployPet";
        Width = 1080;
        Height = 820;
        MinWidth = 760;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        Grid root = new() { Margin = new Thickness(20) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        StackPanel heading = new();
        heading.Children.Add(new TextBlock
        {
            Text = "Sanitized incident bundle preview",
            FontSize = 28,
            FontWeight = FontWeights.SemiBold,
            Foreground = FindBrush("AppTextBrush", Brushes.White)
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Select only the evidence you want to inspect. ZDeployPet reads trusted report artifacts and bounded in-memory consoles, sanitizes them, adds a no-authority coding-agent handoff manifest, and shows the exact content before any local export.",
            Margin = new Thickness(0, 6, 0, 14),
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        });
        root.Children.Add(heading);

        Border selectionCard = BuildCard();
        Grid selectionGrid = new();
        selectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        selectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        selectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        selectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        selectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        selectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        TextBlock selectionTitle = new()
        {
            Text = "1. Evidence selection",
            FontSize = 19,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10),
            Foreground = FindBrush("AppTextBrush", Brushes.White)
        };
        Grid.SetColumnSpan(selectionTitle, 2);
        selectionGrid.Children.Add(selectionTitle);

        _jsonCheck = BuildCheck("Deployment JSON", true);
        _summaryCheck = BuildCheck("Deployment summary", true);
        _fullLogCheck = BuildCheck("Deployment full-log excerpt", false);
        _dryRunCheck = BuildCheck("Dry run console", true);
        _liveDeployCheck = BuildCheck("Live deploy console", true);
        _zdeployPetCheck = BuildCheck("ZDeployPet console", true);

        AddSelection(selectionGrid, _jsonCheck, 1, 0);
        AddSelection(selectionGrid, _summaryCheck, 1, 1);
        AddSelection(selectionGrid, _fullLogCheck, 2, 0);
        AddSelection(selectionGrid, _dryRunCheck, 2, 1);
        AddSelection(selectionGrid, _liveDeployCheck, 3, 0);
        AddSelection(selectionGrid, _zdeployPetCheck, 3, 1);

        foreach (CheckBox checkBox in new[] { _jsonCheck, _summaryCheck, _fullLogCheck, _dryRunCheck, _liveDeployCheck, _zdeployPetCheck })
        {
            checkBox.Checked += EvidenceSelectionChanged;
            checkBox.Unchecked += EvidenceSelectionChanged;
        }

        selectionCard.Child = selectionGrid;
        Grid.SetRow(selectionCard, 1);
        root.Children.Add(selectionCard);

        StackPanel previewHeader = new()
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 14, 0, 10)
        };
        _buildPreviewButton = new Button
        {
            Content = "Build sanitized preview",
            Padding = new Thickness(16, 8, 16, 8),
            Margin = new Thickness(0, 0, 12, 0)
        };
        _buildPreviewButton.Click += async (_, _) => await BuildPreviewAsync();
        previewHeader.Children.Add(_buildPreviewButton);

        _statusText = new TextBlock
        {
            Text = "Preview not built yet.",
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        };
        previewHeader.Children.Add(_statusText);
        Grid.SetRow(previewHeader, 2);
        root.Children.Add(previewHeader);

        _previewText = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = TextWrapping.NoWrap,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12.5,
            Padding = new Thickness(12),
            Background = FindBrush("AppBackgroundBrush", Brushes.Black),
            Foreground = FindBrush("AppTextBrush", Brushes.White),
            BorderBrush = FindBrush("AppBorderBrush", Brushes.Gray),
            BorderThickness = new Thickness(1),
            Text = "PREVIEW NOT BUILT — no bundle has been exported."
        };
        Grid.SetRow(_previewText, 3);
        root.Children.Add(_previewText);

        StackPanel actions = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };
        _copyAllButton = new Button
        {
            Content = "Copy all",
            Padding = new Thickness(16, 8, 16, 8),
            Margin = new Thickness(0, 0, 10, 0),
            IsEnabled = false
        };
        _copyAllButton.Click += CopyAll_Click;
        actions.Children.Add(_copyAllButton);

        _exportButton = new Button
        {
            Content = "Export sanitized bundle…",
            Padding = new Thickness(16, 8, 16, 8),
            Margin = new Thickness(0, 0, 10, 0),
            IsEnabled = false
        };
        _exportButton.Click += Export_Click;
        actions.Children.Add(_exportButton);

        Button close = new()
        {
            Content = "Close",
            Padding = new Thickness(16, 8, 16, 8)
        };
        close.Click += (_, _) => Close();
        actions.Children.Add(close);
        Grid.SetRow(actions, 4);
        root.Children.Add(actions);

        Content = root;
        Loaded += (_, _) => ThemeRuntime.Apply(this);
    }

    private async Task BuildPreviewAsync()
    {
        SetBuiltPreviewState(false);
        _buildPreviewButton.IsEnabled = false;
        _statusText.Text = "READING… collecting selected trusted evidence.";
        _previewText.Text = "Collecting and sanitizing selected evidence…";

        try
        {
            IncidentEvidenceSelection selection = CurrentSelection();
            IncidentEvidenceCollectionResult collection = await _collector.CollectAsync(
                _profile,
                _report,
                _canonicalReportRoot,
                selection,
                ShellRuntime.Activity.Snapshot());

            if (!collection.Success)
            {
                BlockPreview("BLOCKED — one or more selected evidence items could not be collected safely.", collection.Errors, selection);
                return;
            }

            IncidentBundleBuildResult bundle = IncidentEvidenceSanitizer.Build(collection.Evidence);
            if (!bundle.Success)
            {
                BlockPreview("BLOCKED — evidence sanitization/bounds validation failed.", bundle.Errors, selection);
                return;
            }

            IReadOnlyList<string> omitted = GetOmittedLabels(selection);
            string repositoryLabel = Path.GetFileName(Path.TrimEndingDirectorySeparator(_profile.WindowsProjectPath));
            if (string.IsNullOrWhiteSpace(repositoryLabel))
                repositoryLabel = "project";

            IncidentAgentHandoffManifestBuildResult manifest = IncidentAgentHandoffManifest.Build(new(
                RepositoryLabel: repositoryLabel,
                ProfileName: _profile.Name,
                DeploymentId: _report.DeploymentId,
                ReporterOutcome: _report.Outcome.ToString(),
                Release: _report.Release,
                Mode: _report.Mode,
                Target: _report.Target,
                IncludedEvidenceLabels: bundle.Evidence.Select(item => item.Label).ToArray(),
                OmittedEvidenceLabels: omitted));

            if (!manifest.Success)
            {
                BlockPreview("BLOCKED — coding-agent handoff manifest validation failed.", manifest.Errors, selection);
                return;
            }

            int redacted = bundle.Evidence.Count(item => item.RedactionApplied);
            int truncated = bundle.Evidence.Count(item => item.Truncated);
            _statusText.Text = $"READY — {bundle.Evidence.Count} included item(s), agent manifest included, {redacted} redacted, {truncated} truncated, {bundle.TotalCharacters:N0} evidence characters.";
            _statusText.Foreground = FindBrush("AppSuccessBrush", Brushes.LightGreen);
            _previewText.Text = BuildBundleText(bundle, manifest.Manifest, omitted);
            SetBuiltPreviewState(true);
        }
        catch (Exception exception)
        {
            _statusText.Text = "ERROR — incident preview could not be built.";
            _statusText.Foreground = FindBrush("AppAccentBrush", Brushes.IndianRed);
            _previewText.Text = "PREVIEW BLOCKED\n\nERROR\n" + exception.Message;
        }
        finally
        {
            _buildPreviewButton.IsEnabled = true;
        }
    }

    private void BlockPreview(string status, IReadOnlyList<string> errors, IncidentEvidenceSelection selection)
    {
        _statusText.Text = status;
        _statusText.Foreground = FindBrush("AppAccentBrush", Brushes.IndianRed);
        _previewText.Text = BuildErrorPreview(errors, selection);
    }

    private void CopyAll_Click(object sender, RoutedEventArgs e)
    {
        if (!_hasBuiltSanitizedPreview || string.IsNullOrWhiteSpace(_previewText.Text))
            return;

        try
        {
            Clipboard.SetText(_previewText.Text);
            _statusText.Text = "COPIED — sanitized bundle + agent manifest copied to the Windows clipboard.";
            _statusText.Foreground = FindBrush("AppSuccessBrush", Brushes.LightGreen);
        }
        catch (Exception exception)
        {
            _statusText.Text = "ERROR — sanitized bundle content could not be copied to the clipboard.";
            _statusText.Foreground = FindBrush("AppAccentBrush", Brushes.IndianRed);
            MessageBox.Show(this, exception.Message, "Copy sanitized bundle", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (!_hasBuiltSanitizedPreview || string.IsNullOrWhiteSpace(_previewText.Text))
            return;

        SaveFileDialog dialog = new()
        {
            Title = "Export sanitized ZDeployPet incident bundle",
            Filter = "Text file (*.txt)|*.txt",
            DefaultExt = IncidentBundleExportPolicy.RequiredExtension,
            AddExtension = true,
            OverwritePrompt = true,
            FileName = IncidentBundleExportPolicy.BuildSuggestedFileName(_report.DeploymentId)
        };

        if (dialog.ShowDialog(this) != true)
            return;

        IncidentBundleExportValidationResult validation = IncidentBundleExportPolicy.ValidateDestination(
            dialog.FileName,
            _profile.WindowsProjectPath);

        if (!validation.Success || string.IsNullOrWhiteSpace(validation.CanonicalPath))
        {
            _statusText.Text = "BLOCKED — unsafe incident-bundle export destination.";
            _statusText.Foreground = FindBrush("AppAccentBrush", Brushes.IndianRed);
            MessageBox.Show(
                this,
                string.Join(Environment.NewLine, validation.Errors),
                "Incident bundle export blocked",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        string destination = validation.CanonicalPath;
        string? directory = Path.GetDirectoryName(destination);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            _statusText.Text = "BLOCKED — export directory does not exist.";
            _statusText.Foreground = FindBrush("AppAccentBrush", Brushes.IndianRed);
            return;
        }

        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(destination)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, _previewText.Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporaryPath, destination, overwrite: true);

            _statusText.Text = $"EXPORTED — sanitized bundle + agent manifest saved to {destination}";
            _statusText.Foreground = FindBrush("AppSuccessBrush", Brushes.LightGreen);
            ShellRuntime.Activity.Add(
                ShellActivityLevel.Info,
                "Incident",
                $"Exported sanitized incident bundle with no-authority agent handoff manifest for deployment {_report.DeploymentId} to a local operator-selected path.");
        }
        catch (Exception exception)
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch
            {
                // Best-effort cleanup only; never mask the original export failure.
            }

            _statusText.Text = "ERROR — sanitized incident bundle could not be exported.";
            _statusText.Foreground = FindBrush("AppAccentBrush", Brushes.IndianRed);
            MessageBox.Show(this, exception.Message, "Incident bundle export", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EvidenceSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (!_hasBuiltSanitizedPreview)
            return;

        SetBuiltPreviewState(false);
        _statusText.Text = "SELECTION CHANGED — rebuild the sanitized preview before copying or exporting.";
        _statusText.Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray);
    }

    private void SetBuiltPreviewState(bool ready)
    {
        _hasBuiltSanitizedPreview = ready;
        _copyAllButton.IsEnabled = ready;
        _exportButton.IsEnabled = ready;
    }

    private IncidentEvidenceSelection CurrentSelection()
        => new(
            IncludeDeploymentJson: _jsonCheck.IsChecked == true,
            IncludeDeploymentSummary: _summaryCheck.IsChecked == true,
            IncludeDeploymentFullLog: _fullLogCheck.IsChecked == true,
            IncludeDryRunConsole: _dryRunCheck.IsChecked == true,
            IncludeLiveDeployConsole: _liveDeployCheck.IsChecked == true,
            IncludeZDeployPetConsole: _zdeployPetCheck.IsChecked == true);

    private string BuildBundleText(
        IncidentBundleBuildResult bundle,
        string agentManifest,
        IReadOnlyList<string> omitted)
    {
        StringBuilder output = new();
        output.AppendLine("ZDEPLOYPET SANITIZED INCIDENT BUNDLE");
        output.AppendLine("DIAGNOSTIC EVIDENCE ONLY — DOES NOT GRANT DEPLOYMENT AUTHORITY");
        output.AppendLine();
        output.AppendLine(agentManifest.TrimEnd());
        output.AppendLine();
        output.AppendLine("BUNDLE SUMMARY");
        output.AppendLine($"Profile: {_profile.Name}");
        output.AppendLine($"Deployment ID: {_report.DeploymentId}");
        output.AppendLine($"Reporter outcome: {_report.Outcome}");
        output.AppendLine($"Release / mode / target: {_report.Release} / {_report.Mode} / {_report.Target}");
        output.AppendLine($"Included items: {bundle.Evidence.Count}");
        output.AppendLine($"Sanitized evidence characters: {bundle.TotalCharacters}");
        output.AppendLine();
        output.AppendLine("OMITTED BY OPERATOR");
        if (omitted.Count == 0) output.AppendLine("- none");
        else foreach (string label in omitted) output.AppendLine("- " + label);
        output.AppendLine();

        for (int index = 0; index < bundle.Evidence.Count; index++)
        {
            SanitizedIncidentEvidence item = bundle.Evidence[index];
            output.AppendLine(new string('=', 72));
            output.AppendLine($"ITEM {index + 1}: {item.Label}");
            output.AppendLine($"KIND: {item.Kind}");
            output.AppendLine($"REDACTION APPLIED: {(item.RedactionApplied ? "YES" : "NO")}");
            output.AppendLine($"TRUNCATED: {(item.Truncated ? "YES" : "NO")}");
            output.AppendLine(new string('-', 72));
            output.AppendLine(item.Content);
            if (!item.Content.EndsWith('\n')) output.AppendLine();
        }

        output.AppendLine(new string('=', 72));
        output.AppendLine("END OF SANITIZED INCIDENT BUNDLE");
        return output.ToString();
    }

    private string BuildErrorPreview(IReadOnlyList<string> errors, IncidentEvidenceSelection selection)
    {
        StringBuilder output = new();
        output.AppendLine("ZDEPLOYPET SANITIZED INCIDENT BUNDLE");
        output.AppendLine("PREVIEW BLOCKED — NOTHING HAS BEEN EXPORTED");
        output.AppendLine();
        foreach (string error in errors)
            output.AppendLine("- " + error);
        output.AppendLine();
        output.AppendLine("OMITTED BY OPERATOR");
        IReadOnlyList<string> omitted = GetOmittedLabels(selection);
        if (omitted.Count == 0) output.AppendLine("- none");
        else foreach (string label in omitted) output.AppendLine("- " + label);
        return output.ToString();
    }

    private static IReadOnlyList<string> GetOmittedLabels(IncidentEvidenceSelection selection)
    {
        List<string> omitted = [];
        if (!selection.IncludeDeploymentJson) omitted.Add("Deployment JSON");
        if (!selection.IncludeDeploymentSummary) omitted.Add("Deployment summary");
        if (!selection.IncludeDeploymentFullLog) omitted.Add("Deployment full-log excerpt");
        if (!selection.IncludeDryRunConsole) omitted.Add("Dry run console");
        if (!selection.IncludeLiveDeployConsole) omitted.Add("Live deploy console");
        if (!selection.IncludeZDeployPetConsole) omitted.Add("ZDeployPet console");
        return omitted;
    }

    private Border BuildCard()
        => new()
        {
            Padding = new Thickness(16),
            Background = FindBrush("AppSurfaceBrush", Brushes.DimGray),
            BorderBrush = FindBrush("AppBorderBrush", Brushes.Gray),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4)
        };

    private CheckBox BuildCheck(string label, bool isChecked)
        => new()
        {
            Content = label,
            IsChecked = isChecked,
            Margin = new Thickness(0, 5, 16, 5),
            Foreground = FindBrush("AppTextBrush", Brushes.White)
        };

    private static void AddSelection(Grid grid, CheckBox checkBox, int row, int column)
    {
        Grid.SetRow(checkBox, row);
        Grid.SetColumn(checkBox, column);
        grid.Children.Add(checkBox);
    }

    private static Brush FindBrush(string key, Brush fallback)
        => Application.Current.TryFindResource(key) as Brush ?? fallback;
}
