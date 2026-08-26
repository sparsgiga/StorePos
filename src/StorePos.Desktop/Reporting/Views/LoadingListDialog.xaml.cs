using System.Windows;
using StorePos.Desktop.Reporting.ViewModels;

namespace StorePos.Desktop.Reporting.Views;

public partial class LoadingListDialog : Window
{
    public LoadingListDialog(LoadingListDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
