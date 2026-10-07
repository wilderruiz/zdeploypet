using System.Diagnostics;
using MillenovaDeployConsole.Core;

namespace MillenovaDeployConsole.WslBridge;

public sealed class WslCapabilityDiscovery
{
    private const string ProbeScript = """
        repo="$1"
        reports="$2"
        printf 'ssh_path=%s\n' "$(command -v ssh || true)"
        printf 'ssh_version=%s\n' "$(ssh -V 2>&1 || true)"
        printf 'ssh_agent_path=%s\n' "$(command -v ssh-agent || true)"
        printf 'ssh_add_path=%s\n' "$(command -v ssh-add || true)"
        test -d "$repo" && printf 'repository_exists=true\n' || printf 'repository_exists=false\n'
        test -f "$repo/deploy_millenova.sh" && printf 'deploy_script_exists=true\n' || printf 'deploy_script_exists=false\n'
        test -d "$reports" && printf 'report_root_exists=true\n' || printf 'report_root_exists=false\n'
        """;

    public async Task<WslDiscoveryResult> DiscoverAsync(DiscoveryOptions options, CancellationToken cancellationToken = default)
    {
        using Process process = new();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "wsl.exe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        process.StartInfo.ArgumentList.Add("--distribution");
        process.StartInfo.ArgumentList.Add(options.WslDistribution);
        process.StartInfo.ArgumentList.Add("--");
        process.StartInfo.ArgumentList.Add("sh");
        process.StartInfo.ArgumentList.Add("-c");
        process.StartInfo.ArgumentList.Add(ProbeScript);
        process.StartInfo.ArgumentList.Add("pda0-probe");
        process.StartInfo.ArgumentList.Add(options.WslRepositoryPath);
        process.StartInfo.ArgumentList.Add(options.DeploymentReportRoot);

        try
        {
            process.Start();
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            string output = await outputTask;
            string standardError = await errorTask;
            return process.ExitCode == 0
                ? WslDiscoveryParser.Parse(options.WslDistribution, output)
                : WslDiscoveryParser.Parse(options.WslDistribution, output, standardError.Trim());
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return WslDiscoveryParser.Parse(options.WslDistribution, string.Empty, exception.Message);
        }
    }
}
