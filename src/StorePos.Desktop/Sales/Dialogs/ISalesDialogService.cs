using StorePos.Desktop.Sales.Models;
using StorePos.Desktop.Sales.ViewModels;

namespace StorePos.Desktop.Sales.Dialogs;

public interface ISalesDialogService
{
    UpdateDraftSaleInfoResponse? ShowCustomerInfo(
        SaleTabViewModel sale,
        CancellationToken cancellationToken = default);

    UpdateSaleItemResponse? ShowEditItem(
        long saleId,
        SaleItemViewModel item,
        CancellationToken cancellationToken = default);

    CompleteSaleResponse? ShowCompleteSale(
        SaleTabViewModel sale,
        CancellationToken cancellationToken = default);

    bool ConfirmRemoveItem(string productName);

    bool ConfirmCancelSale(string saleNumber, decimal totalAmount);
}
