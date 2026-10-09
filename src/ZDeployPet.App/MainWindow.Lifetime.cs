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
    /// Window_Closing in MainWindow.xaml.cs runs before this hook and remains the
    /// place for graceful close work such as saving an unfinished setup draft.
    /// PDA-2 must likewise perform deployment-session lock/cleanup during Closing
    /// before this final process boundary is reached.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

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
