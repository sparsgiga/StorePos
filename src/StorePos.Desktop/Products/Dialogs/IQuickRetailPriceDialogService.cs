using StorePos.Desktop.Products.Models;

namespace StorePos.Desktop.Products.Dialogs;

public interface IQuickRetailPriceDialogService
{
    UpdateProductRetailPriceDto? ShowQuickRetailPrice(
        ProductSearchResultDto product,
        CancellationToken cancellationToken = default);
}
