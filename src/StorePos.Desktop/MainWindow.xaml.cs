using System.Text;
using System.Windows;
using StorePos.Desktop.Sales.ViewModels;

namespace StorePos.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(SalesWorkspaceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
