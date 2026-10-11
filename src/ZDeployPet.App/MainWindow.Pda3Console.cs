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
    private const int MaximumEmbeddedActivityRows = 500;
    private const string DryRunActivityCategory = "Dry run";
    private const string LiveDeployActivityCategory = "Live deploy";
    private const string RawDryScriptCategory = "deploy_millenova.sh";
    private const string RawLiveScriptCategory = "deploy_millenova.live.sh";

    private enum EmbeddedConsoleChannel
    {
        DryRun,
        LiveDeploy,
        ZDeployPet,
        Reports
    }

    private ColumnDefinition? _activityPaneColumn;
    private ColumnDefinition? _activitySplitterColumn;
    private GridSplitter? _activityPaneSplitter;
    private Border? _activityPane;
    private TabControl? _activityConsoleTabs;
    private TextBox? _activityConsoleTextBox;
    private TextBlock? _activityProfileText;
    private TextBlock? _activitySessionText;
    private TextBlock? _activityStatusText;
    private bool _embeddedActivityHooked;
    private readonly List<ActivityRow> _embeddedActivityRows = [];
    private GridLength _rememberedActivityPaneWidth = new(430);
    private string _embeddedReportText = string.Empty;
    private string _embeddedReportStatus = "No report selected. Choose Summary or Full log from Latest deployment report or Deployment history.";

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
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        DockPanel header = new()
        {
            LastChildFill = true,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Button help = HelpTipFactory.Create(new HelpTipSpec(
            "Read current deployment output, ZDeployPet activity and trusted reports.",
            "Dry Run and Live deploy retain only their latest execution. ZDeployPet contains bounded app activity. Reports is one shared read-only viewer reused for Summary and Full log artifacts.",
            WhenToUse: "Use Dry Run or Live deploy while an execution is active, ZDeployPet for application diagnostics, and Reports when inspecting the latest or historical reporter evidence.",
            WhatItDoes: "The four permanent tabs avoid accumulating runs, report tabs or popup viewers. Historical deployment evidence remains in Deployment history and opens into the shared Reports tab.",
            Safety: "Output and report content are observational/read-only. Report reads retain PDA-4 canonical-path and size validation and never grant deployment authority."));
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
            Text = "Output / Reports",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Latest Dry Run / Live deploy output, bounded ZDeployPet activity, and one shared report viewer.",
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

        _activityConsoleTabs = new TabControl
        {
            Margin = new Thickness(0, 0, 0, 6),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            SelectedIndex = 0
        };
        _activityConsoleTabs.Items.Add(BuildConsoleTab("Dry Run", EmbeddedConsoleChannel.DryRun, Brushes.Goldenrod));
        _activityConsoleTabs.Items.Add(BuildConsoleTab("Live deploy", EmbeddedConsoleChannel.LiveDeploy, Brushes.LightCoral));
        _activityConsoleTabs.Items.Add(BuildConsoleTab("ZDeployPet", EmbeddedConsoleChannel.ZDeployPet, Brushes.LightSteelBlue));
        _activityConsoleTabs.Items.Add(BuildConsoleTab("Reports", EmbeddedConsoleChannel.Reports, Brushes.LightSeaGreen));
        _activityConsoleTabs.SelectionChanged += (_, _) => RefreshEmbeddedActivityConsole();
        Grid.SetRow(_activityConsoleTabs, 2);
        root.Children.Add(_activityConsoleTabs);

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
        Grid.SetRow(consoleBorder, 3);
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

        Grid.SetRow(footer, 4);
        root.Children.Add(footer);

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
            if (_activityConsoleTabs is not null)
            {
                _activityConsoleTabs.Width = available;
                _activityConsoleTabs.MaxWidth = available;
            }
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
                AddEmbeddedActivityEntry(entry, selectChannel: false);
            ShellRuntime.Activity.EntryAdded += EmbeddedActivity_EntryAdded;
            ShellRuntime.State.PropertyChanged += EmbeddedActivity_StateChanged;
        }

        RefreshEmbeddedActivityConsole();
        RefreshEmbeddedActivityState();
        return root;
    }

    private static TabItem BuildConsoleTab(string header, EmbeddedConsoleChannel channel, Brush accent)
        => new()
        {
            Header = header,
            Tag = channel,
            Foreground = accent
        };

    private void EmbeddedActivity_EntryAdded(object? sender, ShellActivityEntry entry)
    {
        Dispatcher.InvokeAsync(() =>
        {
            AddEmbeddedActivityEntry(entry, selectChannel: true);
            RefreshEmbeddedActivityConsole();
        });
    }

    private void AddEmbeddedActivityEntry(ShellActivityEntry entry, bool selectChannel)
    {
        bool startsDryRun =
            string.Equals(entry.Category, DryRunActivityCategory, StringComparison.OrdinalIgnoreCase) &&
            entry.Message.StartsWith("Dry run started", StringComparison.OrdinalIgnoreCase);
        bool startsLiveDeploy =
            string.Equals(entry.Category, LiveDeployActivityCategory, StringComparison.OrdinalIgnoreCase) &&
            entry.Message.StartsWith("Live deployment started", StringComparison.OrdinalIgnoreCase);

        if (startsDryRun)
            _embeddedActivityRows.RemoveAll(row => IsRowInChannel(row, EmbeddedConsoleChannel.DryRun));
        else if (startsLiveDeploy)
            _embeddedActivityRows.RemoveAll(row => IsRowInChannel(row, EmbeddedConsoleChannel.LiveDeploy));

        _embeddedActivityRows.Add(ToEmbeddedActivityRow(entry));
        if (_embeddedActivityRows.Count > MaximumEmbeddedActivityRows)
            _embeddedActivityRows.RemoveRange(0, _embeddedActivityRows.Count - MaximumEmbeddedActivityRows);

        if (!selectChannel || _activityConsoleTabs is null)
            return;

        if (startsDryRun)
            _activityConsoleTabs.SelectedIndex = 0;
        else if (startsLiveDeploy)
            _activityConsoleTabs.SelectedIndex = 1;
    }

    private void EmbeddedActivity_StateChanged(object? sender, PropertyChangedEventArgs e) =>
        Dispatcher.InvokeAsync(RefreshEmbeddedActivityState);

    private void RefreshEmbeddedActivityConsole()
    {
        if (_activityConsoleTextBox is null) return;

        EmbeddedConsoleChannel channel = GetSelectedConsoleChannel();
        if (channel == EmbeddedConsoleChannel.Reports)
        {
            _activityConsoleTextBox.Text = _embeddedReportText;
            _activityConsoleTextBox.ScrollToHome();
            if (_activityStatusText is not null)
                _activityStatusText.Text = _embeddedReportStatus;
            return;
        }

        List<ActivityRow> rows = _embeddedActivityRows.Where(row => IsRowInChannel(row, channel)).ToList();
        StringBuilder text = new();
        foreach (ActivityRow row in rows)
            text.AppendLine(channel == EmbeddedConsoleChannel.ZDeployPet
                ? FormatZDeployPetActivityRow(row)
                : FormatEmbeddedActivityRow(row));

        _activityConsoleTextBox.Text = text.ToString().TrimEnd();
        _activityConsoleTextBox.ScrollToEnd();
        RefreshEmbeddedActivityStatus(rows.Count, channel);
    }

    private EmbeddedConsoleChannel GetSelectedConsoleChannel()
    {
        if (_activityConsoleTabs?.SelectedItem is TabItem tab && tab.Tag is EmbeddedConsoleChannel channel)
            return channel;

        return EmbeddedConsoleChannel.DryRun;
    }

    private static bool IsRowInChannel(ActivityRow row, EmbeddedConsoleChannel channel)
    {
        bool dry = string.Equals(row.Category, DryRunActivityCategory, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(row.Category, RawDryScriptCategory, StringComparison.OrdinalIgnoreCase);
        bool live = string.Equals(row.Category, LiveDeployActivityCategory, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(row.Category, RawLiveScriptCategory, StringComparison.OrdinalIgnoreCase);

        return channel switch
        {
            EmbeddedConsoleChannel.DryRun => dry,
            EmbeddedConsoleChannel.LiveDeploy => live,
            EmbeddedConsoleChannel.ZDeployPet => !dry && !live,
            EmbeddedConsoleChannel.Reports => false,
            _ => false
        };
    }

    private void RefreshEmbeddedActivityState()
    {
        if (_activityProfileText is null || _activitySessionText is null) return;
        _activityProfileText.Text = string.IsNullOrWhiteSpace(ShellRuntime.State.ProfileName) ? "—" : ShellRuntime.State.ProfileName;
        _activitySessionText.Text = ShellRuntime.State.SessionExpiresAtUtc is DateTimeOffset expires
            ? $"{ShellRuntime.State.SessionState.ToString().ToUpperInvariant()} until {expires.ToLocalTime():g}"
            : ShellRuntime.State.SessionState.ToString().ToUpperInvariant();
    }

    private void RefreshEmbeddedActivityStatus(int visibleCount, EmbeddedConsoleChannel channel)
    {
        if (_activityStatusText is null) return;

        string label = channel switch
        {
            EmbeddedConsoleChannel.DryRun => "dry-run",
            EmbeddedConsoleChannel.LiveDeploy => "live-deploy",
            EmbeddedConsoleChannel.ZDeployPet => "ZDeployPet",
            _ => "report"
        };
        _activityStatusText.Text = $"{visibleCount} {label} line{(visibleCount == 1 ? string.Empty : "s")}";
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
        if (_activityConsoleTextBox is null || string.IsNullOrWhiteSpace(_activityConsoleTextBox.Text))
        {
            if (_activityStatusText is not null) _activityStatusText.Text = "No text in this view yet.";
            return;
        }

        Clipboard.SetText(_activityConsoleTextBox.Text);
        EmbeddedConsoleChannel channel = GetSelectedConsoleChannel();
        if (_activityStatusText is not null)
        {
            _activityStatusText.Text = channel == EmbeddedConsoleChannel.Reports
                ? "Copied the complete displayed report artifact."
                : "Copied all displayed console lines.";
        }
    }

    private static ActivityRow ToEmbeddedActivityRow(ShellActivityEntry entry) => new(
        entry.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
        entry.Level.ToString().ToUpperInvariant(),
        entry.Category,
        entry.Message);

    private static string FormatEmbeddedActivityRow(ActivityRow row)
    {
        if (string.Equals(row.Category, RawDryScriptCategory, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(row.Category, RawLiveScriptCategory, StringComparison.OrdinalIgnoreCase))
            return row.Message;

        return $"{row.LocalTime} | {row.Level} | {row.Category} | {row.Message}";
    }

    private static string FormatZDeployPetActivityRow(ActivityRow row)
    {
        string symbol = row.Level switch
        {
            "SUCCESS" => "✓",
            "WARNING" => "!",
            "ERROR" => "×",
            _ => "›"
        };

        return $"{row.LocalTime}  {symbol}  {row.Message}";
    }

    private void ShowEmbeddedReportArtifact(string status, string content)
    {
        _embeddedReportStatus = status;
        _embeddedReportText = content ?? string.Empty;
        EnsureEmbeddedActivityPaneVisible();

        if (_activityConsoleTabs is not null)
            _activityConsoleTabs.SelectedIndex = 3;

        RefreshEmbeddedActivityConsole();
        _activityConsoleTextBox?.Focus();
    }

    private void EnsureEmbeddedActivityPaneVisible()
    {
        if (_activityPaneColumn is null || _activitySplitterColumn is null || _activityPane is null || _activityPaneSplitter is null)
            return;

        if (_activityPane.Visibility == Visibility.Visible)
            return;

        _activityPaneColumn.MinWidth = MinimumActivityPaneWidth;
        _activityPaneColumn.Width = _rememberedActivityPaneWidth;
        _activitySplitterColumn.Width = new GridLength(5);
        _activityPaneSplitter.Visibility = Visibility.Visible;
        _activityPane.Visibility = Visibility.Visible;
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
            ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Shell", "Output / Reports pane hidden.");
        }
        else
        {
            EnsureEmbeddedActivityPaneVisible();
            ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Shell", "Output / Reports pane shown.");
        }
    }
}
