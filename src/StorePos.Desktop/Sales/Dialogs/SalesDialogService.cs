using System.Windows;
using StorePos.Desktop.Api;
using StorePos.Desktop.Sales.Models;
using StorePos.Desktop.Sales.ViewModels;
using StorePos.Desktop.Sales.Views;

namespace StorePos.Desktop.Sales.Dialogs;

public sealed class SalesDialogService(IStorePosApiClient apiClient) : ISalesDialogService
{
    public CustomerDialogResult? ShowCustomerInfo(
        SaleTabViewModel sale,
        CancellationToken cancellationToken = default)
    {
        var viewModel = new CustomerInfoDialogViewModel(
            apiClient,
            sale.Id,
            sale.CustomerId,
            sale.CustomerName,
            sale.CustomerIdentificationNumber,
            sale.Comment,
            cancellationToken);
        var dialog = new CustomerInfoDialog(viewModel)
        {
            Owner = Application.Current.MainWindow
        };

        dialog.ShowDialog();
        return viewModel.Result;
    }

    public UpdateSaleItemResponse? ShowEditItem(
        long saleId,
        SaleItemViewModel item,
        CancellationToken cancellationToken = default)
    {
        var viewModel = new EditSaleItemDialogViewModel(
            apiClient,
            saleId,
            item,
            cancellationToken);
        var dialog = new EditSaleItemDialog(viewModel)
        {
            Owner = Application.Current.MainWindow
        };

        return dialog.ShowDialog() == true ? viewModel.Result : null;
    }

    public CompleteSaleResponse? ShowCompleteSale(
        SaleTabViewModel sale,
        CancellationToken cancellationToken = default)
    {
        var viewModel = new CompleteSaleDialogViewModel(
            apiClient,
            sale.Id,
            sale.TotalAmount,
            cancellationToken);
        var dialog = new CompleteSaleDialog(viewModel)
        {
            Owner = Application.Current.MainWindow
        };

        return dialog.ShowDialog() == true ? viewModel.Result : null;
    }

    public bool ConfirmRemoveItem(string productName)
        => MessageBox.Show(
               Application.Current.MainWindow,
               $"„{productName}“ წაიშალოს გაყიდვიდან?",
               "პროდუქტის წაშლა",
               MessageBoxButton.YesNo,
               MessageBoxImage.Question,
               MessageBoxResult.No) == MessageBoxResult.Yes;

    public bool ConfirmCancelSale(string saleNumber, decimal totalAmount)
        => MessageBox.Show(
               Application.Current.MainWindow,
               $"ნამდვილად გსურთ გაყიდვის გაუქმება?\n\n{saleNumber}\nჯამი: {totalAmount:N2} ₾",
               "გაყიდვის გაუქმება",
               MessageBoxButton.YesNo,
               MessageBoxImage.Warning,
               MessageBoxResult.No) == MessageBoxResult.Yes;
}
