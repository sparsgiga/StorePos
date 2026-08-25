using System.Windows.Controls;
using System.Windows.Input;
using StorePos.Desktop.History.ViewModels;

namespace StorePos.Desktop.History.Views;

public partial class SoldProductsView : UserControl
{
    public SoldProductsView() => InitializeComponent();

    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is SoldProductsViewModel viewModel &&
            viewModel.OpenDetailsCommand.CanExecute(null))
        {
            viewModel.OpenDetailsCommand.Execute(null);
        }
    }
}
