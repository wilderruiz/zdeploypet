using System.Security.Cryptography;
using MillenovaDeployConsole.Core;

namespace MillenovaDeployConsole.Infrastructure;

public sealed class LocalCapabilityDiscovery
{
    public LocalDiscoveryResult Discover(string repositoryPath)
    {
        string deployScriptPath = Path.Combine(repositoryPath, "deploy_millenova.sh");
        bool scriptExists = File.Exists(deployScriptPath);

        return new LocalDiscoveryResult(
            Environment.Version.ToString(),
            Environment.OSVersion.VersionString,
            Directory.Exists(repositoryPath),
            scriptExists,
            scriptExists ? ComputeSha256(deployScriptPath) : null);
    }

    public static string ComputeSha256(string filePath)
    {
        using FileStream stream = File.OpenRead(filePath);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
