using StorePos.Application.Common.Models;
using StorePos.Application.Sales.Queries.GetDetails;
using StorePos.Application.Sales.Queries.GetHistory;
using StorePos.Application.Sales.Queries.GetSoldProducts;

namespace StorePos.Application.Common.Interfaces;

public interface ISalesReadService
{
    Task<PagedResult<SalesHistoryItemModel>> GetHistoryAsync(
        GetSalesHistoryQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedResult<SoldProductModel>> GetSoldProductsAsync(
        GetSoldProductsQuery query,
        CancellationToken cancellationToken = default);

    Task<SaleDetailsModel?> GetDetailsAsync(
        long saleId,
        CancellationToken cancellationToken = default);
}
