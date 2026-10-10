using ZDeployPet.Core;

namespace ZDeployPet.Core.Tests;

public sealed class DeploymentReportTests
{
    private const string ReportRoot = "/home/wilder/.local/state/millenova/deployments";

    [Fact]
    public void ParsesValidPassReportAndKeepsReporterResultAuthoritative()
    {
        DeploymentReportParseResult result = DeploymentReportParser.Parse(ValidReport(
            result: "pass",
            exitStatus: 7,
            failureJson: "null"));

        Assert.True(result.Success);
        Assert.NotNull(result.Report);
        Assert.Equal(DeploymentReportOutcome.Pass, result.Report!.Outcome);
        Assert.True(result.Report.IsSuccessful);
        Assert.Equal(7, result.Report.ExitStatus);
        Assert.Equal("4.4.9", result.Report.Release);
        Assert.Equal("both", result.Report.Target);
    }

    [Fact]
    public void ParsesFailedReportWithFailureEvidence()
    {
        DeploymentReportParseResult result = DeploymentReportParser.Parse(ValidReport(
            result: "failed",
            exitStatus: 23,
            failureJson: """
              {
                "area": "VPS",
                "check": "Health check",
                "exit_code": 23,
                "short_diagnostic": "health endpoint failed"
              }
              """));

        Assert.True(result.Success);
        Assert.NotNull(result.Report);
        Assert.Equal(DeploymentReportOutcome.Failed, result.Report!.Outcome);
        Assert.False(result.Report.IsSuccessful);
        Assert.NotNull(result.Report.Failure);
        Assert.Equal("Health check", result.Report.Failure!.Check);
        Assert.Equal(23, result.Report.Failure.ExitCode);
    }

    [Fact]
    public void ParsesCancelledReport()
    {
        DeploymentReportParseResult result = DeploymentReportParser.Parse(ValidReport(
            result: "cancelled",
            exitStatus: 130,
            failureJson: "null"));

        Assert.True(result.Success);
        Assert.Equal(DeploymentReportOutcome.Cancelled, result.Report!.Outcome);
        Assert.False(result.Report.IsSuccessful);
    }

    [Fact]
    public void RejectsMalformedOrPartiallyWrittenJson()
    {
        DeploymentReportParseResult result = DeploymentReportParser.Parse("{ \"schema_version\": 1, \"deployment_id\": \"x\"");

        Assert.False(result.Success);
        Assert.Null(result.Report);
        Assert.Contains(result.Errors, error => error.Contains("malformed or incomplete", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RejectsUnsupportedSchema()
    {
        string json = ValidReport("pass", 0, "null").Replace("\"schema_version\": 1", "\"schema_version\": 2");

        DeploymentReportParseResult result = DeploymentReportParser.Parse(json);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("Unsupported deployment report schema_version", StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsFailedReportWithoutFailureObject()
    {
        DeploymentReportParseResult result = DeploymentReportParser.Parse(ValidReport("failed", 1, "null"));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("must include a failure object", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(ReportRoot, ReportRoot + "/latest.json", true)]
    [InlineData(ReportRoot, ReportRoot + "/2026-10-10/run.full.log", true)]
    [InlineData(ReportRoot, ReportRoot, true)]
    [InlineData(ReportRoot, ReportRoot + "-escape/run.json", false)]
    [InlineData(ReportRoot, "/etc/passwd", false)]
    [InlineData(ReportRoot, "relative/latest.json", false)]
    public void CanonicalPathBoundaryRequiresCandidateInsideRoot(string root, string candidate, bool expected)
    {
        Assert.Equal(expected, DeploymentReportPathBoundary.IsWithinCanonicalRoot(root, candidate));
    }

    [Fact]
    public void CanonicalPathNormalizationCollapsesDotSegmentsButRejectsRootEscape()
    {
        Assert.True(DeploymentReportPathBoundary.TryNormalizeCanonicalPosixPath(
            "/home/wilder/reports/2026-10-10/../latest.json",
            out string normalized));
        Assert.Equal("/home/wilder/reports/latest.json", normalized);

        Assert.False(DeploymentReportPathBoundary.TryNormalizeCanonicalPosixPath("/../etc/passwd", out _));
    }

    [Fact]
    public void HistorySelectionOrdersNewestFirstAndDeduplicatesDeploymentId()
    {
        DeploymentReportHistoryCandidate[] candidates =
        [
            Candidate("older.json", ReportWithIdentity("older", "2026-10-10T10:00:00+03:00")),
            Candidate("newest.json", ReportWithIdentity("newest", "2026-10-10T12:00:00+03:00")),
            Candidate("duplicate.json", ReportWithIdentity("newest", "2026-10-10T12:00:00+03:00")),
            Candidate("middle.json", ReportWithIdentity("middle", "2026-10-10T11:00:00+03:00"))
        ];

        DeploymentReportHistorySelection selection = DeploymentReportHistory.SelectTrusted(
            candidates,
            ReportRoot,
            maximumCandidates: 90,
            maximumRows: 30);

        Assert.Equal(["newest", "middle", "older"], selection.Reports.Select(report => report.DeploymentId));
        Assert.Equal(1, selection.DuplicateCount);
        Assert.Equal(0, selection.RejectedCount);
        Assert.False(selection.RowLimitReached);
    }

    [Fact]
    public void HistorySelectionRejectsPartialJsonAndPathEscapes()
    {
        DeploymentReportHistoryCandidate[] candidates =
        [
            Candidate("valid.json", ReportWithIdentity("valid", "2026-10-10T12:00:00+03:00")),
            Candidate("partial.json", "{\"schema_version\":1"),
            new DeploymentReportHistoryCandidate(
                "/etc/escaped.json",
                ReportWithIdentity("escaped-candidate", "2026-10-10T13:00:00+03:00")),
            Candidate(
                "escaped-artifact.json",
                ReportWithIdentity("escaped-artifact", "2026-10-10T14:00:00+03:00")
                    .Replace(ReportRoot + "/2026-10-10/run.full.log", "/etc/passwd"))
        ];

        DeploymentReportHistorySelection selection = DeploymentReportHistory.SelectTrusted(
            candidates,
            ReportRoot,
            maximumCandidates: 90,
            maximumRows: 30);

        Assert.Single(selection.Reports);
        Assert.Equal("valid", selection.Reports[0].DeploymentId);
        Assert.Equal(3, selection.RejectedCount);
    }

    [Fact]
    public void HistorySelectionEnforcesCandidateAndRowBounds()
    {
        DeploymentReportHistoryCandidate[] candidates = Enumerable.Range(0, 8)
            .Select(index => Candidate(
                $"run-{index}.json",
                ReportWithIdentity($"run-{index}", $"2026-10-10T{index + 10:00}:00:00+03:00")))
            .ToArray();

        DeploymentReportHistorySelection candidateBound = DeploymentReportHistory.SelectTrusted(
            candidates,
            ReportRoot,
            maximumCandidates: 5,
            maximumRows: 30);
        Assert.True(candidateBound.CandidateLimitReached);
        Assert.Equal(5, candidateBound.Reports.Count);

        DeploymentReportHistorySelection rowBound = DeploymentReportHistory.SelectTrusted(
            candidates,
            ReportRoot,
            maximumCandidates: 90,
            maximumRows: 3);
        Assert.True(rowBound.RowLimitReached);
        Assert.Equal(3, rowBound.Reports.Count);
        Assert.Equal(["run-7", "run-6", "run-5"], rowBound.Reports.Select(report => report.DeploymentId));
    }

    private static DeploymentReportHistoryCandidate Candidate(string fileName, string json) =>
        new($"{ReportRoot}/2026-10-10/{fileName}", json);

    private static string ReportWithIdentity(string deploymentId, string startedAt)
    {
        DateTimeOffset started = DateTimeOffset.Parse(startedAt);
        string finished = started.AddSeconds(15).ToString("yyyy-MM-dd'T'HH:mm:sszzz");
        return ValidReport("pass", 0, "null")
            .Replace("20261010-120000_v4.4.9_dry_both", deploymentId)
            .Replace("2026-10-10T12:00:00+03:00", started.ToString("yyyy-MM-dd'T'HH:mm:sszzz"))
            .Replace("2026-10-10T12:00:15+03:00", finished);
    }

    private static string ValidReport(string result, int exitStatus, string failureJson) => $$"""
    {
      "schema_version": 1,
      "deployment_id": "20261010-120000_v4.4.9_dry_both",
      "release": "4.4.9",
      "previous_release": "4.4.8",
      "mode": "dry",
      "target": "both",
      "result": "{{result}}",
      "exit_status": {{exitStatus}},
      "started_at": "2026-10-10T12:00:00+03:00",
      "finished_at": "2026-10-10T12:00:15+03:00",
      "duration_seconds": 15,
      "checks": [],
      "tests": {
        "node_local": null,
        "node_vps": null
      },
      "hostinger": {
        "attempted": true,
        "files_changed": 1,
        "bytes_changed": 6,
        "result": "pass"
      },
      "vps": {
        "attempted": true,
        "files_changed": 0,
        "bytes_changed": 0,
        "result": "pass"
      },
      "failure": {{failureJson}},
      "artifacts": {
        "full_log": "/home/wilder/.local/state/millenova/deployments/2026-10-10/run.full.log",
        "summary": "/home/wilder/.local/state/millenova/deployments/2026-10-10/run.summary.txt",
        "json": "/home/wilder/.local/state/millenova/deployments/2026-10-10/run.json"
      }
    }
    """;
}
