using System.Windows;
using StorePos.Desktop.Api;
using StorePos.Desktop.Products.Models;
using StorePos.Desktop.Products.ViewModels;
using StorePos.Desktop.Products.Views;

namespace StorePos.Desktop.Products.Dialogs;

public sealed class QuickRetailPriceDialogService(IStorePosApiClient apiClient)
    : IQuickRetailPriceDialogService
{
    public UpdateProductRetailPriceDto? ShowQuickRetailPrice(
        ProductSearchResultDto product,
        CancellationToken cancellationToken = default)
    {
        var viewModel = new QuickRetailPriceDialogViewModel(
            apiClient,
            product,
            cancellationToken);
        var dialog = new QuickRetailPriceDialog(viewModel)
        {
            Owner = Application.Current.MainWindow
        };

        return dialog.ShowDialog() == true ? viewModel.Result : null;
    }
}
