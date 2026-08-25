using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using StorePos.Desktop.Api;
using StorePos.Desktop.Products.Models;
using StorePos.Desktop.Products.ViewModels;
using StorePos.Desktop.Sales.Models;

namespace StorePos.Desktop.Tests.Products;

public sealed class ProductSearchViewModelTests
{
    [Fact]
    public async Task Enter_ExactBarcodeAddsSingleResolvedProductAndClearsQuery()
    {
        var handler = new ScannerFlowHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        var apiClient = new StorePosApiClient(httpClient);
        using var viewModel = new ProductSearchViewModel(apiClient, () => 5);
        var added = new TaskCompletionSource<AddProductSaleItemResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.ProductAdded += (_, args) => added.TrySetResult(args.Result);
        viewModel.Query = "12345678";

        viewModel.EnterCommand.Execute(null);
        var result = await added.Task.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Equal(5, result.SaleId);
        Assert.Equal(10, result.ProductId);
        Assert.Equal(string.Empty, viewModel.Query);
        Assert.Contains("exactOnly=true", handler.SearchRequestUri?.Query);
        Assert.Equal(1, handler.AddRequestCount);
    }

    private sealed class ScannerFlowHandler : HttpMessageHandler
    {
        public Uri? SearchRequestUri { get; private set; }

        public int AddRequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get &&
                request.RequestUri?.AbsolutePath == "/api/products/search")
            {
                SearchRequestUri = request.RequestUri;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new[]
                    {
                        new ProductSearchResultDto(
                            10,
                            "PRD-10",
                            "12345678",
                            "Cement",
                            1,
                            "Piece",
                            "pc",
                            3.50m)
                    })
                });
            }

            if (request.Method == HttpMethod.Post &&
                request.RequestUri?.AbsolutePath == "/api/sales/5/items/product")
            {
                AddRequestCount++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new AddProductSaleItemResponse(
                        5,
                        20,
                        10,
                        "PRD-10",
                        "12345678",
                        "Cement",
                        1,
                        "Piece",
                        1m,
                        3.50m,
                        3.50m,
                        3.50m,
                        true,
                        null))
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
