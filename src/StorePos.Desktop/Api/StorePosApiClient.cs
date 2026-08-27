using System.Net.Http;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using StorePos.Desktop.Customers;
using StorePos.Desktop.Customers.Models;
using StorePos.Desktop.Sales.Models;
using StorePos.Desktop.History.Models;
using System.Globalization;
using StorePos.Desktop.Products;
using StorePos.Desktop.Products.Models;

namespace StorePos.Desktop.Api;

public sealed class StorePosApiClient(HttpClient httpClient) : IStorePosApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<DraftSaleDto>> GetDraftSalesAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/sales/drafts", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DraftSaleDto[]>(
                   JsonOptions,
                   cancellationToken)
               ?? [];
    }

    public async Task<CreateDraftSaleResponse> CreateDraftSaleAsync(
        CreateDraftSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/sales/drafts",
            request,
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CreateDraftSaleResponse>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty create-sale response.");
    }

    public async Task<DraftSaleDetailsDto> GetDraftSaleDetailsAsync(
        long saleId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/sales/drafts/{saleId}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DraftSaleDetailsDto>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty sale-details response.");
    }

    public async Task<AddManualSaleItemResponse> AddManualSaleItemAsync(
        long saleId,
        AddManualSaleItemRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/sales/{saleId}/items/manual",
            request,
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AddManualSaleItemResponse>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty manual-item response.");
    }

    public async Task<IReadOnlyList<ProductSearchResultDto>> SearchProductsAsync(
        string query,
        int limit = 15,
        bool exactOnly = false,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/products/search?query={Uri.EscapeDataString(query)}&limit={limit}&exactOnly={exactOnly.ToString().ToLowerInvariant()}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductSearchResultDto[]>(
                   JsonOptions,
                   cancellationToken)
               ?? [];
    }

    public async Task<IReadOnlyList<MeasurementUnitDto>> GetMeasurementUnitsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            "api/measurement-units",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<MeasurementUnitDto[]>(
                   JsonOptions,
                   cancellationToken)
               ?? [];
    }

    public async Task<ProductCreationDefaultsDto> GetProductCreationDefaultsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            "api/products/creation-defaults",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductCreationDefaultsDto>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException(
                   "The API returned an empty product-creation-defaults response.");
    }

    public async Task<PagedResultDto<ProductListItemDto>> GetProductsAsync(
        ProductListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryStringBuilder()
            .Add("search", filter.Search)
            .Add("status", filter.Status)
            .Add("measurementUnitId", filter.MeasurementUnitId)
            .Add("priceFrom", filter.PriceFrom)
            .Add("priceTo", filter.PriceTo)
            .Add("pageNumber", filter.PageNumber)
            .Add("pageSize", filter.PageSize);
        using var response = await httpClient.GetAsync($"api/products{query}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResultDto<ProductListItemDto>>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty product list.");
    }

    public async Task<ProductDetailsDto> GetProductAsync(
        long productId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/products/{productId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDetailsDto>(JsonOptions, cancellationToken)
               ?? throw new InvalidOperationException("The API returned empty product details.");
    }

    public async Task<ProductMutationDto> CreateProductAsync(
        SaveProductRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/products", request, JsonOptions, cancellationToken);
        await ThrowIfProductConflictAsync(response, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadProductMutationAsync(response, cancellationToken);
    }

    public async Task<ProductMutationDto> UpdateProductAsync(
        long productId,
        SaveProductRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/products/{productId}", request, JsonOptions, cancellationToken);
        await ThrowIfProductConflictAsync(response, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadProductMutationAsync(response, cancellationToken);
    }

    public async Task<ProductMutationDto> DeactivateProductAsync(
        long productId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"api/products/{productId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadProductMutationAsync(response, cancellationToken);
    }

    public async Task<ProductMutationDto> ActivateProductAsync(
        long productId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync(
            $"api/products/{productId}/activate", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadProductMutationAsync(response, cancellationToken);
    }

    public async Task<AddProductSaleItemResponse> AddProductSaleItemAsync(
        long saleId,
        AddProductSaleItemRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/sales/{saleId}/items/product",
            request,
            JsonOptions,
            cancellationToken);
        await ThrowIfProductConflictAsync(response, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AddProductSaleItemResponse>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty product-item response.");
    }

    public async Task<AddProductSaleItemResponse> CreateProductAndAddSaleItemAsync(
        long saleId,
        CreateProductAndAddSaleItemRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/sales/{saleId}/items/product/create",
            request,
            JsonOptions,
            cancellationToken);
        await ThrowIfProductConflictAsync(response, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AddProductSaleItemResponse>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty create-product response.");
    }

    public async Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/customers/search?query={Uri.EscapeDataString(query)}&limit={limit}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CustomerDto[]>(
                   JsonOptions,
                   cancellationToken)
               ?? [];
    }

    public async Task<CustomerDto> GetCustomerAsync(
        long customerId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/customers/{customerId}",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await ReadCustomerAsync(response, cancellationToken);
    }

    public async Task<CustomerDto> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/customers",
            request,
            JsonOptions,
            cancellationToken);
        ThrowIfCustomerConflict(response);
        response.EnsureSuccessStatusCode();

        return await ReadCustomerAsync(response, cancellationToken);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(
        long customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/customers/{customerId}",
            request,
            JsonOptions,
            cancellationToken);
        ThrowIfCustomerConflict(response);
        response.EnsureSuccessStatusCode();

        return await ReadCustomerAsync(response, cancellationToken);
    }

    public async Task<SaleCustomerResponse> AssignCustomerToSaleAsync(
        long saleId,
        long customerId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/sales/{saleId}/customer",
            new AssignCustomerToSaleRequest(customerId),
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SaleCustomerResponse>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty assign-customer response.");
    }

    public async Task<SaleCustomerResponse> RemoveCustomerFromSaleAsync(
        long saleId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync(
            $"api/sales/{saleId}/customer",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SaleCustomerResponse>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty remove-customer response.");
    }

    public async Task<UpdateSaleCommentResponse> UpdateSaleCommentAsync(
        long saleId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/sales/{saleId}/comment",
            new UpdateSaleCommentRequest(comment),
            JsonOptions,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UpdateSaleCommentResponse>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty update-comment response.");
    }

    public async Task<UpdateSaleItemResponse> UpdateSaleItemAsync(
        long saleId,
        long saleItemId,
        UpdateSaleItemRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/sales/{saleId}/items/{saleItemId}",
            request,
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UpdateSaleItemResponse>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty update-item response.");
    }

    public async Task<RemoveSaleItemResponse> RemoveSaleItemAsync(
        long saleId,
        long saleItemId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync(
            $"api/sales/{saleId}/items/{saleItemId}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RemoveSaleItemResponse>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty remove-item response.");
    }

    public async Task<CompleteSaleResponse> CompleteSaleAsync(
        long saleId,
        CompleteSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/sales/{saleId}/complete",
            request,
            JsonOptions,
            cancellationToken);

        await ThrowIfSaleOperationConflictAsync(response, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty complete-sale response.");
    }

    public async Task<CancelSaleResponse> CancelSaleAsync(
        long saleId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync(
            $"api/sales/{saleId}/cancel",
            content: null,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CancelSaleResponse>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty cancel-sale response.");
    }

    public async Task<PagedResultDto<SalesHistoryItemDto>> GetSalesHistoryAsync(
        SalesHistoryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryStringBuilder()
            .AddDate("dateFrom", filter.DateFrom)
            .AddDate("dateTo", filter.DateTo)
            .Add("saleNumber", filter.SaleNumber)
            .Add("customerName", filter.CustomerName)
            .Add("status", filter.Status)
            .Add("pageNumber", filter.PageNumber)
            .Add("pageSize", filter.PageSize);

        using var response = await httpClient.GetAsync(
            $"api/sales/history{query}",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PagedResultDto<SalesHistoryItemDto>>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty sales-history response.");
    }

    public async Task<PagedResultDto<SoldProductDto>> GetSoldProductsAsync(
        SoldProductsFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryStringBuilder()
            .AddDate("dateFrom", filter.DateFrom)
            .AddDate("dateTo", filter.DateTo)
            .Add("productSearch", filter.ProductSearch)
            .Add("saleNumber", filter.SaleNumber)
            .Add("customerName", filter.CustomerName)
            .Add("isManual", filter.IsManual)
            .Add("pageNumber", filter.PageNumber)
            .Add("pageSize", filter.PageSize);

        using var response = await httpClient.GetAsync(
            $"api/sales/sold-items{query}",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PagedResultDto<SoldProductDto>>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty sold-products response.");
    }

    public async Task<SaleDetailsDto> GetSaleDetailsAsync(
        long saleId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/sales/{saleId}",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SaleDetailsDto>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty sale-details response.");
    }

    public async Task<ReopenSaleResponse> ReopenSaleAsync(
        long saleId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync(
            $"api/sales/{saleId}/reopen",
            content: null,
            cancellationToken);
        await ThrowIfSaleOperationConflictAsync(response, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ReopenSaleResponse>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty reopen-sale response.");
    }

    public async Task<AddDebtPaymentResponse> AddDebtPaymentAsync(
        long saleId,
        AddDebtPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/sales/{saleId}/debt-payments",
            request,
            JsonOptions,
            cancellationToken);
        await ThrowIfSaleOperationConflictAsync(response, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AddDebtPaymentResponse>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty debt-payment response.");
    }

    private static async Task<CustomerDto> ReadCustomerAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
        => await response.Content.ReadFromJsonAsync<CustomerDto>(
               JsonOptions,
               cancellationToken)
           ?? throw new InvalidOperationException("The API returned an empty customer response.");

    private static async Task<ProductMutationDto> ReadProductMutationAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
        => await response.Content.ReadFromJsonAsync<ProductMutationDto>(
               JsonOptions,
               cancellationToken)
           ?? throw new InvalidOperationException("The API returned an empty product response.");

    private static void ThrowIfCustomerConflict(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new CustomerConflictException();
        }
    }

    private static async Task ThrowIfProductConflictAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode != HttpStatusCode.Conflict)
        {
            return;
        }

        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>(
            JsonOptions,
            cancellationToken);
        var kind = problem?.Title switch
        {
            { } title when title.Contains("barcode", StringComparison.OrdinalIgnoreCase) =>
                ProductConflictKind.Barcode,
            { } title when title.Contains("code", StringComparison.OrdinalIgnoreCase) =>
                ProductConflictKind.Code,
            { } title when title.Contains("measurement", StringComparison.OrdinalIgnoreCase) =>
                ProductConflictKind.MeasurementUnit,
            { } title when title.Contains("retail price", StringComparison.OrdinalIgnoreCase) =>
                ProductConflictKind.RetailPrice,
            _ => ProductConflictKind.Unknown
        };

        throw new ProductConflictException(kind, problem?.Detail);
    }

    private static async Task ThrowIfSaleOperationConflictAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode != HttpStatusCode.Conflict)
        {
            return;
        }

        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>(
            JsonOptions,
            cancellationToken);
        throw new SaleOperationException(
            problem?.Detail ?? "The sale operation conflicts with its current state.");
    }

    private sealed record ApiProblemDetails(string? Title, string? Detail);

    private sealed class QueryStringBuilder
    {
        private readonly List<string> _parameters = [];

        public QueryStringBuilder Add(string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _parameters.Add(
                    $"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value.Trim())}");
            }

            return this;
        }

        public QueryStringBuilder Add(string name, int? value)
            => value.HasValue ? Add(name, value.Value.ToString(CultureInfo.InvariantCulture)) : this;

        public QueryStringBuilder Add(string name, int value)
            => Add(name, value.ToString(CultureInfo.InvariantCulture));

        public QueryStringBuilder Add(string name, decimal? value)
            => value.HasValue
                ? Add(name, value.Value.ToString(CultureInfo.InvariantCulture))
                : this;

        public QueryStringBuilder Add(string name, bool? value)
            => value.HasValue ? Add(name, value.Value ? "true" : "false") : this;

        public QueryStringBuilder AddDate(string name, DateTime? value)
            => value.HasValue
                ? Add(name, value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                : this;

        public override string ToString()
            => _parameters.Count == 0 ? string.Empty : $"?{string.Join('&', _parameters)}";
    }
}
