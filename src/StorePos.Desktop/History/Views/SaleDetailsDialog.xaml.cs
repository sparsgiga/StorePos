using System.Windows;
using StorePos.Desktop.History.ViewModels;

namespace StorePos.Desktop.History.Views;

public partial class SaleDetailsDialog : Window
{
    public SaleDetailsDialog(SaleDetailsDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
