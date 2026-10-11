using System.Text;
using ZDeployPet.Core;
using ZDeployPet.WslBridge;

namespace ZDeployPet.App;

internal sealed record IncidentEvidenceSelection(
    bool IncludeDeploymentJson = true,
    bool IncludeDeploymentSummary = true,
    bool IncludeDeploymentFullLog = false,
    bool IncludeDryRunConsole = true,
    bool IncludeLiveDeployConsole = true,
    bool IncludeZDeployPetConsole = true);

internal sealed record IncidentEvidenceCollectionResult(
    bool Success,
    IReadOnlyList<IncidentEvidenceInput> Evidence,
    IReadOnlyList<string> Errors);

/// <summary>
/// PDA-7 read-only collector. It never accepts arbitrary filesystem paths from UI callers.
/// Reporter artifacts can only be selected from an already parsed/trusted DeploymentReport,
/// and every path is rechecked against the canonical reporter root before the WSL reader is used.
/// Console evidence comes only from the bounded in-memory ShellRuntime activity snapshot.
/// </summary>
internal sealed class IncidentEvidenceCollector
{
    private const string DryRunActivityCategory = "Dry run";
    private const string LiveDeployActivityCategory = "Live deploy";
    private const string RawDryScriptCategory = "deploy_millenova.sh";
    private const string RawLiveScriptCategory = "deploy_millenova.live.sh";
    private const int MaximumCollectedCharacters = IncidentBundleLimits.MaximumInputCharactersPerItem - 1024;

    private readonly WslDeploymentReportReader _reportReader;

    public IncidentEvidenceCollector(WslDeploymentReportReader reportReader)
    {
        _reportReader = reportReader ?? throw new ArgumentNullException(nameof(reportReader));
    }

    public async Task<IncidentEvidenceCollectionResult> CollectAsync(
        DeploymentProfile profile,
        DeploymentReport report,
        string canonicalReportRoot,
        IncidentEvidenceSelection selection,
        IReadOnlyList<ShellActivityEntry> activitySnapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(activitySnapshot);

        List<IncidentEvidenceInput> evidence = [];
        List<string> errors = [];

        if (!DeploymentReportPathBoundary.TryNormalizeCanonicalPosixPath(canonicalReportRoot, out string normalizedRoot))
            return new IncidentEvidenceCollectionResult(false, evidence, ["The canonical deployment-report root is invalid."]);

        if (!ArtifactsStayInsideRoot(normalizedRoot, report.Artifacts))
            return new IncidentEvidenceCollectionResult(false, evidence, ["The selected deployment report declares an artifact outside the trusted report root."]);

        if (selection.IncludeDeploymentJson)
            evidence.Add(new IncidentEvidenceInput(
                IncidentEvidenceKind.DeploymentJson,
                $"Deployment JSON — {report.DeploymentId}",
                BoundText(report.RawJson, preserveTail: false)));

        if (selection.IncludeDeploymentSummary)
        {
            IncidentEvidenceInput? summary = await ReadArtifactAsync(
                profile,
                normalizedRoot,
                report.Artifacts.Summary,
                WslDeploymentReportReader.MaximumSummaryBytes,
                IncidentEvidenceKind.DeploymentSummary,
                $"Deployment summary — {report.DeploymentId}",
                preserveTail: false,
                cancellationToken,
                errors);
            if (summary is not null) evidence.Add(summary);
        }

        if (selection.IncludeDeploymentFullLog)
        {
            IncidentEvidenceInput? fullLog = await ReadArtifactAsync(
                profile,
                normalizedRoot,
                report.Artifacts.FullLog,
                WslDeploymentReportReader.MaximumFullLogBytes,
                IncidentEvidenceKind.DeploymentFullLog,
                $"Deployment full-log excerpt — {report.DeploymentId}",
                preserveTail: true,
                cancellationToken,
                errors);
            if (fullLog is not null) evidence.Add(fullLog);
        }

        if (selection.IncludeDryRunConsole)
            AddConsoleEvidence(evidence, activitySnapshot, ConsoleChannel.DryRun);
        if (selection.IncludeLiveDeployConsole)
            AddConsoleEvidence(evidence, activitySnapshot, ConsoleChannel.LiveDeploy);
        if (selection.IncludeZDeployPetConsole)
            AddConsoleEvidence(evidence, activitySnapshot, ConsoleChannel.ZDeployPet);

        if (evidence.Count == 0 && errors.Count == 0)
            errors.Add("No incident evidence was selected.");

        return new IncidentEvidenceCollectionResult(errors.Count == 0, evidence, errors);
    }

    private async Task<IncidentEvidenceInput?> ReadArtifactAsync(
        DeploymentProfile profile,
        string canonicalRoot,
        string declaredPath,
        int maximumBytes,
        IncidentEvidenceKind kind,
        string label,
        bool preserveTail,
        CancellationToken cancellationToken,
        List<string> errors)
    {
        if (!DeploymentReportPathBoundary.IsWithinCanonicalRoot(canonicalRoot, declaredPath))
        {
            errors.Add($"{label} was blocked because its path escapes the trusted report root.");
            return null;
        }

        WslDeploymentArtifactReadResult read = await _reportReader.ReadTextArtifactAsync(
            profile.WslDistribution,
            canonicalRoot,
            declaredPath,
            maximumBytes,
            cancellationToken);

        if (!read.Success || read.Text is null || read.CanonicalPath is null)
        {
            errors.Add($"{label} could not be read safely: {read.Error ?? "unknown read failure"}");
            return null;
        }

        if (!DeploymentReportPathBoundary.IsWithinCanonicalRoot(canonicalRoot, read.CanonicalPath))
        {
            errors.Add($"{label} resolved outside the trusted report root and was blocked.");
            return null;
        }

        return new IncidentEvidenceInput(kind, label, BoundText(read.Text, preserveTail));
    }

    private static void AddConsoleEvidence(
        List<IncidentEvidenceInput> evidence,
        IReadOnlyList<ShellActivityEntry> entries,
        ConsoleChannel channel)
    {
        StringBuilder output = new();
        foreach (ShellActivityEntry entry in entries)
        {
            if (!IsInChannel(entry, channel)) continue;

            if (IsRawScriptEntry(entry))
            {
                output.AppendLine(entry.Message);
            }
            else
            {
                output.Append(entry.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
                output.Append(" | ");
                output.Append(entry.Level.ToString().ToUpperInvariant());
                output.Append(" | ");
                output.Append(entry.Category);
                output.Append(" | ");
                output.AppendLine(entry.Message);
            }
        }

        if (output.Length == 0) return;

        (IncidentEvidenceKind kind, string label) = channel switch
        {
            ConsoleChannel.DryRun => (IncidentEvidenceKind.DryRunConsole, "Dry run console"),
            ConsoleChannel.LiveDeploy => (IncidentEvidenceKind.LiveDeployConsole, "Live deploy console"),
            _ => (IncidentEvidenceKind.ApplicationActivity, "ZDeployPet console")
        };

        evidence.Add(new IncidentEvidenceInput(kind, label, BoundText(output.ToString(), preserveTail: true)));
    }

    private static bool IsInChannel(ShellActivityEntry entry, ConsoleChannel channel)
    {
        bool dry = string.Equals(entry.Category, DryRunActivityCategory, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(entry.Category, RawDryScriptCategory, StringComparison.OrdinalIgnoreCase);
        bool live = string.Equals(entry.Category, LiveDeployActivityCategory, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entry.Category, RawLiveScriptCategory, StringComparison.OrdinalIgnoreCase);

        return channel switch
        {
            ConsoleChannel.DryRun => dry,
            ConsoleChannel.LiveDeploy => live,
            ConsoleChannel.ZDeployPet => !dry && !live,
            _ => false
        };
    }

    private static bool IsRawScriptEntry(ShellActivityEntry entry)
        => string.Equals(entry.Category, RawDryScriptCategory, StringComparison.OrdinalIgnoreCase) ||
           string.Equals(entry.Category, RawLiveScriptCategory, StringComparison.OrdinalIgnoreCase);

    private static bool ArtifactsStayInsideRoot(string canonicalRoot, DeploymentReportArtifacts artifacts)
        => DeploymentReportPathBoundary.IsWithinCanonicalRoot(canonicalRoot, artifacts.Json)
           && DeploymentReportPathBoundary.IsWithinCanonicalRoot(canonicalRoot, artifacts.Summary)
           && DeploymentReportPathBoundary.IsWithinCanonicalRoot(canonicalRoot, artifacts.FullLog);

    private static string BoundText(string value, bool preserveTail)
    {
        if (value.Length <= MaximumCollectedCharacters)
            return value;

        const string marker = "\n[ZDeployPet collector excerpt — content omitted]\n";
        int available = MaximumCollectedCharacters - marker.Length;
        if (!preserveTail)
            return value[..available] + marker;

        int headLength = available / 2;
        int tailLength = available - headLength;
        return value[..headLength] + marker + value[^tailLength..];
    }

    private enum ConsoleChannel
    {
        DryRun,
        LiveDeploy,
        ZDeployPet
    }
}
