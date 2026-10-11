using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
    private readonly TextBlock _statusText;
    private readonly TextBox _previewText;

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
            Text = "Select only the evidence you want to inspect. ZDeployPet reads trusted report artifacts and bounded in-memory consoles, sanitizes them, and shows the exact preview. Nothing is exported in this PDA-7 slice.",
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
            Text = "PREVIEW ONLY — no bundle has been exported."
        };
        Grid.SetRow(_previewText, 3);
        root.Children.Add(_previewText);

        StackPanel actions = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };
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
        _buildPreviewButton.IsEnabled = false;
        _statusText.Text = "READING… collecting selected trusted evidence.";
        _previewText.Text = "PREVIEW ONLY — collecting and sanitizing selected evidence…";

        try
        {
            IncidentEvidenceSelection selection = new(
                IncludeDeploymentJson: _jsonCheck.IsChecked == true,
                IncludeDeploymentSummary: _summaryCheck.IsChecked == true,
                IncludeDeploymentFullLog: _fullLogCheck.IsChecked == true,
                IncludeDryRunConsole: _dryRunCheck.IsChecked == true,
                IncludeLiveDeployConsole: _liveDeployCheck.IsChecked == true,
                IncludeZDeployPetConsole: _zdeployPetCheck.IsChecked == true);

            IncidentEvidenceCollectionResult collection = await _collector.CollectAsync(
                _profile,
                _report,
                _canonicalReportRoot,
                selection,
                ShellRuntime.Activity.Snapshot());

            if (!collection.Success)
            {
                _statusText.Text = "BLOCKED — one or more selected evidence items could not be collected safely.";
                _statusText.Foreground = FindBrush("AppAccentBrush", Brushes.IndianRed);
                _previewText.Text = BuildErrorPreview(collection.Errors, selection);
                return;
            }

            IncidentBundleBuildResult bundle = IncidentEvidenceSanitizer.Build(collection.Evidence);
            if (!bundle.Success)
            {
                _statusText.Text = "BLOCKED — evidence sanitization/bounds validation failed.";
                _statusText.Foreground = FindBrush("AppAccentBrush", Brushes.IndianRed);
                _previewText.Text = BuildErrorPreview(bundle.Errors, selection);
                return;
            }

            int redacted = bundle.Evidence.Count(item => item.RedactionApplied);
            int truncated = bundle.Evidence.Count(item => item.Truncated);
            _statusText.Text = $"READY — {bundle.Evidence.Count} included item(s), {redacted} redacted, {truncated} truncated, {bundle.TotalCharacters:N0} characters.";
            _statusText.Foreground = FindBrush("AppSuccessBrush", Brushes.LightGreen);
            _previewText.Text = BuildPreviewText(bundle, selection);
        }
        catch (Exception exception)
        {
            _statusText.Text = "ERROR — incident preview could not be built.";
            _statusText.Foreground = FindBrush("AppAccentBrush", Brushes.IndianRed);
            _previewText.Text = "PREVIEW ONLY\n\nERROR\n" + exception.Message;
        }
        finally
        {
            _buildPreviewButton.IsEnabled = true;
        }
    }

    private string BuildPreviewText(IncidentBundleBuildResult bundle, IncidentEvidenceSelection selection)
    {
        StringBuilder output = new();
        output.AppendLine("ZDEPLOYPET SANITIZED INCIDENT BUNDLE PREVIEW");
        output.AppendLine("PREVIEW ONLY — NOTHING HAS BEEN EXPORTED");
        output.AppendLine();
        output.AppendLine($"Profile: {_profile.Name}");
        output.AppendLine($"Deployment ID: {_report.DeploymentId}");
        output.AppendLine($"Reporter outcome: {_report.Outcome}");
        output.AppendLine($"Release / mode / target: {_report.Release} / {_report.Mode} / {_report.Target}");
        output.AppendLine($"Included items: {bundle.Evidence.Count}");
        output.AppendLine($"Sanitized characters: {bundle.TotalCharacters}");
        output.AppendLine();
        output.AppendLine("OMITTED BY OPERATOR");
        foreach (string omitted in GetOmittedLabels(selection))
            output.AppendLine("- " + omitted);
        if (GetOmittedLabels(selection).Count == 0)
            output.AppendLine("- none");
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
        output.AppendLine("END OF PREVIEW — no export action exists in this build.");
        return output.ToString();
    }

    private string BuildErrorPreview(IReadOnlyList<string> errors, IncidentEvidenceSelection selection)
    {
        StringBuilder output = new();
        output.AppendLine("ZDEPLOYPET SANITIZED INCIDENT BUNDLE PREVIEW");
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
