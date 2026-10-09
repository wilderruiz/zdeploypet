using System.Windows;

namespace ZDeployPet.App;

public partial class MainWindow
{
    /// <summary>
    /// The main window is the lifetime owner of ZDeployPet.
    ///
    /// WPF can otherwise leave the process alive when a background operation,
    /// dispatcher work item, or future companion window/thread remains active.
    /// That stale process keeps the published executable locked and prevents the
    /// local publish workflow from replacing it.
    ///
    /// The PDA-2 deployment session is also owned by this main-window lifetime.
    /// Closing ZDeployPet therefore performs a best-effort verified shutdown of
    /// the dedicated app-owned ssh-agent before the final process boundary.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        try
        {
            _deploymentSession.LockSynchronouslyBestEffort();
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
            // published executable locked after the UI has disappeared.
            Environment.Exit(0);
        }
    }
}
