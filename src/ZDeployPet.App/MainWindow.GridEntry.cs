using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private void TargetsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.None) return;
        e.Handled = true;

        TargetsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        TargetsGrid.CommitEdit(DataGridEditingUnit.Row, true);

        TargetDraft? current = TargetsGrid.CurrentItem as TargetDraft;
        TargetDraft next = GetOrCreateNextTarget(current);
        MoveGridToRow(TargetsGrid, next);
    }

    private void DestinationsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.None) return;
        e.Handled = true;

        DestinationsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        DestinationsGrid.CommitEdit(DataGridEditingUnit.Row, true);

        DestinationDraft? current = DestinationsGrid.CurrentItem as DestinationDraft;
        DestinationDraft next = GetOrCreateNextDestination(current);
        MoveGridToRow(DestinationsGrid, next);
    }

    private TargetDraft GetOrCreateNextTarget(TargetDraft? current)
    {
        int index = current is null ? -1 : _targetDrafts.IndexOf(current);
        if (index >= 0 && index < _targetDrafts.Count - 1)
            return _targetDrafts[index + 1];

        if (current is not null && current.IsBlank)
            return current;

        TargetDraft next = new() { Port = 22 };
        _targetDrafts.Add(next);
        return next;
    }

    private DestinationDraft GetOrCreateNextDestination(DestinationDraft? current)
    {
        int index = current is null ? -1 : _destinationDrafts.IndexOf(current);
        if (index >= 0 && index < _destinationDrafts.Count - 1)
            return _destinationDrafts[index + 1];

        if (current is not null && current.IsBlank)
            return current;

        DestinationDraft next = new();
        _destinationDrafts.Add(next);
        return next;
    }

    private static void MoveGridToRow(DataGrid grid, object row)
    {
        grid.SelectedItem = row;
        grid.ScrollIntoView(row);

        grid.Dispatcher.BeginInvoke(() =>
        {
            if (grid.Columns.Count == 0) return;
            grid.CurrentCell = new DataGridCellInfo(row, grid.Columns[0]);
            grid.Focus();
            grid.BeginEdit();
        }, DispatcherPriority.Input);
    }
}
