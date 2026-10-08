namespace ZDeployPet.Core;

public sealed record DeploymentTarget(
    string Id,
    string Label,
    string Host,
    int Port,
    string User);

public sealed record DeploymentDestination(
    string Id,
    string TargetId,
    string Label,
    string RemotePath);

public sealed record DeploymentProfile(
    int SchemaVersion,
    string Id,
    string Name,
    string WindowsProjectPath,
    string WslDistribution,
    string WslProjectPath,
    string ScriptRelativePath,
    string? ReportRoot,
    string LiveConfirmationPhrase,
    IReadOnlyList<DeploymentTarget> Targets,
    IReadOnlyList<DeploymentDestination> Destinations)
{
    public const int CurrentSchemaVersion = 1;
}

public sealed record ProfileValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
