using System.Windows;
using StorePos.Desktop.Reporting.ViewModels;

namespace StorePos.Desktop.Reporting.Views;

public partial class PrintOptionsDialog : Window
{
    public PrintOptionsDialog(PrintOptionsDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
