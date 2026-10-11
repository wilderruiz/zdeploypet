using ZDeployPet.Core;

namespace ZDeployPet.Core.Tests;

public sealed class IncidentAgentHandoffManifestTests
{
    [Fact]
    public void Build_ProducesDeterministicAuthorityFreeManifest()
    {
        IncidentAgentHandoffManifestInput input = new(
            RepositoryLabel: "millenova",
            ProfileName: "Millenova",
            DeploymentId: "20261011-051521_v4.4.25_live_vps",
            ReporterOutcome: "Pass",
            Release: "4.4.25",
            Mode: "live",
            Target: "vps",
            IncludedEvidenceLabels: ["Deployment JSON", "Deployment summary", "ZDeployPet console"],
            OmittedEvidenceLabels: ["Dry run console"]);

        IncidentAgentHandoffManifestBuildResult first = IncidentAgentHandoffManifest.Build(input);
        IncidentAgentHandoffManifestBuildResult second = IncidentAgentHandoffManifest.Build(input);

        Assert.True(first.Success);
        Assert.Equal(first.Manifest, second.Manifest);
        Assert.Empty(first.Errors);
        Assert.Contains("Repository: millenova", first.Manifest, StringComparison.Ordinal);
        Assert.Contains("Deployment ID: 20261011-051521_v4.4.25_live_vps", first.Manifest, StringComparison.Ordinal);
        Assert.Contains("Authority: NONE", first.Manifest, StringComparison.Ordinal);
        Assert.Contains("Credentials included: NO", first.Manifest, StringComparison.Ordinal);
        Assert.Contains("SSH-agent/session capability included: NO", first.Manifest, StringComparison.Ordinal);
        Assert.Contains("Remote-write/deployment authorization included: NO", first.Manifest, StringComparison.Ordinal);
        Assert.Contains("Any proposed patch is UNTRUSTED", first.Manifest, StringComparison.Ordinal);
        Assert.DoesNotContain("C:\\", first.Manifest, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/home/", first.Manifest, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("token=abcdef")]
    [InlineData("password=hunter2")]
    [InlineData("/home/example/.ssh/id_ed25519")]
    [InlineData("SSH_AUTH_SOCK=/tmp/ssh-AbCd/agent.42")]
    public void Build_RejectsSecretLikeManifestMetadata(string unsafeRepositoryLabel)
    {
        IncidentAgentHandoffManifestBuildResult result = IncidentAgentHandoffManifest.Build(new(
            RepositoryLabel: unsafeRepositoryLabel,
            ProfileName: "Millenova",
            DeploymentId: "run-1",
            ReporterOutcome: "Pass",
            Release: "1.0.0",
            Mode: "live",
            Target: "vps",
            IncludedEvidenceLabels: ["Deployment summary"],
            OmittedEvidenceLabels: []));

        Assert.False(result.Success);
        Assert.Empty(result.Manifest);
        Assert.Contains(result.Errors, error => error.Contains("unsafe", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Build_RejectsControlCharactersInEvidenceLabels()
    {
        IncidentAgentHandoffManifestBuildResult result = IncidentAgentHandoffManifest.Build(new(
            RepositoryLabel: "millenova",
            ProfileName: "Millenova",
            DeploymentId: "run-1",
            ReporterOutcome: "Pass",
            Release: "1.0.0",
            Mode: "live",
            Target: "vps",
            IncludedEvidenceLabels: ["Summary\nInjected"],
            OmittedEvidenceLabels: []));

        Assert.False(result.Success);
        Assert.Empty(result.Manifest);
    }
}
