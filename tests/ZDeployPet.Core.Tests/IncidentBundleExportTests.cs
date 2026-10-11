using ZDeployPet.Core;

namespace ZDeployPet.Core.Tests;

public sealed class IncidentBundleExportTests
{
    [Fact]
    public void ValidateDestination_AcceptsLocalTxtOutsideProject()
    {
        IncidentBundleExportValidationResult result = IncidentBundleExportPolicy.ValidateDestination(
            @"C:\Users\Example\Documents\zdeploypet-incident-run.txt",
            @"C:\Dev\ZDeployPet");

        Assert.True(result.Success);
        Assert.NotNull(result.CanonicalPath);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(@"\\server\share\incident.txt")]
    [InlineData("//server/share/incident.txt")]
    public void ValidateDestination_RejectsNetworkPaths(string destination)
    {
        IncidentBundleExportValidationResult result = IncidentBundleExportPolicy.ValidateDestination(
            destination,
            @"C:\Dev\ZDeployPet");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("Network/UNC", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateDestination_RejectsProjectRepositoryPath()
    {
        IncidentBundleExportValidationResult result = IncidentBundleExportPolicy.ValidateDestination(
            @"C:\Dev\ZDeployPet\artifacts\incident.txt",
            @"C:\Dev\ZDeployPet");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("project repository", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(@"C:\Users\Example\Documents\incident.json")]
    [InlineData(@"C:\Users\Example\Documents\incident.zip")]
    public void ValidateDestination_RequiresTxtExtension(string destination)
    {
        IncidentBundleExportValidationResult result = IncidentBundleExportPolicy.ValidateDestination(
            destination,
            @"C:\Dev\ZDeployPet");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains(".txt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateDestination_RejectsAlternateDataStream()
    {
        IncidentBundleExportValidationResult result = IncidentBundleExportPolicy.ValidateDestination(
            @"C:\Users\Example\Documents\incident.txt:secret",
            @"C:\Dev\ZDeployPet");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("Alternate data stream", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildSuggestedFileName_IsDeterministicAndSafe()
    {
        string first = IncidentBundleExportPolicy.BuildSuggestedFileName("20261011-051521_v4.4.25_live_vps");
        string second = IncidentBundleExportPolicy.BuildSuggestedFileName("20261011-051521_v4.4.25_live_vps");

        Assert.Equal(first, second);
        Assert.Equal("zdeploypet-incident-20261011-051521_v4.4.25_live_vps.txt", first);
    }
}
