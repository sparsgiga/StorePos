using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using StorePos.Desktop.Api;
using StorePos.Desktop.Products.Barcodes;
using StorePos.Desktop.Products.Dialogs;
using StorePos.Desktop.Products.Models;
using StorePos.Desktop.Products.ViewModels;
using StorePos.Desktop.History.ViewModels;
using StorePos.Desktop.Sales.ViewModels;

namespace StorePos.Desktop.Tests.Products;

public sealed class ProductManagementViewModelTests
{
    [Fact]
    public async Task MainNavigation_CreatesAndLoadsProductsOnlyWhenOpened()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/measurement-units" => Json(new[]
            {
                new MeasurementUnitDto(1, "Piece", "pc", null)
            }),
            "/api/products" => Json(new
            {
                items = new[]
                {
                    new ProductListItemDto(
                        10, "100", "111", "Product", 1, "Piece", "pc", 2m, true)
                },
                totalCount = 1,
                pageNumber = 1,
                pageSize = 50
            }),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        var apiClient = CreateClient(handler);
        var factoryCalls = 0;
        using var main = new MainWindowViewModel(
            new SalesWorkspaceViewModel(apiClient, null!, null!),
            new SalesHistoryViewModel(apiClient, null!),
            new SoldProductsViewModel(apiClient, null!),
            () =>
            {
                factoryCalls++;
                return new ProductsViewModel(apiClient, new StubDialogService());
            });

        Assert.Equal(0, factoryCalls);
        Assert.Null(main.Products);
        Assert.Empty(handler.Requests);

        main.ShowProductsCommand.Execute(null);
        for (var attempt = 0; attempt < 100 && main.Products?.Items.Count == 0; attempt++)
        {
            await Task.Delay(10);
        }

        Assert.Equal(1, factoryCalls);
        Assert.Equal("Product", Assert.Single(main.Products!.Items).Name);
    }

    [Fact]
    public async Task ProductsViewModel_DoesNotLoadUntilRefreshAndThenLoadsFirstPage()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/measurement-units" => Json(new[]
            {
                new MeasurementUnitDto(1, "Piece", "pc", null)
            }),
            "/api/products" => Json(new
            {
                items = new[]
                {
                    new ProductListItemDto(
                        10, "100", "111", "Product", 1, "Piece", "pc", 2m, true)
                },
                totalCount = 1,
                pageNumber = 1,
                pageSize = 50
            }),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        var apiClient = CreateClient(handler);
        using var viewModel = new ProductsViewModel(apiClient, new StubDialogService());

        Assert.Empty(handler.Requests);
        await viewModel.RefreshAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("Product", Assert.Single(viewModel.Items).Name);
        Assert.Equal("1 / 1", viewModel.PageLabel);
    }

    [Fact]
    public async Task CreateEditor_LoadsDefaultsAndAutoInitializesBarcode()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/measurement-units" => Json(new[]
            {
                new MeasurementUnitDto(1, "Piece", "pc", null)
            }),
            "/api/products/creation-defaults" => Json(new ProductCreationDefaultsDto(
                "10526", 1, "Piece", "pc", null)),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        var viewModel = new ProductEditorDialogViewModel(
            CreateClient(handler), null, CancellationToken.None);

        await viewModel.InitializeAsync();

        Assert.Equal("10526", viewModel.Code);
        Assert.Equal(new Ean13BarcodeGenerator().Generate("10526"), viewModel.Barcode);
        Assert.Equal(1, viewModel.SelectedMeasurementUnit!.Id);
    }

    [Fact]
    public async Task CreateEditor_RequiresAsciiNumericCode()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/measurement-units" => Json(new[]
            {
                new MeasurementUnitDto(1, "Piece", "pc", null)
            }),
            "/api/products/creation-defaults" => Json(new ProductCreationDefaultsDto(
                "10526", 1, "Piece", "pc", null)),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        var viewModel = new ProductEditorDialogViewModel(
            CreateClient(handler), null, CancellationToken.None);
        await viewModel.InitializeAsync();
        viewModel.Name = "Product";
        viewModel.Price = "1";

        viewModel.Code = "A-1";
        Assert.False(viewModel.SaveCommand.CanExecute(null));

        viewModel.Code = "10526";
        Assert.True(viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task EditEditor_PreservesBarcodeUntilExplicitRegeneration()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/measurement-units" => Json(new[]
            {
                new MeasurementUnitDto(1, "Piece", "pc", null)
            }),
            "/api/products/10" => Json(new ProductDetailsDto(
                10, "A-100", "existing-barcode", "Product", 1, "Piece", "pc", 2m, true)),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        var viewModel = new ProductEditorDialogViewModel(
            CreateClient(handler), 10, CancellationToken.None);

        await viewModel.InitializeAsync();
        Assert.True(viewModel.SaveCommand.CanExecute(null));
        viewModel.Code = "200";

        Assert.Equal("existing-barcode", viewModel.Barcode);
        viewModel.GenerateBarcodeCommand.Execute(null);
        Assert.Equal(new Ean13BarcodeGenerator().Generate("200"), viewModel.Barcode);
    }

    [Fact]
    public async Task ApiClient_ProductFiltersAreSentServerSide()
    {
        var handler = new StubHandler(_ => Json(new
        {
            items = Array.Empty<ProductListItemDto>(),
            totalCount = 0,
            pageNumber = 2,
            pageSize = 50
        }));
        var client = CreateClient(handler);

        await client.GetProductsAsync(new ProductListFilter(
            "cement", 2, 5, 10.5m, 20.75m, 2, 50));

        var query = Assert.Single(handler.Requests).Query;
        Assert.Contains("search=cement", query);
        Assert.Contains("status=2", query);
        Assert.Contains("measurementUnitId=5", query);
        Assert.Contains("priceFrom=10.5", query);
        Assert.Contains("priceTo=20.75", query);
        Assert.Contains("pageNumber=2", query);
    }

    private static StorePosApiClient CreateClient(HttpMessageHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

    private static HttpResponseMessage Json<T>(T value)
        => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

    private sealed class StubHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class StubDialogService : IProductDialogService
    {
        public Task<bool> ShowCreateAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<bool> ShowEditAsync(
            long productId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public bool ConfirmDeactivate(string productName) => false;
    }
}
