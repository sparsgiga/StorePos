using StorePos.Desktop.Sales.Models;
using StorePos.Desktop.History.Models;
using StorePos.Desktop.Customers.Models;
using StorePos.Desktop.Products.Models;

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

    Task<IReadOnlyList<ProductSearchResultDto>> SearchProductsAsync(
        string query,
        int limit = 15,
        bool exactOnly = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeasurementUnitDto>> GetMeasurementUnitsAsync(
        CancellationToken cancellationToken = default);

    Task<ProductCreationDefaultsDto> GetProductCreationDefaultsAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<ProductListItemDto>> GetProductsAsync(
        ProductListFilter filter,
        CancellationToken cancellationToken = default);

    Task<ProductDetailsDto> GetProductAsync(
        long productId,
        CancellationToken cancellationToken = default);

    Task<ProductMutationDto> CreateProductAsync(
        SaveProductRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductMutationDto> UpdateProductAsync(
        long productId,
        SaveProductRequest request,
        CancellationToken cancellationToken = default);

    Task<UpdateProductRetailPriceDto> UpdateProductRetailPriceAsync(
        long productId,
        UpdateProductRetailPriceRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductMutationDto> DeactivateProductAsync(
        long productId,
        CancellationToken cancellationToken = default);

    Task<ProductMutationDto> ActivateProductAsync(
        long productId,
        CancellationToken cancellationToken = default);

    Task<AddProductSaleItemResponse> AddProductSaleItemAsync(
        long saleId,
        AddProductSaleItemRequest request,
        CancellationToken cancellationToken = default);

    Task<AddProductSaleItemResponse> CreateProductAndAddSaleItemAsync(
        long saleId,
        CreateProductAndAddSaleItemRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<CustomerDto> GetCustomerAsync(
        long customerId,
        CancellationToken cancellationToken = default);

    Task<CustomerDto> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerDto> UpdateCustomerAsync(
        long customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task<SaleCustomerResponse> AssignCustomerToSaleAsync(
        long saleId,
        long customerId,
        CancellationToken cancellationToken = default);

    Task<SaleCustomerResponse> RemoveCustomerFromSaleAsync(
        long saleId,
        CancellationToken cancellationToken = default);

    Task<UpdateSaleCommentResponse> UpdateSaleCommentAsync(
        long saleId,
        string? comment,
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

    Task<AddDebtPaymentResponse> AddDebtPaymentAsync(
        long saleId,
        AddDebtPaymentRequest request,
        CancellationToken cancellationToken = default);
}
