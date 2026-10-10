using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ZDeployPet.App;

public sealed record ActivityRow(
    string LocalTime,
    string Level,
    string Category,
    string Message);

public partial class ActivityWindow : Window
{
    private readonly List<ActivityRow> _rows = [];

    public ActivityWindow()
    {
        InitializeComponent();

        Button selectionHelp = HelpTipFactory.Create(new HelpTipSpec(
            "Copy the text currently selected in the activity console.",
            "The copied text comes from the already-sanitized console output shown in this window.",
            WhenToUse: "Drag across the console to select only the lines or text you want to share, then use Copy selection.",
            Safety: "ZDeployPet redacts common secret-bearing assignments and private-key markers before entries reach this view, but you should still review copied text before sharing it externally."));
        CopySelectedButton.ToolTip = selectionHelp.ToolTip;

        Button allHelp = HelpTipFactory.Create(new HelpTipSpec(
            "Copy the complete visible activity console.",
            "This copies the bounded in-memory ZDeployPet activity buffer in timestamp order.",
            WhenToUse: "Use this when a longer sequence of profile, session, target-probe, or application lifecycle events is useful for debugging.",
            Safety: "The stream is sanitized before display and copy. It intentionally does not expose private-key contents, passphrases, passwords, agent sockets, or deployment authorization."));
        CopyAllButton.ToolTip = allHelp.ToolTip;

        foreach (ShellActivityEntry entry in ShellRuntime.Activity.Snapshot())
            _rows.Add(ToRow(entry));

        RefreshConsole();
        ShellRuntime.Activity.EntryAdded += Activity_EntryAdded;
        ShellRuntime.State.PropertyChanged += State_PropertyChanged;
        RefreshStateHeader();
        RefreshStatus();
    }

    private void Activity_EntryAdded(object? sender, ShellActivityEntry entry)
    {
        Dispatcher.InvokeAsync(() =>
        {
            _rows.Add(ToRow(entry));
            if (ConsoleTextBox.Text.Length > 0)
                ConsoleTextBox.AppendText(Environment.NewLine);
            ConsoleTextBox.AppendText(FormatRow(_rows[^1]));
            ConsoleTextBox.ScrollToEnd();
            RefreshStatus();
        });
    }

    private void State_PropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        Dispatcher.InvokeAsync(RefreshStateHeader);

    private void RefreshStateHeader()
    {
        ProfileText.Text = string.IsNullOrWhiteSpace(ShellRuntime.State.ProfileName)
            ? "—"
            : ShellRuntime.State.ProfileName;

        SessionText.Text = ShellRuntime.State.SessionExpiresAtUtc is DateTimeOffset expires
            ? $"{ShellRuntime.State.SessionState.ToString().ToUpperInvariant()} until {expires.ToLocalTime():g}"
            : ShellRuntime.State.SessionState.ToString().ToUpperInvariant();
    }

    private void RefreshConsole()
    {
        ConsoleTextBox.Text = FormatRows(_rows);
        ConsoleTextBox.ScrollToEnd();
    }

    private void RefreshStatus() =>
        StatusText.Text = $"{_rows.Count} activit{(_rows.Count == 1 ? "y" : "ies")} in memory.";

    private void CopySelected_Click(object sender, RoutedEventArgs e)
    {
        string selected = ConsoleTextBox.SelectedText;
        if (string.IsNullOrEmpty(selected))
        {
            StatusText.Text = "Select text in the console first.";
            ConsoleTextBox.Focus();
            return;
        }

        Clipboard.SetText(selected);
        StatusText.Text = "Copied selected console text.";
    }

    private void CopyAll_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ConsoleTextBox.Text))
        {
            StatusText.Text = "There is no activity to copy yet.";
            return;
        }

        Clipboard.SetText(ConsoleTextBox.Text);
        StatusText.Text = $"Copied all {_rows.Count} activities.";
    }

    private static string FormatRows(IEnumerable<ActivityRow> rows)
    {
        StringBuilder text = new();
        foreach (ActivityRow row in rows)
            text.AppendLine(FormatRow(row));
        return text.ToString().TrimEnd();
    }

    private static string FormatRow(ActivityRow row) =>
        $"{row.LocalTime}  [{row.Level,-7}]  {row.Category,-14}  {row.Message}";

    private static ActivityRow ToRow(ShellActivityEntry entry) => new(
        entry.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
        entry.Level.ToString().ToUpperInvariant(),
        entry.Category,
        entry.Message);

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_Closed(object? sender, EventArgs e)
    {
        ShellRuntime.Activity.EntryAdded -= Activity_EntryAdded;
        ShellRuntime.State.PropertyChanged -= State_PropertyChanged;
    }
}
