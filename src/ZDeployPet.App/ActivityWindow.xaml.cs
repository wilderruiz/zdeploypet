using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;

namespace ZDeployPet.App;

public sealed record ActivityRow(
    string LocalTime,
    string Level,
    string Category,
    string Message);

public partial class ActivityWindow : Window
{
    private readonly ObservableCollection<ActivityRow> _rows = [];

    public ActivityWindow()
    {
        InitializeComponent();
        ActivityList.ItemsSource = _rows;

        HelpTipFactory.AttachToButton(
            CopySelectedButton,
            new HelpTipSpec(
                "Copy only the activity rows you selected.",
                "The copied text comes from the already-sanitized activity stream shown in this window.",
                WhenToUse: "Use this when you want to paste a focused diagnostic into an issue, note, or debugging conversation.",
                Safety: "ZDeployPet redacts common secret-bearing assignments and private-key markers before entries reach this view, but you should still review copied text before sharing it externally."));

        HelpTipFactory.AttachToButton(
            CopyAllButton,
            new HelpTipSpec(
                "Copy the complete visible activity history.",
                "This copies the bounded in-memory ZDeployPet activity buffer in timestamp order.",
                WhenToUse: "Use this when a longer sequence of profile, session, target-probe, or application lifecycle events is useful for debugging.",
                Safety: "The stream is sanitized before display and copy. It intentionally does not expose private-key contents, passphrases, passwords, agent sockets, or deployment authorization."));

        foreach (ShellActivityEntry entry in ShellRuntime.Activity.Snapshot())
            _rows.Add(ToRow(entry));

        ShellRuntime.Activity.EntryAdded += Activity_EntryAdded;
        ShellRuntime.State.PropertyChanged += State_PropertyChanged;
        RefreshStateHeader();
        StatusText.Text = $"{_rows.Count} activit{(_rows.Count == 1 ? "y" : "ies")} in memory.";
    }

    private void Activity_EntryAdded(object? sender, ShellActivityEntry entry)
    {
        Dispatcher.InvokeAsync(() =>
        {
            _rows.Add(ToRow(entry));
            ActivityList.ScrollIntoView(_rows[^1]);
            StatusText.Text = $"{_rows.Count} activit{(_rows.Count == 1 ? "y" : "ies")} in memory.";
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

    private void CopySelected_Click(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<ActivityRow> selected = ActivityList.SelectedItems.Cast<ActivityRow>().ToArray();
        if (selected.Count == 0)
        {
            StatusText.Text = "Select one or more activity rows first.";
            return;
        }

        Clipboard.SetText(FormatRows(selected));
        StatusText.Text = $"Copied {selected.Count} selected activit{(selected.Count == 1 ? "y" : "ies")}.";
    }

    private void CopyAll_Click(object sender, RoutedEventArgs e)
    {
        if (_rows.Count == 0)
        {
            StatusText.Text = "There is no activity to copy yet.";
            return;
        }

        Clipboard.SetText(FormatRows(_rows));
        StatusText.Text = $"Copied all {_rows.Count} activities.";
    }

    private static string FormatRows(IEnumerable<ActivityRow> rows)
    {
        StringBuilder text = new();
        foreach (ActivityRow row in rows)
            text.Append(row.LocalTime).Append("  [").Append(row.Level).Append("]  ")
                .Append(row.Category).Append("  —  ").AppendLine(row.Message);
        return text.ToString().TrimEnd();
    }

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
