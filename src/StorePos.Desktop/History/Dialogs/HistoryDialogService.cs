using System.Windows;
using StorePos.Desktop.History.Models;
using StorePos.Desktop.History.Views;

namespace StorePos.Desktop.History.Dialogs;

public sealed class HistoryDialogService : IHistoryDialogService
{
    public void ShowSaleDetails(SaleDetailsDto sale)
    {
        var dialog = new SaleDetailsDialog(sale)
        {
            Owner = Application.Current.MainWindow
        };
        dialog.ShowDialog();
    }

    public bool ConfirmReopen(SalesHistoryItemDto sale)
        => MessageBox.Show(
               Application.Current.MainWindow,
               $"{sale.SaleNumber}\nჯამი: {sale.TotalAmount:N2} ₾\n\n" +
               "გაყიდვა კვლავ აქტიური გახდება და დასრულებისას " +
               "დაფიქსირებული გადახდები გაუქმდება.",
               "გაყიდვის Draft-ზე დაბრუნება",
               MessageBoxButton.YesNo,
               MessageBoxImage.Warning,
               MessageBoxResult.No) == MessageBoxResult.Yes;
}
