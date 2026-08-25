using StorePos.Desktop.History.Models;

namespace StorePos.Desktop.History.Dialogs;

public interface IHistoryDialogService
{
    void ShowSaleDetails(SaleDetailsDto sale);

    bool ConfirmReopen(SalesHistoryItemDto sale);
}
