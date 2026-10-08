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

    private static DeploymentProfile ValidProfile(string root) => new(
        1, Guid.NewGuid().ToString("D"), "Example", root, "Ubuntu", "/mnt/c/example", "deploy.sh",
        null, "DEPLOY", [new DeploymentTarget("production", "Production", "example.invalid", 22, "deploy", "/srv/example")]);
}
