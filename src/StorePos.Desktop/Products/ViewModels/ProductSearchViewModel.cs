using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using StorePos.Desktop.Api;
using StorePos.Desktop.Common;
using StorePos.Desktop.Products.Models;
using StorePos.Desktop.Products.Dialogs;
using StorePos.Desktop.Sales.Models;

namespace StorePos.Desktop.Products.ViewModels;

public sealed class ProductSearchViewModel : ObservableObject, IDisposable
{
    private const int SearchLimit = 15;
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(250);

    private readonly IStorePosApiClient _apiClient;
    private readonly Func<long?> _getSaleId;
    private readonly IQuickRetailPriceDialogService _quickRetailPriceDialogService;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly AsyncRelayCommand _enterCommand;
    private readonly AsyncRelayCommand _addSelectedCommand;
    private CancellationTokenSource? _searchCancellation;
    private string _query = string.Empty;
    private string? _lastCompletedQuery;
    private ProductSearchResultDto? _selectedProduct;
    private bool _hasResults;
    private bool _isBusy;
    private bool _suppressQuerySearch;

    public ProductSearchViewModel(
        IStorePosApiClient apiClient,
        Func<long?> getSaleId,
        IQuickRetailPriceDialogService quickRetailPriceDialogService)
    {
        _apiClient = apiClient;
        _getSaleId = getSaleId;
        _quickRetailPriceDialogService = quickRetailPriceDialogService;
        _enterCommand = new AsyncRelayCommand(HandleEnterAsync, () => !IsBusy);
        _addSelectedCommand = new AsyncRelayCommand(
            AddSelectedAsync,
            () => !IsBusy && SelectedProduct is not null && _getSaleId().HasValue);
        MoveUpCommand = new RelayCommand(() => MoveSelection(-1), () => HasResults);
        MoveDownCommand = new RelayCommand(() => MoveSelection(1), () => HasResults);
        ClearCommand = new RelayCommand(Clear);
    }

    public event EventHandler<ProductAddedEventArgs>? ProductAdded;

    public event EventHandler<ManualProductFallbackEventArgs>? ManualFallbackRequested;

    public event EventHandler<ProductSearchErrorEventArgs>? ErrorOccurred;

    public event EventHandler? FocusRequested;

    public ObservableCollection<ProductSearchResultDto> Results { get; } = [];

    public string Query
    {
        get => _query;
        set
        {
            if (!SetProperty(ref _query, value ?? string.Empty) || _suppressQuerySearch)
            {
                return;
            }

            ScheduleSearch();
        }
    }

    public ProductSearchResultDto? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                _addSelectedCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool HasResults
    {
        get => _hasResults;
        private set
        {
            if (SetProperty(ref _hasResults, value))
            {
                ((RelayCommand)MoveUpCommand).NotifyCanExecuteChanged();
                ((RelayCommand)MoveDownCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                _enterCommand.NotifyCanExecuteChanged();
                _addSelectedCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public ICommand EnterCommand => _enterCommand;

    public ICommand AddSelectedCommand => _addSelectedCommand;

    public ICommand MoveUpCommand { get; }

    public ICommand MoveDownCommand { get; }

    public ICommand ClearCommand { get; }

    public void NotifySaleChanged() => _addSelectedCommand.NotifyCanExecuteChanged();

    public void ClearAndFocus()
    {
        Clear();
        FocusRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        CancelPendingSearch();
        _lifetimeCancellation.Cancel();
        _lifetimeCancellation.Dispose();
    }

    private void ScheduleSearch()
    {
        CancelPendingSearch();

        var query = Query.Trim();
        if (query.Length < 2)
        {
            SetResults([], null);
            return;
        }

        _searchCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            _lifetimeCancellation.Token);
        _ = SearchAfterDelayAsync(query, _searchCancellation);
    }

    private async Task SearchAfterDelayAsync(
        string query,
        CancellationTokenSource cancellation)
    {
        try
        {
            await Task.Delay(DebounceDelay, cancellation.Token);
            var results = await _apiClient.SearchProductsAsync(
                query,
                SearchLimit,
                exactOnly: false,
                cancellation.Token);

            if (Query.Trim() == query)
            {
                SetResults(results, query);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorOccurred?.Invoke(
                this,
                new ProductSearchErrorEventArgs(
                    "პროდუქტების ძებნა ვერ მოხერხდა. შეამოწმეთ API კავშირი.",
                    exception));
        }
        finally
        {
            if (ReferenceEquals(_searchCancellation, cancellation))
            {
                _searchCancellation = null;
                cancellation.Dispose();
            }
        }
    }

    private async Task HandleEnterAsync()
    {
        var query = Query.Trim();
        if (string.IsNullOrEmpty(query))
        {
            return;
        }

        CancelPendingSearch();

        if (SelectedProduct is not null &&
            string.Equals(_lastCompletedQuery, query, StringComparison.Ordinal))
        {
            await AddSelectedAsync();
            return;
        }

        try
        {
            IsBusy = true;
            var results = await _apiClient.SearchProductsAsync(
                query,
                SearchLimit,
                exactOnly: true,
                _lifetimeCancellation.Token);

            if (results.Count == 1)
            {
                await AddProductAsync(results[0]);
                return;
            }

            if (results.Count > 1)
            {
                SetResults(results, query);
                return;
            }

            SetResults([], query);
            ManualFallbackRequested?.Invoke(
                this,
                new ManualProductFallbackEventArgs(query, LooksLikeBarcode(query)));
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorOccurred?.Invoke(
                this,
                new ProductSearchErrorEventArgs(
                    "პროდუქტის მოძებნა ან დამატება ვერ მოხერხდა.",
                    exception));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddSelectedAsync()
    {
        if (SelectedProduct is not null)
        {
            await AddProductAsync(SelectedProduct);
        }
    }

    private async Task AddProductAsync(ProductSearchResultDto product)
    {
        var saleId = _getSaleId();
        if (!saleId.HasValue)
        {
            ErrorOccurred?.Invoke(
                this,
                new ProductSearchErrorEventArgs("ჯერ აირჩიეთ ან შექმენით ღია გაყიდვა."));
            return;
        }

        if (product.Price <= 0)
        {
            var updatedPrice = _quickRetailPriceDialogService.ShowQuickRetailPrice(
                product,
                _lifetimeCancellation.Token);
            if (updatedPrice is null)
            {
                return;
            }

            if (updatedPrice.ProductId != product.Id || updatedPrice.Price <= 0)
            {
                ErrorOccurred?.Invoke(
                    this,
                    new ProductSearchErrorEventArgs(
                        "განახლებული საცალო ფასი ვერ დადასტურდა."));
                return;
            }

            product = product with { Price = updatedPrice.Price };
            ReplaceProductResult(product);
        }

        try
        {
            IsBusy = true;
            var result = await _apiClient.AddProductSaleItemAsync(
                saleId.Value,
                new AddProductSaleItemRequest(product.Id),
                _lifetimeCancellation.Token);

            ProductAdded?.Invoke(this, new ProductAddedEventArgs(result));
            Clear();
            FocusRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (ProductConflictException exception)
        {
            ErrorOccurred?.Invoke(this, new ProductSearchErrorEventArgs(exception.Message, exception));
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorOccurred?.Invoke(
                this,
                new ProductSearchErrorEventArgs(
                    "პროდუქტის გაყიდვაში დამატება ვერ მოხერხდა.",
                    exception));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ReplaceProductResult(ProductSearchResultDto product)
    {
        var existing = Results.FirstOrDefault(item => item.Id == product.Id);
        if (existing is null)
        {
            return;
        }

        Results[Results.IndexOf(existing)] = product;
        SelectedProduct = product;
    }

    private void MoveSelection(int offset)
    {
        if (Results.Count == 0)
        {
            return;
        }

        var currentIndex = SelectedProduct is null
            ? -1
            : Results.IndexOf(SelectedProduct);
        var newIndex = Math.Clamp(currentIndex + offset, 0, Results.Count - 1);
        SelectedProduct = Results[newIndex];
    }

    private void SetResults(
        IEnumerable<ProductSearchResultDto> results,
        string? completedQuery)
    {
        Results.Clear();
        foreach (var result in results)
        {
            Results.Add(result);
        }

        _lastCompletedQuery = completedQuery;
        SelectedProduct = Results.FirstOrDefault();
        HasResults = Results.Count > 0;
    }

    private void Clear()
    {
        CancelPendingSearch();
        _suppressQuerySearch = true;
        try
        {
            Query = string.Empty;
        }
        finally
        {
            _suppressQuerySearch = false;
        }

        SetResults([], null);
    }

    private void CancelPendingSearch()
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = null;
    }

    private static bool LooksLikeBarcode(string value)
        => value.Length >= 8 && value.All(char.IsDigit);
}
