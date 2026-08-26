using System.Windows;
using StorePos.Desktop.Reporting.Printing;

namespace StorePos.Desktop.Reporting.Views;

public partial class PrintPreviewWindow : Window
{
    public PrintPreviewWindow(PrintPreviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
