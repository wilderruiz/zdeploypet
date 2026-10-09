using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ZDeployPet.App;

public partial class MainWindow
{
    static MainWindow()
    {
        // A class handler runs before the instance Click handler declared in XAML.
        // This guarantees that text still being edited inside a DataGrid ComboBox
        // is pushed into the backing draft objects before SaveProfile_Click builds JSON.
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            Button.ClickEvent,
            new RoutedEventHandler(OnMainWindowButtonClick),
            true);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        ConfigureManagedEntryRows();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Window_Closing persists the unfinished draft. Flush editors first so the
        // draft on disk matches every value currently visible in the grids.
        FlushVisibleGridEditors();
        base.OnClosing(e);
    }

    private static void OnMainWindowButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window) return;
        if (e.OriginalSource is not Button button) return;
        if (!ReferenceEquals(button, window.SaveProfileButton)) return;

        window.FlushVisibleGridEditors();
    }

    private void FlushVisibleGridEditors()
    {
        // DataGridTemplateColumn contains always-visible editable ComboBoxes. WPF can
        // leave their Text binding one dispatcher step behind the text on screen.
        // Force every visible editor to update its source before CommitEdit/BuildDraft.
        UpdateComboBoxTextBindings(TargetsGrid);
        UpdateComboBoxTextBindings(DestinationsGrid);

        TargetsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        TargetsGrid.CommitEdit(DataGridEditingUnit.Row, true);
        DestinationsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        DestinationsGrid.CommitEdit(DataGridEditingUnit.Row, true);
    }

    private static void UpdateComboBoxTextBindings(DependencyObject root)
    {
        int childCount = VisualTreeHelper.GetChildrenCount(root);
        for (int index = 0; index < childCount; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);
            if (child is ComboBox comboBox)
                comboBox.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();

            UpdateComboBoxTextBindings(child);
        }
    }

    private void ConfigureManagedEntryRows()
    {
        // WPF's built-in DataGrid "new item" row is a temporary CollectionView placeholder.
        // It can display typed values that are not yet present in our ObservableCollection,
        // so a profile save can silently omit what the operator can see on screen.
        // ZDeployPet owns row creation instead: every visible editable row is a real draft object.
        TargetsGrid.CanUserAddRows = false;
        DestinationsGrid.CanUserAddRows = false;
        EnsureTrailingEntryRows();
    }

    private void EnsureTrailingEntryRows()
    {
        if (_targetDrafts.Count == 0 || !_targetDrafts[^1].IsBlank)
            _targetDrafts.Add(new TargetDraft { Port = 22 });

        if (_destinationDrafts.Count == 0 || !_destinationDrafts[^1].IsBlank)
            _destinationDrafts.Add(new DestinationDraft());
    }

    private void TargetsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.None) return;
        e.Handled = true;

        FlushVisibleGridEditors();

        TargetDraft? current = TargetsGrid.CurrentItem as TargetDraft;
        TargetDraft next = GetOrCreateNextTarget(current);
        MoveGridToRow(TargetsGrid, next);
    }

    private void DestinationsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.None) return;
        e.Handled = true;

        FlushVisibleGridEditors();

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
