using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZDeployPet.App;

public partial class MainWindow
{
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
            Background = FindBrush("AppBackgroundBrush", Brushes.Black)
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Grid header = new();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        StackPanel heading = new();
        heading.Children.Add(new TextBlock
        {
            Text = "Console / Activity",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Live sanitized activity from this ZDeployPet session.",
            Margin = new Thickness(0, 4, 0, 0),
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray),
            TextWrapping = TextWrapping.Wrap
        });
        header.Children.Add(heading);

        Button help = HelpTipFactory.Create(new HelpTipSpec(
            "Read the live ZDeployPet activity console.",
            "This pane follows application, profile, session, probe and Git-safety activity as it happens.",
            WhenToUse: "Keep it visible while operating ZDeployPet when you want immediate confirmation of what the app is doing.",
            WhatItDoes: "The pane reads the bounded in-memory shell activity stream and current profile/session state. Drag the splitter to resize it, or use View to hide/show it.",
            Safety: "Activity is sanitized before display. The console does not expose passwords, passphrases, private-key contents or deployment authority."));
        help.Margin = new Thickness(8, 0, 0, 0);
        help.HorizontalAlignment = HorizontalAlignment.Right;
        help.VerticalAlignment = VerticalAlignment.Top;
        Grid.SetColumn(help, 1);
        header.Children.Add(help);
        root.Children.Add(header);

        Border stateBorder = new()
        {
            Margin = new Thickness(0, 12, 0, 10),
            Padding = new Thickness(10),
            Background = FindBrush("AppSurfaceBrush", Brushes.DarkSlateGray),
            BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray),
            BorderThickness = new Thickness(1)
        };
        Grid state = new();
        state.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        state.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        state.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        state.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        state.Children.Add(new TextBlock { Text = "Profile", FontWeight = FontWeights.SemiBold });
        _activityProfileText = new TextBlock { Text = "—", Margin = new Thickness(8, 0, 16, 0), TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(_activityProfileText, 1);
        state.Children.Add(_activityProfileText);
        TextBlock sessionLabel = new() { Text = "Session", FontWeight = FontWeights.SemiBold };
        Grid.SetColumn(sessionLabel, 2);
        state.Children.Add(sessionLabel);
        _activitySessionText = new TextBlock { Text = "LOCKED", Margin = new Thickness(8, 0, 0, 0), TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(_activitySessionText, 3);
        state.Children.Add(_activitySessionText);
        stateBorder.Child = state;
        Grid.SetRow(stateBorder, 1);
        root.Children.Add(stateBorder);

        Border consoleBorder = new()
        {
            Background = new SolidColorBrush(Color.FromRgb(0x0B, 0x0D, 0x10)),
            BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray),
            BorderThickness = new Thickness(1)
        };
        _activityConsoleTextBox = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            FontFamily = new FontFamily("Cascadia Mono, Consolas"),
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(0xD8, 0xDE, 0xE9)),
            Background = new SolidColorBrush(Color.FromRgb(0x0B, 0x0D, 0x10)),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(10),
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            SelectionBrush = FindBrush("AppSelectionBrush", Brushes.DimGray)
        };
        _activityConsoleTextBox.SizeChanged += EmbeddedConsoleTextBox_SizeChanged;
        consoleBorder.Child = _activityConsoleTextBox;
        Grid.SetRow(consoleBorder, 2);
        root.Children.Add(consoleBorder);

        DockPanel footer = new() { Margin = new Thickness(0, 10, 0, 0), LastChildFill = false };
        _activityStatusText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        };
        DockPanel.SetDock(_activityStatusText, Dock.Left);
        footer.Children.Add(_activityStatusText);

        Button copySelection = new() { Content = "Copy selection", Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(8, 0, 0, 0) };
        copySelection.Click += EmbeddedCopySelection_Click;
        DockPanel.SetDock(copySelection, Dock.Right);
        footer.Children.Add(copySelection);

        Button copyAll = new() { Content = "Copy all", Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(8, 0, 0, 0) };
        copyAll.Click += EmbeddedCopyAll_Click;
        DockPanel.SetDock(copyAll, Dock.Right);
        footer.Children.Add(copyAll);

        Grid.SetRow(footer, 3);
        root.Children.Add(footer);

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

    private void EmbeddedConsoleTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) < 2) return;
        RefreshEmbeddedActivityConsole();
    }

    private void RefreshEmbeddedActivityConsole()
    {
        if (_activityConsoleTextBox is null) return;

        double wrapWidth = GetEmbeddedConsoleWrapPixelWidth();
        StringBuilder text = new();
        foreach (ActivityRow row in _embeddedActivityRows)
        {
            AppendWrappedConsoleLine(text, FormatEmbeddedActivityRow(row), wrapWidth);
            text.AppendLine();
        }

        _activityConsoleTextBox.Text = text.ToString().TrimEnd();
        _activityConsoleTextBox.ScrollToEnd();
        RefreshEmbeddedActivityStatus();
    }

    private double GetEmbeddedConsoleWrapPixelWidth()
    {
        if (_activityConsoleTextBox is null || _activityConsoleTextBox.ActualWidth <= 0)
            return 500;

        // Reserve the TextBox padding plus a little room for the vertical scrollbar.
        // Wrapping is measured using the actual console font and DPI below, so it
        // stays correct when the operator drags the splitter or Windows scaling changes.
        return Math.Max(120, _activityConsoleTextBox.ActualWidth - 38);
    }

    private void AppendWrappedConsoleLine(StringBuilder output, string line, double maxPixelWidth)
    {
        if (_activityConsoleTextBox is null)
        {
            output.Append(line);
            return;
        }

        string[] words = line.TrimEnd().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return;

        const string continuationIndent = "    ";
        string current = string.Empty;
        bool continuation = false;

        foreach (string word in words)
        {
            string prefix = continuation ? continuationIndent : string.Empty;
            string candidate = current.Length == 0 ? prefix + word : current + " " + word;

            if (MeasureConsoleTextWidth(candidate) <= maxPixelWidth)
            {
                current = candidate;
                continue;
            }

            if (current.Length > 0)
            {
                output.AppendLine(current);
                continuation = true;
                current = continuationIndent + word;
            }
            else
            {
                AppendLongConsoleToken(output, word, maxPixelWidth, continuationIndent, ref continuation, ref current);
            }

            if (MeasureConsoleTextWidth(current) > maxPixelWidth)
                AppendLongConsoleToken(output, current.TrimStart(), maxPixelWidth, continuationIndent, ref continuation, ref current);
        }

        if (current.Length > 0)
            output.Append(current);
    }

    private void AppendLongConsoleToken(
        StringBuilder output,
        string token,
        double maxPixelWidth,
        string continuationIndent,
        ref bool continuation,
        ref string current)
    {
        string remaining = token;
        while (remaining.Length > 0)
        {
            string prefix = continuation ? continuationIndent : string.Empty;
            int take = FindFittingConsolePrefixLength(prefix, remaining, maxPixelWidth);
            if (take >= remaining.Length)
            {
                current = prefix + remaining;
                return;
            }

            output.AppendLine(prefix + remaining[..take]);
            remaining = remaining[take..];
            continuation = true;
        }

        current = string.Empty;
    }

    private int FindFittingConsolePrefixLength(string prefix, string value, double maxPixelWidth)
    {
        int low = 1;
        int high = value.Length;
        int best = 1;

        while (low <= high)
        {
            int mid = low + ((high - low) / 2);
            if (MeasureConsoleTextWidth(prefix + value[..mid]) <= maxPixelWidth)
            {
                best = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return Math.Max(1, best);
    }

    private double MeasureConsoleTextWidth(string value)
    {
        if (_activityConsoleTextBox is null || string.IsNullOrEmpty(value)) return 0;

        Typeface typeface = new(
            _activityConsoleTextBox.FontFamily,
            _activityConsoleTextBox.FontStyle,
            _activityConsoleTextBox.FontWeight,
            _activityConsoleTextBox.FontStretch);
        double pixelsPerDip = VisualTreeHelper.GetDpi(_activityConsoleTextBox).PixelsPerDip;
        FormattedText formatted = new(
            value,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            _activityConsoleTextBox.FontSize,
            Brushes.White,
            pixelsPerDip);
        return formatted.WidthIncludingTrailingWhitespace;
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
        if (_activityConsoleTextBox is null || string.IsNullOrWhiteSpace(_activityConsoleTextBox.Text))
        {
            if (_activityStatusText is not null) _activityStatusText.Text = "No activity to copy yet.";
            return;
        }
        Clipboard.SetText(_activityConsoleTextBox.Text);
        if (_activityStatusText is not null) _activityStatusText.Text = $"Copied all {_embeddedActivityRows.Count} activities.";
    }

    private static ActivityRow ToEmbeddedActivityRow(ShellActivityEntry entry) => new(
        entry.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
        entry.Level.ToString().ToUpperInvariant(),
        entry.Category,
        entry.Message);

    private static string FormatEmbeddedActivityRow(ActivityRow row) =>
        $"{row.LocalTime}  [{row.Level,-7}]  {row.Category,-12}  {row.Message}";

    private void ToggleEmbeddedActivityPane()
    {
        if (_activityPaneColumn is null || _activitySplitterColumn is null || _activityPane is null || _activityPaneSplitter is null)
            return;

        bool visible = _activityPane.Visibility == Visibility.Visible;
        if (visible)
        {
            if (_activityPaneColumn.ActualWidth > 0)
                _rememberedActivityPaneWidth = new GridLength(Math.Max(300, _activityPaneColumn.ActualWidth));
            _activityPaneColumn.MinWidth = 0;
            _activityPane.Visibility = Visibility.Collapsed;
            _activityPaneSplitter.Visibility = Visibility.Collapsed;
            _activityPaneColumn.Width = new GridLength(0);
            _activitySplitterColumn.Width = new GridLength(0);
            ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Shell", "Console / Activity pane hidden.");
        }
        else
        {
            _activityPaneColumn.MinWidth = 300;
            _activityPaneColumn.Width = _rememberedActivityPaneWidth;
            _activitySplitterColumn.Width = new GridLength(5);
            _activityPaneSplitter.Visibility = Visibility.Visible;
            _activityPane.Visibility = Visibility.Visible;
            ShellRuntime.Activity.Add(ShellActivityLevel.Info, "Shell", "Console / Activity pane shown.");
        }
    }
}
