using ZDeployPet.Core;

namespace ZDeployPet.Core.Tests;

public sealed class DeploymentProfileValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("/root")]
    [InlineData("/home/user")]
    [InlineData("relative/path")]
    [InlineData("/var/../etc")]
    [InlineData("~")]
    [InlineData("~/../other")]
    public void UnsafeRemoteDestinationsAreRejected(string path) =>
        Assert.False(DeploymentProfileValidator.IsSafeRemoteDestination(path));

    [Theory]
    [InlineData("/var/www/app")]
    [InlineData("/home/user/apps/site")]
    [InlineData("/srv/releases/current")]
    [InlineData("~/domains/example.test/public_html")]
    public void ScopedRemoteDestinationsAreAccepted(string path) =>
        Assert.True(DeploymentProfileValidator.IsSafeRemoteDestination(path));

    [Fact]
    public void ScriptTraversalIsRejected()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var profile = ValidProfile(root) with { ScriptRelativePath = "../deploy.sh" };
            ProfileValidationResult result = new DeploymentProfileValidator().Validate(profile);
            Assert.Contains(result.Errors, error => error.Contains("inside the project", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void OneTargetCanDeclareMultipleDistinctDestinations()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "deploy.sh"), "#!/bin/sh");
        try
        {
            DeploymentProfile profile = ValidProfile(root) with
            {
                Destinations =
                [
                    new DeploymentDestination("site", "production", "Site", "~/domains/example.test/public_html/app"),
                    new DeploymentDestination("config", "production", "Private config", "~/domains/example.test/public_html/app_config.php")
                ]
            };

            Assert.True(new DeploymentProfileValidator().Validate(profile).IsValid);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static DeploymentProfile ValidProfile(string root) => new(
        1, Guid.NewGuid().ToString("D"), "Example", root, "Ubuntu", "/mnt/c/example", "deploy.sh",
        null, "DEPLOY",
        [new DeploymentTarget("production", "Production", "example.invalid", 22, "deploy")],
        [new DeploymentDestination("app", "production", "Application", "/srv/example")]);
}
