namespace ZDeployPet.Core;

public sealed record DiscoveryOptions(
    string WindowsRepositoryPath,
    string WslDistribution,
    string WslRepositoryPath,
    string DeploymentReportRoot);

public sealed record LocalDiscoveryResult(
    string RuntimeVersion,
    string OperatingSystem,
    bool RepositoryExists,
    bool DeployScriptExists,
    string? DeployScriptSha256);

public sealed record WslDiscoveryResult(
    bool Available,
    string Distribution,
    string? SshPath,
    string? SshVersion,
    string? SshAgentPath,
    string? SshAddPath,
    bool RepositoryExists,
    bool DeployScriptExists,
    bool ReportRootExists,
    string? Error);
