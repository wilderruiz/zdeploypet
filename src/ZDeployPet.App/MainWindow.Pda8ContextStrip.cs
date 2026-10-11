using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda8ContextStripInitialized;
    private string? _pda8ContextStripLastText;

    private void InitializePda8ContextStrip()
    {
        if (_pda8ContextStripInitialized) return;
        _pda8ContextStripInitialized = true;

        Border? stripBorder = FindContextStripBorder();
        if (stripBorder is null) return;

        stripBorder.Padding = new Thickness(10, 7, 10, 7);
        stripBorder.Margin = new Thickness(0, 0, 0, 10);
        stripBorder.Background = FindBrush("AppSurfaceBrush", Brushes.Black);
        stripBorder.BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray);
        stripBorder.BorderThickness = new Thickness(1);
        stripBorder.CornerRadius = new CornerRadius(8);
        stripBorder.SnapsToDevicePixels = true;

        ProfileSummaryText.FontSize = 11;
        ProfileSummaryText.FontWeight = FontWeights.Normal;
        ProfileSummaryText.Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray);
        ProfileSummaryText.TextWrapping = TextWrapping.NoWrap;
        ProfileSummaryText.TextTrimming = TextTrimming.CharacterEllipsis;
        ProfileSummaryText.VerticalAlignment = VerticalAlignment.Center;

        // The visible strip stays deliberately compact. Keep the complete context
        // available on hover even when a long project path is ellipsized.
        ProfileSummaryText.LayoutUpdated += (_, _) => RefreshPda8ContextStripToolTip();
        RefreshPda8ContextStripToolTip();
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

    private void RefreshPda8ContextStripToolTip()
    {
        string text = ProfileSummaryText.Text ?? string.Empty;
        if (string.Equals(text, _pda8ContextStripLastText, StringComparison.Ordinal)) return;

        _pda8ContextStripLastText = text;
        ProfileSummaryText.ToolTip = string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
