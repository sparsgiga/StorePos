using System.Net;
using System.Net.Http.Json;
using StorePos.Desktop.Api;
using StorePos.Desktop.Products.Models;
using StorePos.Desktop.Products.ViewModels;
using StorePos.Desktop.Sales.Dialogs;

namespace StorePos.Desktop.Tests.Products;

public sealed class QuickRetailPriceDialogViewModelTests
{
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    public void InvalidPrice_DisablesSave(string price)
    {
        using var httpClient = CreateClient(new PriceUpdateHandler());
        var viewModel = CreateViewModel(httpClient);

        viewModel.Price = price;

        Assert.False(viewModel.SaveCommand.CanExecute(null));
        if (price.Length > 0)
        {
            Assert.Contains("0-ზე მეტი", viewModel.ErrorMessage);
        }
    }

    [Fact]
    public async Task Save_AcceptsCommaDecimalAndReturnsApiConfirmedNormalizedPrice()
    {
        var handler = new PriceUpdateHandler();
        using var httpClient = CreateClient(handler);
        var viewModel = CreateViewModel(httpClient);
        var closed = new TaskCompletionSource<DialogCloseRequestedEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.CloseRequested += (_, args) => closed.TrySetResult(args);
        viewModel.Price = "25,123456";

        viewModel.SaveCommand.Execute(null);
        var close = await closed.Task.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.True(close.DialogResult);
        Assert.Equal(25.12346m, handler.ReceivedPrice);
        Assert.Equal(25.12346m, viewModel.Result?.Price);
        Assert.Equal("/api/products/10/retail-price", handler.RequestPath);
    }

    [Fact]
    public void Cancel_DoesNotCallApiAndClosesWithoutResult()
    {
        var handler = new PriceUpdateHandler();
        using var httpClient = CreateClient(handler);
        var viewModel = CreateViewModel(httpClient);
        DialogCloseRequestedEventArgs? close = null;
        viewModel.CloseRequested += (_, args) => close = args;

        viewModel.CancelCommand.Execute(null);

        Assert.False(close?.DialogResult);
        Assert.Null(viewModel.Result);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task ApiFailure_KeepsDialogOpenAndShowsFriendlyError()
    {
        var handler = new PriceUpdateHandler(HttpStatusCode.InternalServerError);
        using var httpClient = CreateClient(handler);
        var viewModel = CreateViewModel(httpClient);
        var errorChanged = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(viewModel.ErrorMessage) &&
                viewModel.ErrorMessage is not null)
            {
                errorChanged.TrySetResult();
            }
        };
        viewModel.Price = "25";

        viewModel.SaveCommand.Execute(null);
        await errorChanged.Task.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Contains("შენახვა ვერ მოხერხდა", viewModel.ErrorMessage);
        Assert.Null(viewModel.Result);
    }

    private static QuickRetailPriceDialogViewModel CreateViewModel(HttpClient httpClient)
        => new(
            new StorePosApiClient(httpClient),
            new ProductSearchResultDto(10, "P-10", "100", "Cement", 1, "Piece", "pc", 0m));

    private static HttpClient CreateClient(HttpMessageHandler handler)
        => new(handler) { BaseAddress = new Uri("http://localhost/") };

    private sealed class PriceUpdateHandler(HttpStatusCode statusCode = HttpStatusCode.OK)
        : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        public decimal? ReceivedPrice { get; private set; }

        public string? RequestPath { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            RequestPath = request.RequestUri?.AbsolutePath;
            var body = await request.Content!.ReadFromJsonAsync<UpdateProductRetailPriceRequest>(
                cancellationToken: cancellationToken);
            ReceivedPrice = body?.Price;
            return new HttpResponseMessage(statusCode)
            {
                Content = statusCode == HttpStatusCode.OK
                    ? JsonContent.Create(new UpdateProductRetailPriceDto(10, body!.Price))
                    : JsonContent.Create(new { title = "Failure" })
            };
        }
    }
}
