using MillenovaDeployConsole.Core;

namespace MillenovaDeployConsole.WslBridge;

public static class WslDiscoveryParser
{
    public static WslDiscoveryResult Parse(string distribution, string output, string? error = null)
    {
        Dictionary<string, string> values = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);

        string? Value(string key) => values.TryGetValue(key, out string? value) && value.Length > 0 ? value : null;
        bool Flag(string key) => string.Equals(Value(key), "true", StringComparison.OrdinalIgnoreCase);

        return new WslDiscoveryResult(error is null, distribution, Value("ssh_path"), Value("ssh_version"),
            Value("ssh_agent_path"), Value("ssh_add_path"), Flag("repository_exists"),
            Flag("deploy_script_exists"), Flag("report_root_exists"), error);
    }
}
