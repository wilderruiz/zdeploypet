using System.Threading.Tasks;
using System.Windows;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private static readonly TimeSpan ShutdownCleanupBudget = TimeSpan.FromSeconds(2.25);

    /// <summary>
    /// The main window is the lifetime owner of ZDeployPet.
    ///
    /// WPF can otherwise leave the process alive when a background operation,
    /// dispatcher work item, or future companion window/thread remains active.
    /// That stale process keeps the published executable locked and prevents the
    /// local publish workflow from replacing it.
    ///
    /// The PDA-2 deployment session is also owned by this main-window lifetime.
    /// Closing ZDeployPet therefore attempts a best-effort verified shutdown of
    /// the dedicated app-owned ssh-agent before the final process boundary.
    /// Cleanup is deliberately bounded: a stuck WSL/OpenSSH teardown must never
    /// prevent the visible application close from becoming a real process exit.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        try
        {
            Task cleanup = Task.Run(() =>
                _deploymentSession.LockSynchronouslyBestEffort(TimeSpan.FromSeconds(2)));

            // Cancellation is advisory to the underlying WSL/OpenSSH process path.
            // If that external teardown ignores cancellation, stop waiting here and
            // let the final process boundary below terminate all remaining workers.
            _ = cleanup.Wait(ShutdownCleanupBudget);
        }
        catch
        {
            // Closing the UI is authoritative. Failure to verify teardown must not
            // strand a hidden ZDeployPet process or keep the single-instance mutex.
        }
        finally
        {
            base.OnClosed(e);
        }

        try
        {
            Application.Current?.Shutdown(0);
        }
        finally
        {
            // Closing the main window means ZDeployPet is finished. Force the
            // process boundary so no hidden/background component can keep the
            // published executable locked or retain the single-instance mutex.
            Environment.Exit(0);
        }
    }
}
