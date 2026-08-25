using StorePos.Desktop.Sales.Models;
using StorePos.Desktop.History.Models;

namespace StorePos.Desktop.Api;

public interface IStorePosApiClient
{
    Task<IReadOnlyList<DraftSaleDto>> GetDraftSalesAsync(
        CancellationToken cancellationToken = default);

    Task<CreateDraftSaleResponse> CreateDraftSaleAsync(
        CreateDraftSaleRequest request,
        CancellationToken cancellationToken = default);

    Task<DraftSaleDetailsDto> GetDraftSaleDetailsAsync(
        long saleId,
        CancellationToken cancellationToken = default);

    Task<AddManualSaleItemResponse> AddManualSaleItemAsync(
        long saleId,
        AddManualSaleItemRequest request,
        CancellationToken cancellationToken = default);

    Task<UpdateDraftSaleInfoResponse> UpdateDraftSaleInfoAsync(
        long saleId,
        UpdateDraftSaleInfoRequest request,
        CancellationToken cancellationToken = default);

    Task<UpdateSaleItemResponse> UpdateSaleItemAsync(
        long saleId,
        long saleItemId,
        UpdateSaleItemRequest request,
        CancellationToken cancellationToken = default);

    Task<RemoveSaleItemResponse> RemoveSaleItemAsync(
        long saleId,
        long saleItemId,
        CancellationToken cancellationToken = default);

    Task<CompleteSaleResponse> CompleteSaleAsync(
        long saleId,
        CompleteSaleRequest request,
        CancellationToken cancellationToken = default);

    Task<CancelSaleResponse> CancelSaleAsync(
        long saleId,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<SalesHistoryItemDto>> GetSalesHistoryAsync(
        SalesHistoryFilter filter,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<SoldProductDto>> GetSoldProductsAsync(
        SoldProductsFilter filter,
        CancellationToken cancellationToken = default);

    Task<SaleDetailsDto> GetSaleDetailsAsync(
        long saleId,
        CancellationToken cancellationToken = default);

    Task<ReopenSaleResponse> ReopenSaleAsync(
        long saleId,
        CancellationToken cancellationToken = default);
}
