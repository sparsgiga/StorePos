using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using StorePos.Desktop.Sales.Models;

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
}
