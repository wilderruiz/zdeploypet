using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda8V8ReportVisualsInitialized;
    private readonly HashSet<Grid> _pda8V8StyledHistoryRows = new();

    private void InitializePda8V8ReportVisuals()
    {
        if (_pda8V8ReportVisualsInitialized)
            return;

        if (_latestReportCard is null ||
            _deploymentHistoryCard is null ||
            _deploymentHistoryRowsPanel is null)
            return;

        _pda8V8ReportVisualsInitialized = true;

        StyleReportCard(_latestReportCard, raised: true);
        StyleReportCard(_deploymentHistoryCard, raised: false);

        if (_latestReportStateText is not null)
        {
            _latestReportStateText.FontSize = 11;
            _latestReportStateText.Margin = new Thickness(0, 3, 0, 0);
        }

        if (_latestReportDetailsText is not null)
        {
            _latestReportDetailsText.FontSize = 11;
            _latestReportDetailsText.LineHeight = 17;
            _latestReportDetailsText.Margin = new Thickness(0, 8, 0, 0);
        }

        ApplyCompactReportAction(_latestReportSummaryButton);
        ApplyCompactReportAction(_latestReportFullLogButton);

        if (_latestReportRefreshButton is not null &&
            Application.Current.TryFindResource("Pda8IconButtonStyle") is Style iconButtonStyle)
        {
            _latestReportRefreshButton.Style = iconButtonStyle;
            _latestReportRefreshButton.ToolTip = "Refresh deployment reports";
        }

        if (_deploymentHistoryStatusText is not null)
        {
            _deploymentHistoryStatusText.FontSize = 11;
            _deploymentHistoryStatusText.LineHeight = 16;
            _deploymentHistoryStatusText.Margin = new Thickness(0, 3, 0, 8);
        }

        StyleHistoryHeader();
        StyleHistoryRows();
        _deploymentHistoryRowsPanel.LayoutUpdated += (_, _) => StyleHistoryRows();
    }

    private void StyleReportCard(Border card, bool raised)
    {
        string styleKey = raised ? "Pda8RaisedCardStyle" : "Pda8CardStyle";
        if (Application.Current.TryFindResource(styleKey) is Style style)
            card.Style = style;

        card.Margin = new Thickness(0, 0, 0, 12);
        card.Padding = raised
            ? new Thickness(16, 14, 16, 14)
            : new Thickness(16, 13, 16, 13);
        card.CornerRadius = new CornerRadius(12);
    }

    private void ApplyCompactReportAction(Button? button)
    {
        if (button is null)
            return;

        if (Application.Current.TryFindResource("Pda8ButtonStyle") is Style buttonStyle)
            button.Style = buttonStyle;

        button.MinHeight = 28;
        button.Padding = new Thickness(9, 5, 9, 5);
        button.FontSize = 11;
        button.Margin = button == _latestReportSummaryButton
            ? new Thickness(0, 0, 8, 0)
            : new Thickness(0);
    }

    private void StyleHistoryHeader()
    {
        if (_deploymentHistoryRowsPanel?.Parent is not StackPanel table ||
            table.Children.Count == 0 ||
            table.Children[0] is not Grid header)
            return;

        header.Margin = new Thickness(0, 0, 0, 2);

        foreach (UIElement child in header.Children)
        {
            if (child is not TextBlock text)
                continue;

            text.FontSize = 10.5;
            text.FontWeight = FontWeights.SemiBold;
            text.Margin = new Thickness(4, 2, 4, 5);
            text.Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray);
        }
    }

    private void StyleHistoryRows()
    {
        if (_deploymentHistoryRowsPanel is null)
            return;

        foreach (UIElement child in _deploymentHistoryRowsPanel.Children)
        {
            if (child is not Grid row || !_pda8V8StyledHistoryRows.Add(row))
                continue;

            row.Margin = new Thickness(0);
            row.MinHeight = 30;

            Border separator = new()
            {
                BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Background = Brushes.Transparent,
                IsHitTestVisible = false,
                Margin = new Thickness(0)
            };
            Grid.SetColumnSpan(separator, 8);
            Panel.SetZIndex(separator, -1);
            row.Children.Insert(0, separator);

            foreach (UIElement rowChild in row.Children)
            {
                if (rowChild is TextBlock text)
                {
                    text.FontSize = 11;
                    text.Margin = new Thickness(4, 5, 4, 5);
                    text.VerticalAlignment = VerticalAlignment.Center;
                }
                else if (rowChild is Button action)
                {
                    if (Application.Current.TryFindResource("Pda8ButtonStyle") is Style actionStyle)
                        action.Style = actionStyle;

                    action.MinHeight = 24;
                    action.Padding = new Thickness(7, 3, 7, 3);
                    action.Margin = new Thickness(3, 3, 3, 3);
                    action.FontSize = 10.5;
                }
            }
        }
    }
}
