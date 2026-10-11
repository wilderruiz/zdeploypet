using ZDeployPet.Core;

namespace ZDeployPet.Core.Tests;

public sealed class IncidentBundleTests
{
    [Fact]
    public void Sanitize_RedactsPrivateKeySecretBearerAndAgentSocket()
    {
        string input = """
password=hunter2
Authorization: Bearer abc.def.ghi
SSH_AUTH_SOCK=/tmp/ssh-AbCdEf/agent.1234
key_path=/home/wilder/.ssh/id_ed25519
windows_key=C:\Users\Example\.ssh\id_ed25519
-----BEGIN OPENSSH PRIVATE KEY-----
very-secret-key-bytes
-----END OPENSSH PRIVATE KEY-----
""";

        IncidentEvidenceSanitizer.SanitizationResult result = IncidentEvidenceSanitizer.Sanitize(input);

        Assert.True(result.RedactionApplied);
        Assert.DoesNotContain("hunter2", result.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("abc.def.ghi", result.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("/tmp/ssh-AbCdEf/agent.1234", result.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("/home/wilder/.ssh/id_ed25519", result.Content, StringComparison.Ordinal);
        Assert.DoesNotContain(@"C:\Users\Example\.ssh\id_ed25519", result.Content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("very-secret-key-bytes", result.Content, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", result.Content, StringComparison.Ordinal);
        Assert.Contains("[SSH AGENT SOCKET REDACTED]", result.Content, StringComparison.Ordinal);
        Assert.Contains("[SSH PRIVATE KEY PATH REDACTED]", result.Content, StringComparison.Ordinal);
        Assert.Contains("[PRIVATE KEY REDACTED]", result.Content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("token=abcdef")]
    [InlineData("secret: abcdef")]
    [InlineData("api_key=abcdef")]
    [InlineData("access-key: abcdef")]
    [InlineData("passphrase='abcdef'")]
    public void Sanitize_RedactsCommonSecretAssignments(string input)
    {
        IncidentEvidenceSanitizer.SanitizationResult result = IncidentEvidenceSanitizer.Sanitize(input);

        Assert.True(result.RedactionApplied);
        Assert.DoesNotContain("abcdef", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_SanitizesEvidenceAndPreservesKindAndLabel()
    {
        IncidentBundleBuildResult result = IncidentEvidenceSanitizer.Build(
        [
            new IncidentEvidenceInput(
                IncidentEvidenceKind.LiveDeployConsole,
                "Live deployment console",
                "PASS deployment\ntoken=super-secret")
        ]);

        Assert.True(result.Success);
        SanitizedIncidentEvidence item = Assert.Single(result.Evidence);
        Assert.Equal(IncidentEvidenceKind.LiveDeployConsole, item.Kind);
        Assert.Equal("Live deployment console", item.Label);
        Assert.True(item.RedactionApplied);
        Assert.DoesNotContain("super-secret", item.Content, StringComparison.Ordinal);
        Assert.Equal(item.Content.Length, result.TotalCharacters);
    }

    [Fact]
    public void Build_RejectsUnsupportedEvidenceKind()
    {
        IncidentBundleBuildResult result = IncidentEvidenceSanitizer.Build(
        [
            new IncidentEvidenceInput((IncidentEvidenceKind)999, "Unknown evidence", "safe")
        ]);

        Assert.False(result.Success);
        Assert.Empty(result.Evidence);
        Assert.Contains(result.Errors, error => error.Contains("Unsupported incident-evidence kind", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Build_FailsClosedWhenItemInputExceedsLimit()
    {
        string oversized = new('x', IncidentBundleLimits.MaximumInputCharactersPerItem + 1);

        IncidentBundleBuildResult result = IncidentEvidenceSanitizer.Build(
        [
            new IncidentEvidenceInput(IncidentEvidenceKind.DeploymentFullLog, "Full log", oversized)
        ]);

        Assert.False(result.Success);
        Assert.Empty(result.Evidence);
        Assert.Contains(result.Errors, error => error.Contains("input limit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Build_TruncatesSanitizedItemAtBoundedPreviewLimit()
    {
        string content = new('x', IncidentBundleLimits.MaximumSanitizedCharactersPerItem + 100);

        IncidentBundleBuildResult result = IncidentEvidenceSanitizer.Build(
        [
            new IncidentEvidenceInput(IncidentEvidenceKind.ApplicationActivity, "Activity", content)
        ]);

        Assert.True(result.Success);
        SanitizedIncidentEvidence item = Assert.Single(result.Evidence);
        Assert.True(item.Truncated);
        Assert.Contains("[ZDeployPet evidence truncated]", item.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_RejectsTooManyItems()
    {
        IncidentEvidenceInput[] items = Enumerable.Range(0, IncidentBundleLimits.MaximumItems + 1)
            .Select(index => new IncidentEvidenceInput(
                IncidentEvidenceKind.ApplicationActivity,
                $"Activity {index}",
                "safe"))
            .ToArray();

        IncidentBundleBuildResult result = IncidentEvidenceSanitizer.Build(items);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("limited to", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Build_RejectsControlCharactersInLabel()
    {
        IncidentBundleBuildResult result = IncidentEvidenceSanitizer.Build(
        [
            new IncidentEvidenceInput(IncidentEvidenceKind.DeploymentSummary, "Summary\nInjected", "safe")
        ]);

        Assert.False(result.Success);
        Assert.Empty(result.Evidence);
    }

    [Fact]
    public void Sanitize_LeavesOrdinaryDeploymentOutputUnchanged()
    {
        const string input = "PASS [HOSTINGER FRONTEND] Hostinger site sync | 1 files | 7 B";

        IncidentEvidenceSanitizer.SanitizationResult result = IncidentEvidenceSanitizer.Sanitize(input);

        Assert.False(result.RedactionApplied);
        Assert.Equal(input, result.Content);
    }
}
