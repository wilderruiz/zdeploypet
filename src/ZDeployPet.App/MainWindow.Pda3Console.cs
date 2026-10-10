using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private const double MinimumActivityPaneWidth = 220;
    private const double MinimumOperatorPaneWidth = 420;
    private const string RawDeploymentScriptCategory = "deploy_millenova.sh";

    private ColumnDefinition? _activityPaneColumn;
    private ColumnDefinition? _activitySplitterColumn;
    private GridSplitter? _activityPaneSplitter;
    private Border? _activityPane;
    private TextBox? _activityConsoleTextBox;
    private TextBlock? _activityProfileText;
    private TextBlock? _activitySessionText;
    private TextBlock? _activityStatusText;
    private bool _embeddedActivityHooked;
    private readonly List<ActivityRow> _embeddedActivityRows = [];
    private GridLength _rememberedActivityPaneWidth = new(430);

    private FrameworkElement BuildEmbeddedActivityConsole()
    {
        Grid root = new()
        {
            Margin = new Thickness(14),
            Background = FindBrush("AppBackgroundBrush", Brushes.Black),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        DockPanel header = new()
        {
            LastChildFill = true,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Button help = HelpTipFactory.Create(new HelpTipSpec(
            "Read the live ZDeployPet activity console.",
            "This pane follows application, profile, session, probe and Git-safety activity as it happens.",
            WhenToUse: "Keep it visible while operating ZDeployPet when you want immediate confirmation of what the app is doing.",
            WhatItDoes: "The pane reads the bounded in-memory shell activity stream and current profile/session state. Drag the splitter to resize it, or use View to hide/show it.",
            Safety: "Activity is sanitized before display. The console does not expose passwords, passphrases, private-key contents or deployment authority."));
        help.Margin = new Thickness(8, 0, 0, 0);
        help.HorizontalAlignment = HorizontalAlignment.Right;
        help.VerticalAlignment = VerticalAlignment.Top;
        DockPanel.SetDock(help, Dock.Right);
        header.Children.Add(help);

        StackPanel heading = new()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        heading.Children.Add(new TextBlock
        {
            Text = "Console / Activity",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Live sanitized activity from this ZDeployPet session.",
            Margin = new Thickness(0, 4, 0, 0),
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray),
            TextWrapping = TextWrapping.Wrap
        });
        header.Children.Add(heading);
        root.Children.Add(header);

        Border stateBorder = new()
        {
            Margin = new Thickness(0, 12, 0, 10),
            Padding = new Thickness(10, 6, 10, 6),
            Background = FindBrush("AppSurfaceBrush", Brushes.DarkSlateGray),
            BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        WrapPanel state = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        StackPanel profileState = new()
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 2, 24, 2)
        };
        profileState.Children.Add(new TextBlock
        {
            Text = "Profile",
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        _activityProfileText = new TextBlock
        {
            Text = "—",
            Margin = new Thickness(8, 0, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        profileState.Children.Add(_activityProfileText);
        state.Children.Add(profileState);

        StackPanel sessionState = new()
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 2, 0, 2)
        };
        sessionState.Children.Add(new TextBlock
        {
            Text = "Session",
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        _activitySessionText = new TextBlock
        {
            Text = "LOCKED",
            Margin = new Thickness(8, 0, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        sessionState.Children.Add(_activitySessionText);
        state.Children.Add(sessionState);

        stateBorder.Child = state;
        Grid.SetRow(stateBorder, 1);
        root.Children.Add(stateBorder);

        Border consoleBorder = new()
        {
            Background = new SolidColorBrush(Color.FromRgb(0x0B, 0x0D, 0x10)),
            BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray),
            BorderThickness = new Thickness(1),
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _activityConsoleTextBox = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            AcceptsTab = false,
            FontFamily = new FontFamily("Cascadia Mono, Consolas"),
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(0xD8, 0xDE, 0xE9)),
            Background = new SolidColorBrush(Color.FromRgb(0x0B, 0x0D, 0x10)),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(10),
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Top,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            SelectionBrush = FindBrush("AppSelectionBrush", Brushes.DimGray)
        };
        consoleBorder.Child = _activityConsoleTextBox;
        Grid.SetRow(consoleBorder, 2);
        root.Children.Add(consoleBorder);

        Grid footer = new()
        {
            Margin = new Thickness(0, 10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        footer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        footer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _activityStatusText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 6)
        };
        footer.Children.Add(_activityStatusText);

        WrapPanel footerActions = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Button copyAll = new()
        {
            Content = "Copy all",
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 2, 8, 2),
            MinWidth = 72
        };
        copyAll.Click += EmbeddedCopyAll_Click;
        footerActions.Children.Add(copyAll);

        Button copySelection = new()
        {
            Content = "Copy selection",
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 2, 0, 2),
            MinWidth = 96
        };
        copySelection.Click += EmbeddedCopySelection_Click;
        footerActions.Children.Add(copySelection);

        Grid.SetRow(footerActions, 1);
        footer.Children.Add(footerActions);

        Grid.SetRow(footer, 3);
        root.Children.Add(footer);

        // WPF can otherwise keep Auto-sized descendants at their previous desired
        // width after a GridSplitter move. Constrain every responsive surface to the
        // live console viewport so header, state strip, console and footer all obey
        // the pane edge immediately.
        root.SizeChanged += (_, e) =>
        {
            double available = Math.Max(0, e.NewSize.Width);
            header.Width = available;
            header.MaxWidth = available;
            heading.MaxWidth = Math.Max(0, available - help.ActualWidth - 16);
            stateBorder.Width = available;
            stateBorder.MaxWidth = available;
            state.Width = Math.Max(0, available - stateBorder.Padding.Left - stateBorder.Padding.Right - 2);
            state.MaxWidth = state.Width;
            consoleBorder.Width = available;
            consoleBorder.MaxWidth = available;
            footer.Width = available;
            footer.MaxWidth = available;
            footerActions.Width = available;
            footerActions.MaxWidth = available;
            footerActions.HorizontalAlignment = available < 360
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Right;

            if (_activityConsoleTextBox is not null)
            {
                double textWidth = Math.Max(80, available - 2);
                _activityConsoleTextBox.Width = textWidth;
                _activityConsoleTextBox.MaxWidth = textWidth;
            }
        };

        if (!_embeddedActivityHooked)
        {
            _embeddedActivityHooked = true;
            foreach (ShellActivityEntry entry in ShellRuntime.Activity.Snapshot())
                _embeddedActivityRows.Add(ToEmbeddedActivityRow(entry));
            ShellRuntime.Activity.EntryAdded += EmbeddedActivity_EntryAdded;
            ShellRuntime.State.PropertyChanged += EmbeddedActivity_StateChanged;
        }

        RefreshEmbeddedActivityConsole();
        RefreshEmbeddedActivityState();
        return root;
    }

    private void EmbeddedActivity_EntryAdded(object? sender, ShellActivityEntry entry)
    {
        Dispatcher.InvokeAsync(() =>
        {
            _embeddedActivityRows.Add(ToEmbeddedActivityRow(entry));
            RefreshEmbeddedActivityConsole();
        });
    }

    private void EmbeddedActivity_StateChanged(object? sender, PropertyChangedEventArgs e) =>
        Dispatcher.InvokeAsync(RefreshEmbeddedActivityState);

    private void RefreshEmbeddedActivityConsole()
    {
        if (_activityConsoleTextBox is null) return;

        StringBuilder text = new();
        foreach (ActivityRow row in _embeddedActivityRows)
            text.AppendLine(FormatEmbeddedActivityRow(row));

        _activityConsoleTextBox.Text = text.ToString().TrimEnd();
        _activityConsoleTextBox.ScrollToEnd();
        RefreshEmbeddedActivityStatus();
    }

    private void RefreshEmbeddedActivityState()
    {
        if (_activityProfileText is null || _activitySessionText is null) return;
        _activityProfileText.Text = string.IsNullOrWhiteSpace(ShellRuntime.State.ProfileName) ? "—" : ShellRuntime.State.ProfileName;
        _activitySessionText.Text = ShellRuntime.State.SessionExpiresAtUtc is DateTimeOffset expires
            ? $"{ShellRuntime.State.SessionState.ToString().ToUpperInvariant()} until {expires.ToLocalTime():g}"
            : ShellRuntime.State.SessionState.ToString().ToUpperInvariant();
    }

    private void RefreshEmbeddedActivityStatus()
    {
        if (_activityStatusText is not null)
            _activityStatusText.Text = $"{_embeddedActivityRows.Count} activit{(_embeddedActivityRows.Count == 1 ? "y" : "ies")}";
    }

    private void EmbeddedCopySelection_Click(object sender, RoutedEventArgs e)
    {
        if (_activityConsoleTextBox is null || string.IsNullOrEmpty(_activityConsoleTextBox.SelectedText))
        {
            if (_activityStatusText is not null) _activityStatusText.Text = "Select console text first.";
            _activityConsoleTextBox?.Focus();
            return;
        }

        Clipboard.SetText(_activityConsoleTextBox.SelectedText);
        if (_activityStatusText is not null) _activityStatusText.Text = "Copied selection.";
    }

    private void EmbeddedCopyAll_Click(object sender, RoutedEventArgs e)
    {
        if (_embeddedActivityRows.Count == 0)
        {
            if (_activityStatusText is not null) _activityStatusText.Text = "No activity to copy yet.";
            return;
        }

        Clipboard.SetText(string.Join(Environment.NewLine, _embeddedActivityRows.Select(FormatEmbeddedActivityRow)));
        if (_activityStatusText is not null) _activityStatusText.Text = $"Copied all {_embeddedActivityRows.Count} activities.";
    }

    private static ActivityRow ToEmbeddedActivityRow(ShellActivityEntry entry) => new(
        entry.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
        entry.Level.ToString().ToUpperInvariant(),
        entry.Category,
        entry.Message);

    private static string FormatEmbeddedActivityRow(ActivityRow row)
    {
        // Deployment-script output is already a console stream. Render it as the script
        // printed it instead of wrapping every line in ZDeployPet's activity metadata.
        // The message has still passed through ShellRuntime sanitization before arriving here.
        if (string.Equals(row.Category, RawDeploymentScriptCategory, StringComparison.OrdinalIgnoreCase))
            return row.Message;

        return $"{row.LocalTime} | {row.Level} | {row.Category} | {row.Message}";
    }

    private void ToggleEmbeddedActivityPane()
    {
        if (_activityPaneColumn is null || _activitySplitterColumn is null || _activityPane is null || _activityPaneSplitter is null)
            return;

        bool visible = _activityPane.Visibility == Visibility.Visible;
        if (visible)
        {
            if (_activityPaneColumn.ActualWidth > 0)
                _rememberedActivityPaneWidth = new GridLength(Math.Max(MinimumActivityPaneWidth, _activityPaneColumn.ActualWidth));
            _activityPaneColumn.MinWidth = 0;
            _activityPane.Visibility = Visibility.Collapsed;
            _activityPaneSplitter.Visibility = Visibility.Collapsed;
            _activityPaneColumn.Width = new GridLength(0);
            _activitySplitterColumn.Width = new GridLength(0);
            ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Shell", "Console / Activity pane hidden.");
        }
        else
        {
            _activityPaneColumn.MinWidth = MinimumActivityPaneWidth;
            _activityPaneColumn.Width = _rememberedActivityPaneWidth;
            _activitySplitterColumn.Width = new GridLength(5);
            _activityPaneSplitter.Visibility = Visibility.Visible;
            _activityPane.Visibility = Visibility.Visible;
            ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Shell", "Console / Activity pane shown.");
        }
    }
}
