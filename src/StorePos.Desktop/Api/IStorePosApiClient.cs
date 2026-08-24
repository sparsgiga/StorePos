using StorePos.Desktop.Sales.Models;

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
}
