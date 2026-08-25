using StorePos.Desktop.History.Models;

namespace StorePos.Desktop.History.Dialogs;

public interface IHistoryDialogService
{
    bool ShowSaleDetails(
        SaleDetailsDto sale,
        CancellationToken cancellationToken = default);

    AddDebtPaymentResponse? ShowDebtPayment(
        long saleId,
        decimal outstandingAmount,
        CancellationToken cancellationToken = default);

    bool ConfirmReopen(SalesHistoryItemDto sale);
}
