using System.Text.Json;

namespace ZDeployPet.Core;

public enum DeploymentReportOutcome
{
    Pass,
    Failed,
    Cancelled
}

public sealed record DeploymentReportArtifacts(
    string FullLog,
    string Summary,
    string Json);

public sealed record DeploymentReportFailure(
    string Area,
    string Check,
    int ExitCode,
    string ShortDiagnostic);

public sealed record DeploymentReport(
    int SchemaVersion,
    string DeploymentId,
    string Release,
    string PreviousRelease,
    string Mode,
    string Target,
    DeploymentReportOutcome Outcome,
    int ExitStatus,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    int DurationSeconds,
    DeploymentReportArtifacts Artifacts,
    DeploymentReportFailure? Failure,
    string RawJson)
{
    // Reporter truth is authoritative. ExitStatus is retained as evidence only.
    public bool IsSuccessful => Outcome == DeploymentReportOutcome.Pass;
}

public sealed record DeploymentReportParseResult(
    bool Success,
    DeploymentReport? Report,
    IReadOnlyList<string> Errors)
{
    public static DeploymentReportParseResult Invalid(params string[] errors) =>
        new(false, null, errors);
}

public sealed record DeploymentReportHistoryCandidate(
    string CanonicalJsonPath,
    string Json);

public sealed record DeploymentReportHistorySelection(
    IReadOnlyList<DeploymentReport> Reports,
    int RejectedCount,
    int DuplicateCount,
    bool CandidateLimitReached,
    bool RowLimitReached);

public static class DeploymentReportHistory
{
    public static DeploymentReportHistorySelection SelectTrusted(
        IEnumerable<DeploymentReportHistoryCandidate> candidates,
        string canonicalRoot,
        int maximumCandidates,
        int maximumRows)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (!DeploymentReportPathBoundary.TryNormalizeCanonicalPosixPath(canonicalRoot, out string normalizedRoot))
            throw new ArgumentException("A safe absolute canonical report root is required.", nameof(canonicalRoot));
        if (maximumCandidates <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumCandidates));
        if (maximumRows <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumRows));

        List<DeploymentReport> trusted = [];
        HashSet<string> deploymentIds = new(StringComparer.Ordinal);
        int rejected = 0;
        int duplicates = 0;
        int processed = 0;
        bool candidateLimitReached = false;

        foreach (DeploymentReportHistoryCandidate candidate in candidates)
        {
            if (processed >= maximumCandidates)
            {
                candidateLimitReached = true;
                break;
            }
            processed++;

            if (!DeploymentReportPathBoundary.IsWithinCanonicalRoot(normalizedRoot, candidate.CanonicalJsonPath))
            {
                rejected++;
                continue;
            }

            DeploymentReportParseResult parsed = DeploymentReportParser.Parse(candidate.Json);
            if (!parsed.Success || parsed.Report is null ||
                !ArtifactsStayInsideRoot(normalizedRoot, parsed.Report.Artifacts))
            {
                rejected++;
                continue;
            }

            if (!deploymentIds.Add(parsed.Report.DeploymentId))
            {
                duplicates++;
                continue;
            }

            trusted.Add(parsed.Report);
        }

        trusted.Sort((left, right) =>
        {
            int byStarted = right.StartedAt.CompareTo(left.StartedAt);
            return byStarted != 0
                ? byStarted
                : StringComparer.Ordinal.Compare(right.DeploymentId, left.DeploymentId);
        });

        bool rowLimitReached = trusted.Count > maximumRows;
        if (rowLimitReached)
            trusted = trusted.Take(maximumRows).ToList();

        return new DeploymentReportHistorySelection(
            trusted,
            rejected,
            duplicates,
            candidateLimitReached,
            rowLimitReached);
    }

    private static bool ArtifactsStayInsideRoot(string canonicalRoot, DeploymentReportArtifacts artifacts)
        => DeploymentReportPathBoundary.IsWithinCanonicalRoot(canonicalRoot, artifacts.FullLog)
           && DeploymentReportPathBoundary.IsWithinCanonicalRoot(canonicalRoot, artifacts.Summary)
           && DeploymentReportPathBoundary.IsWithinCanonicalRoot(canonicalRoot, artifacts.Json);
}

public static class DeploymentReportParser
{
    public const int SupportedSchemaVersion = 1;

    public static DeploymentReportParseResult Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return DeploymentReportParseResult.Invalid("Deployment report JSON is empty.");

        try
        {
            using JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow
            });

            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return DeploymentReportParseResult.Invalid("Deployment report root must be a JSON object.");

            JsonElement root = document.RootElement;
            List<string> errors = [];

            int schemaVersion = RequiredInt(root, "schema_version", errors);
            if (schemaVersion != 0 && schemaVersion != SupportedSchemaVersion)
                errors.Add($"Unsupported deployment report schema_version: {schemaVersion}.");

            string deploymentId = RequiredString(root, "deployment_id", errors);
            string release = RequiredString(root, "release", errors);
            string previousRelease = RequiredString(root, "previous_release", errors);
            string mode = RequiredString(root, "mode", errors);
            string target = RequiredString(root, "target", errors);
            string resultText = RequiredString(root, "result", errors);
            int exitStatus = RequiredInt(root, "exit_status", errors);
            string startedAtText = RequiredString(root, "started_at", errors);
            string finishedAtText = RequiredString(root, "finished_at", errors);
            int durationSeconds = RequiredInt(root, "duration_seconds", errors);

            if (durationSeconds < 0)
                errors.Add("duration_seconds must not be negative.");

            DeploymentReportOutcome outcome = ParseOutcome(resultText, errors);
            DateTimeOffset startedAt = ParseTimestamp("started_at", startedAtText, errors);
            DateTimeOffset finishedAt = ParseTimestamp("finished_at", finishedAtText, errors);
            if (startedAt != default && finishedAt != default && finishedAt < startedAt)
                errors.Add("finished_at must not be earlier than started_at.");

            DeploymentReportArtifacts artifacts = ParseArtifacts(root, errors);
            DeploymentReportFailure? failure = ParseFailure(root, errors);

            if (outcome == DeploymentReportOutcome.Failed && failure is null)
                errors.Add("A failed deployment report must include a failure object.");

            if (errors.Count != 0)
                return new DeploymentReportParseResult(false, null, errors);

            DeploymentReport report = new(
                schemaVersion,
                deploymentId,
                release,
                previousRelease,
                mode,
                target,
                outcome,
                exitStatus,
                startedAt,
                finishedAt,
                durationSeconds,
                artifacts,
                failure,
                json);

            return new DeploymentReportParseResult(true, report, Array.Empty<string>());
        }
        catch (JsonException ex)
        {
            return DeploymentReportParseResult.Invalid(
                $"Deployment report JSON is malformed or incomplete: {ex.Message}");
        }
    }

    private static DeploymentReportOutcome ParseOutcome(string value, List<string> errors)
    {
        if (value.Equals("pass", StringComparison.OrdinalIgnoreCase))
            return DeploymentReportOutcome.Pass;
        if (value.Equals("failed", StringComparison.OrdinalIgnoreCase))
            return DeploymentReportOutcome.Failed;
        if (value.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
            return DeploymentReportOutcome.Cancelled;

        if (!string.IsNullOrWhiteSpace(value))
            errors.Add($"Unsupported deployment report result: {value}.");
        return default;
    }

    private static DeploymentReportArtifacts ParseArtifacts(JsonElement root, List<string> errors)
    {
        if (!root.TryGetProperty("artifacts", out JsonElement artifacts) || artifacts.ValueKind != JsonValueKind.Object)
        {
            errors.Add("Required object 'artifacts' is missing or invalid.");
            return new DeploymentReportArtifacts(string.Empty, string.Empty, string.Empty);
        }

        return new DeploymentReportArtifacts(
            RequiredString(artifacts, "full_log", errors, "artifacts.full_log"),
            RequiredString(artifacts, "summary", errors, "artifacts.summary"),
            RequiredString(artifacts, "json", errors, "artifacts.json"));
    }

    private static DeploymentReportFailure? ParseFailure(JsonElement root, List<string> errors)
    {
        if (!root.TryGetProperty("failure", out JsonElement failure) || failure.ValueKind == JsonValueKind.Null)
            return null;

        if (failure.ValueKind != JsonValueKind.Object)
        {
            errors.Add("failure must be null or an object.");
            return null;
        }

        return new DeploymentReportFailure(
            RequiredString(failure, "area", errors, "failure.area"),
            RequiredString(failure, "check", errors, "failure.check"),
            RequiredInt(failure, "exit_code", errors, "failure.exit_code"),
            OptionalString(failure, "short_diagnostic"));
    }

    private static string RequiredString(
        JsonElement element,
        string propertyName,
        List<string> errors,
        string? displayName = null)
    {
        string name = displayName ?? propertyName;
        if (!element.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind != JsonValueKind.String)
        {
            errors.Add($"Required string '{name}' is missing or invalid.");
            return string.Empty;
        }

        string text = value.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            errors.Add($"Required string '{name}' must not be empty.");
        return text;
    }

    private static string OptionalString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind != JsonValueKind.String)
            return string.Empty;
        return value.GetString() ?? string.Empty;
    }

    private static int RequiredInt(
        JsonElement element,
        string propertyName,
        List<string> errors,
        string? displayName = null)
    {
        string name = displayName ?? propertyName;
        if (!element.TryGetProperty(propertyName, out JsonElement value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out int number))
        {
            errors.Add($"Required integer '{name}' is missing or invalid.");
            return 0;
        }

        return number;
    }

    private static DateTimeOffset ParseTimestamp(string name, string value, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            return default;

        if (DateTimeOffset.TryParse(value, out DateTimeOffset timestamp))
            return timestamp;

        errors.Add($"{name} is not a valid timestamp.");
        return default;
    }
}

public static class DeploymentReportPathBoundary
{
    public static bool IsWithinCanonicalRoot(string? canonicalRoot, string? canonicalCandidate)
    {
        if (!TryNormalizeCanonicalPosixPath(canonicalRoot, out string root) ||
            !TryNormalizeCanonicalPosixPath(canonicalCandidate, out string candidate))
            return false;

        if (root == "/")
            return true;

        return candidate.Equals(root, StringComparison.Ordinal) ||
               candidate.StartsWith(root + "/", StringComparison.Ordinal);
    }

    public static bool TryNormalizeCanonicalPosixPath(string? path, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/'))
            return false;

        string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        List<string> clean = [];

        foreach (string segment in segments)
        {
            if (segment == ".")
                continue;
            if (segment == "..")
            {
                if (clean.Count == 0)
                    return false;
                clean.RemoveAt(clean.Count - 1);
                continue;
            }
            clean.Add(segment);
        }

        normalized = clean.Count == 0 ? "/" : "/" + string.Join('/', clean);
        return true;
    }
}
