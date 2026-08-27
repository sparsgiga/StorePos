using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using StorePos.Desktop.Api;
using StorePos.Desktop.Products.Models;
using StorePos.Desktop.Products.ViewModels;
using StorePos.Desktop.Products.Dialogs;
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
        var quickPrice = new StubQuickRetailPriceDialogService();
        using var viewModel = new ProductSearchViewModel(apiClient, () => 5, quickPrice);
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
        Assert.Equal(0, quickPrice.CallCount);
    }

    [Fact]
    public async Task Enter_ZeroPriceProductUpdatesPriceAndContinuesScannerAdd()
    {
        var handler = new ScannerFlowHandler(0m, 25m);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        var quickPrice = new StubQuickRetailPriceDialogService(
            new UpdateProductRetailPriceDto(10, 25m));
        using var viewModel = new ProductSearchViewModel(
            new StorePosApiClient(httpClient),
            () => 5,
            quickPrice);
        var added = new TaskCompletionSource<AddProductSaleItemResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.ProductAdded += (_, args) => added.TrySetResult(args.Result);
        viewModel.Query = "12345678";

        viewModel.EnterCommand.Execute(null);
        var result = await added.Task.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Equal(1, quickPrice.CallCount);
        Assert.Equal(1, handler.AddRequestCount);
        Assert.Equal(25m, result.UnitPrice);
    }

    [Fact]
    public async Task Enter_QuickPriceCancelledDoesNotAddProduct()
    {
        var handler = new ScannerFlowHandler(0m);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        var quickPrice = new StubQuickRetailPriceDialogService();
        using var viewModel = new ProductSearchViewModel(
            new StorePosApiClient(httpClient),
            () => 5,
            quickPrice);
        viewModel.Query = "12345678";

        viewModel.EnterCommand.Execute(null);
        await quickPrice.Invoked.Task.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Equal(1, quickPrice.CallCount);
        Assert.Equal(0, handler.AddRequestCount);
    }

    private sealed class ScannerFlowHandler(
        decimal price = 3.50m,
        decimal? salePrice = null) : HttpMessageHandler
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
                            "10",
                            "12345678",
                            "Cement",
                            1,
                            "Piece",
                            "pc",
                            price)
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
                        "10",
                        "12345678",
                        "Cement",
                        1,
                        "Piece",
                        1m,
                        salePrice ?? 3.50m,
                        salePrice ?? 3.50m,
                        false,
                        salePrice ?? 3.50m,
                        true,
                        null))
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class StubQuickRetailPriceDialogService(
        UpdateProductRetailPriceDto? result = null)
        : IQuickRetailPriceDialogService
    {
        public TaskCompletionSource Invoked { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount { get; private set; }

        public UpdateProductRetailPriceDto? ShowQuickRetailPrice(
            ProductSearchResultDto product,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Invoked.TrySetResult();
            return result;
        }
    }
}
