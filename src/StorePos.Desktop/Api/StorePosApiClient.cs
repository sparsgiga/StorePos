using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using StorePos.Desktop.Sales.Models;
using StorePos.Desktop.History.Models;
using System.Globalization;

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

    public async Task<UpdateDraftSaleInfoResponse> UpdateDraftSaleInfoAsync(
        long saleId,
        UpdateDraftSaleInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/sales/drafts/{saleId}/info",
            request,
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UpdateDraftSaleInfoResponse>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty update-sale response.");
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
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ReopenSaleResponse>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("The API returned an empty reopen-sale response.");
    }

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
