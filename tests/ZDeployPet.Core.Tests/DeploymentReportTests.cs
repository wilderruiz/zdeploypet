using ZDeployPet.Core;

namespace ZDeployPet.Core.Tests;

public sealed class DeploymentReportTests
{
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
    [InlineData("/home/wilder/.local/state/millenova/deployments", "/home/wilder/.local/state/millenova/deployments/latest.json", true)]
    [InlineData("/home/wilder/.local/state/millenova/deployments", "/home/wilder/.local/state/millenova/deployments/2026-10-10/run.full.log", true)]
    [InlineData("/home/wilder/.local/state/millenova/deployments", "/home/wilder/.local/state/millenova/deployments", true)]
    [InlineData("/home/wilder/.local/state/millenova/deployments", "/home/wilder/.local/state/millenova/deployments-escape/run.json", false)]
    [InlineData("/home/wilder/.local/state/millenova/deployments", "/etc/passwd", false)]
    [InlineData("/home/wilder/.local/state/millenova/deployments", "relative/latest.json", false)]
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
