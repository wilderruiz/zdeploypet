using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda8P62ContextHelpApplied;

    private void InitializePda8P62ContextHelp()
    {
        if (_pda8P62ContextHelpApplied) return;
        if (_profileStageToolbar?.Child is not StackPanel row) return;
        if (_profileGitSafetyButton is null || _profileDiscoveryButton is null) return;

        Button? gitInfo = FindAdjacentInfoButton(row, _profileGitSafetyButton);
        Button? discoveryInfo = FindAdjacentInfoButton(row, _profileDiscoveryButton);
        if (gitInfo is null || discoveryInfo is null) return;

        _pda8P62ContextHelpApplied = true;

        row.Children.Remove(gitInfo);
        row.Children.Remove(discoveryInfo);
        row.Children.Remove(_profileGitSafetyButton);
        row.Children.Remove(_profileDiscoveryButton);

        row.Children.Add(CreateContextualToolbarHelpHost(
            _profileGitSafetyButton,
            "Git safety",
            gitInfo,
            new Thickness(0, 0, 10, 0)));

        row.Children.Add(CreateContextualToolbarHelpHost(
            _profileDiscoveryButton,
            "Run discovery",
            discoveryInfo,
            new Thickness(0)));
    }

    private static Button? FindAdjacentInfoButton(StackPanel row, Button actionButton)
    {
        int index = row.Children.IndexOf(actionButton);
        if (index < 0 || index + 1 >= row.Children.Count) return null;
        return row.Children[index + 1] as Button;
    }

    private FrameworkElement CreateContextualToolbarHelpHost(
        Button actionButton,
        string shortLabel,
        Button detailInfoButton,
        Thickness margin)
    {
        actionButton.ToolTip = null;
        actionButton.Margin = new Thickness(0);

        Grid host = new()
        {
            Margin = margin,
            VerticalAlignment = VerticalAlignment.Center
        };

        Border flyoutChrome = new()
        {
            Background = AppSurfaceRaised,
            BorderBrush = AppBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 6, 7, 6)
        };

        StackPanel flyoutRow = new()
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        TextBlock label = new()
        {
            Text = shortLabel,
            Foreground = AppText,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 7, 0)
        };
        flyoutRow.Children.Add(label);

        detailInfoButton.Width = 20;
        detailInfoButton.Height = 20;
        detailInfoButton.Margin = new Thickness(0);
        detailInfoButton.Padding = new Thickness(0);
        detailInfoButton.HorizontalAlignment = HorizontalAlignment.Center;
        detailInfoButton.VerticalAlignment = VerticalAlignment.Center;
        AutomationProperties.SetName(detailInfoButton, $"More about {shortLabel}");
        flyoutRow.Children.Add(detailInfoButton);

        flyoutChrome.Child = flyoutRow;

        Popup popup = new()
        {
            Placement = PlacementMode.Bottom,
            PlacementTarget = actionButton,
            StaysOpen = true,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.Fade,
            VerticalOffset = 3,
            Child = flyoutChrome
        };

        ToolTip? detailToolTip = detailInfoButton.ToolTip as ToolTip;
        bool detailToolTipMouseOver = false;

        DispatcherTimer closeDelay = new()
        {
            Interval = TimeSpan.FromMilliseconds(180)
        };

        void OpenFlyout()
        {
            closeDelay.Stop();
            if (actionButton.IsEnabled)
                popup.IsOpen = true;
        }

        void ScheduleClose()
        {
            closeDelay.Stop();
            closeDelay.Start();
        }

        bool IsInsideHelpEnvelope()
        {
            return actionButton.IsMouseOver ||
                   flyoutChrome.IsMouseOver ||
                   actionButton.IsKeyboardFocusWithin ||
                   detailInfoButton.IsKeyboardFocusWithin ||
                   detailToolTipMouseOver ||
                   detailToolTip?.IsOpen == true;
        }

        closeDelay.Tick += (_, _) =>
        {
            closeDelay.Stop();
            if (!IsInsideHelpEnvelope())
                popup.IsOpen = false;
        };

        actionButton.MouseEnter += (_, _) => OpenFlyout();
        actionButton.MouseLeave += (_, _) => ScheduleClose();
        actionButton.GotKeyboardFocus += (_, _) => OpenFlyout();
        actionButton.LostKeyboardFocus += (_, _) => ScheduleClose();

        flyoutChrome.MouseEnter += (_, _) => closeDelay.Stop();
        flyoutChrome.MouseLeave += (_, _) => ScheduleClose();
        detailInfoButton.GotKeyboardFocus += (_, _) => OpenFlyout();
        detailInfoButton.LostKeyboardFocus += (_, _) => ScheduleClose();

        if (detailToolTip is not null)
        {
            detailToolTip.Opened += (_, _) => closeDelay.Stop();
            detailToolTip.Closed += (_, _) =>
            {
                detailToolTipMouseOver = false;
                ScheduleClose();
            };
            detailToolTip.MouseEnter += (_, _) =>
            {
                detailToolTipMouseOver = true;
                closeDelay.Stop();
            };
            detailToolTip.MouseLeave += (_, _) =>
            {
                detailToolTipMouseOver = false;
                ScheduleClose();
            };
        }

        actionButton.IsVisibleChanged += (_, _) =>
        {
            if (!actionButton.IsVisible)
            {
                if (detailToolTip is not null)
                    detailToolTip.IsOpen = false;
                popup.IsOpen = false;
            }
        };

        host.Children.Add(actionButton);
        host.Children.Add(popup);
        return host;
    }
}
