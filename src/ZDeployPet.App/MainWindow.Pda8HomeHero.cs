using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private static readonly bool Pda8HomeHeroRegistered = RegisterPda8HomeHero();
    private bool _pda8HomeHeroInitialized;
    private TextBlock? _pda8HomeHeading;

    private static bool RegisterPda8HomeHero()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(MainWindowPda8HomeHeroLoaded));
        return true;
    }

    private static void MainWindowPda8HomeHeroLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window || window._pda8HomeHeroInitialized)
            return;

        window.InitializePda8HomeHero();
    }

    private void InitializePda8HomeHero()
    {
        if (_pda8HomeHeroInitialized) return;
        _pda8HomeHeroInitialized = true;

        _pda8HomeHeading = FindLegacyHomeHeading();

        DiscoveryPanel.IsVisibleChanged += (_, _) => RefreshPda8HomeHero();
        SetupPanel.IsVisibleChanged += (_, _) => RefreshPda8HomeHero();

        RefreshPda8HomeHero();
    }

    private TextBlock? FindLegacyHomeHeading()
    {
        if (DiscoveryPanel.Parent is not Grid root)
            return null;

        foreach (UIElement child in root.Children)
        {
            if (Grid.GetRow(child) != 0 || child is not Grid headerGrid)
                continue;

            foreach (UIElement headerChild in headerGrid.Children)
            {
                if (headerChild is TextBlock textBlock &&
                    string.Equals(textBlock.Text, "ZDeployPet", StringComparison.Ordinal))
                {
                    return textBlock;
                }
            }
        }

        return null;
    }

    private void RefreshPda8HomeHero()
    {
        if (_pda8HomeHeading is null)
            return;

        bool homeVisible = DiscoveryPanel.Visibility == Visibility.Visible && _activeProfile is not null;
        bool profileVisible = SetupPanel.Visibility == Visibility.Visible;

        _pda8HomeHeading.Foreground = FindBrush("AppTextBrush", Brushes.WhiteSmoke);
        _pda8HomeHeading.FontWeight = FontWeights.SemiBold;
        _pda8HomeHeading.FontSize = 28;

        SubtitleText.Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray);
        SubtitleText.Margin = new Thickness(0, 7, 0, 18);

        if (homeVisible)
        {
            _pda8HomeHeading.Text = "Deployment workspace";
            SubtitleText.Text = "A controlled release surface for verified scripts, immutable reviews and trusted deployment evidence.";
            return;
        }

        if (profileVisible)
        {
            _pda8HomeHeading.Text = "Deployment profile";
            SubtitleText.Text = "Configure the local project, runner, targets and destination map ZDeployPet is allowed to use.";
            return;
        }

        _pda8HomeHeading.Text = "ZDeployPet";
    }
}
