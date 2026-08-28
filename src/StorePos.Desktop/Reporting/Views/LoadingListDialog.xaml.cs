using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StorePos.Desktop.Reporting.ViewModels;

namespace StorePos.Desktop.Reporting.Views;

public partial class LoadingListDialog : Window
{
    public LoadingListDialog(LoadingListDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnItemsPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not DataGrid dataGrid)
        {
            return;
        }

        dataGrid.CommitEdit(DataGridEditingUnit.Cell, exitEditingMode: true);
        dataGrid.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true);
        e.Handled = true;
    }
}
