using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda8ContextStripInitialized;

    private void InitializePda8ContextStrip()
    {
        if (_pda8ContextStripInitialized) return;
        _pda8ContextStripInitialized = true;

        Border? stripBorder = FindContextStripBorder();
        if (stripBorder is null) return;

        stripBorder.Padding = new Thickness(10, 7, 10, 7);
        stripBorder.Margin = new Thickness(0, 0, 0, 10);
        stripBorder.Background = FindBrush("AppSurfaceBrush", Brushes.Black);
        stripBorder.BorderBrush = Brushes.Transparent;
        stripBorder.BorderThickness = new Thickness(0);
        stripBorder.CornerRadius = new CornerRadius(8);
        stripBorder.SnapsToDevicePixels = true;

        ProfileSummaryText.FontSize = 11;
        ProfileSummaryText.FontWeight = FontWeights.Normal;
        ProfileSummaryText.Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray);
        ProfileSummaryText.TextWrapping = TextWrapping.Wrap;
        ProfileSummaryText.TextTrimming = TextTrimming.None;
        ProfileSummaryText.VerticalAlignment = VerticalAlignment.Center;
        ProfileSummaryText.MaxHeight = 34;

        // P10 parity: keep Home context readable inside its own column. The previous
        // single-line ellipsis plus full-value ToolTip could project a very wide popup
        // across the Activity pane. A compact two-line wrap matches the accepted mockup
        // more closely and keeps metadata inspection local to the Home workspace.
        ProfileSummaryText.ToolTip = null;
    }

    private Border? FindContextStripBorder()
    {
        DependencyObject? current = ProfileSummaryText;
        while (current is not null && current != DiscoveryPanel)
        {
            current = VisualTreeHelper.GetParent(current);
            if (current is Border border)
                return border;
        }

        return null;
    }
}
