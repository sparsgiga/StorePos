using System.Windows;
using StorePos.Desktop.Api;
using StorePos.Desktop.Common;
using StorePos.Desktop.Reporting;
using StorePos.Desktop.Reporting.Services;
using StorePos.Desktop.History.Models;
using StorePos.Desktop.History.ViewModels;
using StorePos.Desktop.History.Views;

namespace StorePos.Desktop.History.Dialogs;

public sealed class HistoryDialogService(
    IStorePosApiClient apiClient,
    IClipboardService clipboardService,
    ISaleReportingService reportingService) : IHistoryDialogService
{
    public bool ShowSaleDetails(
        SaleDetailsDto sale,
        CancellationToken cancellationToken = default)
    {
        var viewModel = new SaleDetailsDialogViewModel(
            sale,
            clipboardService,
            this,
            cancellationToken);
        var dialog = new SaleDetailsDialog(viewModel)
        {
            Owner = Application.Current.MainWindow
        };
        dialog.ShowDialog();
        return viewModel.HasFinancialChanges;
    }

    public AddDebtPaymentResponse? ShowDebtPayment(
        long saleId,
        decimal outstandingAmount,
        CancellationToken cancellationToken = default)
    {
        var viewModel = new DebtPaymentDialogViewModel(
            apiClient,
            saleId,
            outstandingAmount,
            cancellationToken);
        var dialog = new DebtPaymentDialog(viewModel)
        {
            Owner = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(window => window.IsActive)
                ?? Application.Current.MainWindow
        };

        return dialog.ShowDialog() == true ? viewModel.Result : null;
    }

    public void ShowSaleReporting(SaleDetailsDto sale)
        => reportingService.ShowOptions(
            SaleReportModelFactory.FromSaleDetails(sale, DateTime.Now));

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
