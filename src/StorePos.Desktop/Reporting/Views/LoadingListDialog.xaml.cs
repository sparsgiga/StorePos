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
        if (sender is not DataGrid dataGrid)
        {
            return;
        }

        if (e.Key == Key.Space &&
            dataGrid.CurrentColumn?.DisplayIndex == 0 &&
            dataGrid.CurrentItem is LoadingListItemSelectionViewModel item)
        {
            item.IsSelected = !item.IsSelected;
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter)
        {
            return;
        }

        dataGrid.CommitEdit(DataGridEditingUnit.Cell, exitEditingMode: true);
        dataGrid.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true);
        e.Handled = true;
    }

    private void OnSelectionCellClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement
            {
                DataContext: LoadingListItemSelectionViewModel item
            })
        {
            item.IsSelected = !item.IsSelected;
            e.Handled = true;
        }
    }
}
