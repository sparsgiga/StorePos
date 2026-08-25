using System.Windows;
using StorePos.Desktop.History.Models;

namespace StorePos.Desktop.History.Views;

public partial class SaleDetailsDialog : Window
{
    public SaleDetailsDialog(SaleDetailsDto sale)
    {
        InitializeComponent();
        DataContext = sale;
    }
}
