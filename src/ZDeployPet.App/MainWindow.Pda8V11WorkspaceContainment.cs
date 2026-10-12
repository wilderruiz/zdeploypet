using System.Windows;
using System.Windows.Controls;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda8V11WorkspaceContainmentApplied;

    private void ApplyPda8V11WorkspaceContainment()
    {
        if (_operatorAccessCard is null || _operatorAccessCard.Child is not Grid workspaceRoot)
            return;

        // The Home DiscoveryPanel is already bounded by the main-window * row.
        // Make the workspace card own the remaining Home height instead of
        // participating as another Auto-sized child. Rows 2+ are legacy slots
        // whose visible report controls have already been re-parented into the
        // DRY | LIVE | REPORTS workspace by PDA-8.
        if (DiscoveryPanel.RowDefinitions.Count > 1)
        {
            DiscoveryPanel.RowDefinitions[0].Height = GridLength.Auto;
            DiscoveryPanel.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            DiscoveryPanel.RowDefinitions[1].MinHeight = 0;

            for (int index = 2; index < DiscoveryPanel.RowDefinitions.Count; index++)
            {
                DiscoveryPanel.RowDefinitions[index].Height = new GridLength(0);
                DiscoveryPanel.RowDefinitions[index].MinHeight = 0;
            }
        }

        Grid.SetRow(_operatorAccessCard, 1);
        _operatorAccessCard.VerticalAlignment = VerticalAlignment.Stretch;

        // V6 owns the heading/tabs/content rows. Reassert the content row as
        // star-sized here because this is the outer containment invariant:
        // heading + tabs are Auto; selected content receives the remainder.
        if (workspaceRoot.RowDefinitions.Count >= 3)
        {
            workspaceRoot.RowDefinitions[0].Height = GridLength.Auto;
            workspaceRoot.RowDefinitions[1].Height = GridLength.Auto;
            workspaceRoot.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
            workspaceRoot.RowDefinitions[2].MinHeight = 0;
        }

        Grid? contentHost = workspaceRoot.Children
            .OfType<Grid>()
            .FirstOrDefault(child => Grid.GetRow(child) == 2);

        if (contentHost is not null)
        {
            contentHost.VerticalAlignment = VerticalAlignment.Stretch;
            contentHost.ClipToBounds = true;
        }

        if (_homeDryContent is not null)
            _homeDryContent.VerticalAlignment = VerticalAlignment.Stretch;
        if (_homeLiveContent is not null)
            _homeLiveContent.VerticalAlignment = VerticalAlignment.Stretch;

        EnsurePda8ReportsWorkspaceScrollViewer(contentHost);
        _pda8V11WorkspaceContainmentApplied = true;
    }

    private void EnsurePda8ReportsWorkspaceScrollViewer(Grid? contentHost)
    {
        if (_homeReportsContent is null || _homeReportsContent is ScrollViewer)
            return;

        if (_homeReportsContent.Parent is not Panel parent)
            return;

        FrameworkElement reportsBody = _homeReportsContent;
        int childIndex = parent.Children.IndexOf(reportsBody);
        if (childIndex < 0)
            return;

        parent.Children.RemoveAt(childIndex);

        ScrollViewer reportsScroll = new()
        {
            Content = reportsBody,
            Background = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            PanningMode = PanningMode.VerticalOnly,
            CanContentScroll = false,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        if (Application.Current.TryFindResource("Pda8WorkflowScrollViewerStyle") is Style scrollStyle)
            reportsScroll.Style = scrollStyle;

        Grid.SetRow(reportsScroll, Grid.GetRow(reportsBody));
        Grid.SetColumn(reportsScroll, Grid.GetColumn(reportsBody));
        Grid.SetRowSpan(reportsScroll, Grid.GetRowSpan(reportsBody));
        Grid.SetColumnSpan(reportsScroll, Grid.GetColumnSpan(reportsBody));

        parent.Children.Insert(childIndex, reportsScroll);
        _homeReportsContent = reportsScroll;

        if (contentHost is not null)
            contentHost.ClipToBounds = true;
    }
}
