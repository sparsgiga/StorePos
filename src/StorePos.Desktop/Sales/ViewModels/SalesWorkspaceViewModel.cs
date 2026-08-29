using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using StorePos.Desktop.Api;
using StorePos.Desktop.Common;
using StorePos.Desktop.Sales.Dialogs;
using StorePos.Desktop.Sales.Models;
using StorePos.Desktop.Products;
using StorePos.Desktop.Products.ViewModels;
using StorePos.Desktop.Products.Dialogs;

namespace StorePos.Desktop.Sales.ViewModels;

public sealed class SalesWorkspaceViewModel : ObservableObject, IDisposable
{
    private readonly IStorePosApiClient _apiClient;
    private readonly ISalesDialogService _dialogService;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly AsyncRelayCommand _newSaleCommand;
    private readonly AsyncRelayCommand _addManualItemCommand;
    private readonly AsyncRelayCommand _removeSelectedItemCommand;
    private readonly AsyncRelayCommand _cancelSaleCommand;
    private readonly AsyncRelayCommand _updateDiscountCommand;
    private readonly RelayCommand _editCustomerCommand;
    private readonly RelayCommand _editSelectedItemCommand;
    private readonly RelayCommand _completeSaleCommand;
    private readonly RelayCommand _printSaleCommand;
    private CancellationTokenSource? _detailsCancellation;
    private CancellationTokenSource? _catalogDefaultsCancellation;
    private SaleTabViewModel? _selectedSale;
    private SaleItemViewModel? _selectedItem;
    private string? _errorMessage;
    private bool _isBusy;

    public SalesWorkspaceViewModel(
        IStorePosApiClient apiClient,
        ISalesDialogService dialogService,
        IQuickRetailPriceDialogService quickRetailPriceDialogService)
    {
        _apiClient = apiClient;
        _dialogService = dialogService;
        ProductSearch = new ProductSearchViewModel(
            apiClient,
            () => SelectedSale?.IsDetailsLoaded == true ? SelectedSale.Id : null,
            quickRetailPriceDialogService);
        ProductSearch.ProductAdded += OnProductAdded;
        ProductSearch.ManualFallbackRequested += OnManualFallbackRequested;
        ProductSearch.ErrorOccurred += OnProductSearchError;
        ManualItemInput.PropertyChanged += OnManualItemInputPropertyChanged;

        _newSaleCommand = new AsyncRelayCommand(CreateDraftSaleAsync, () => !IsBusy);
        _addManualItemCommand = new AsyncRelayCommand(AddManualItemAsync, CanAddManualItem);
        _removeSelectedItemCommand = new AsyncRelayCommand(
            RemoveSelectedItemAsync,
            CanModifySelectedItem);
        _cancelSaleCommand = new AsyncRelayCommand(CancelSaleAsync, CanCancelSale);
        _updateDiscountCommand = new AsyncRelayCommand(UpdateDiscountAsync, CanEditCustomer);
        _editCustomerCommand = new RelayCommand(EditCustomer, CanEditCustomer);
        _editSelectedItemCommand = new RelayCommand(EditSelectedItem, CanModifySelectedItem);
        _completeSaleCommand = new RelayCommand(CompleteSale, CanCompleteSale);
        _printSaleCommand = new RelayCommand(PrintSale, CanPrintSale);
    }

    public event EventHandler? ProductNameFocusRequested;

    public ObservableCollection<SaleTabViewModel> OpenSales { get; } = [];

    public SaleItemInputViewModel ManualItemInput { get; } = new();

    public ProductSearchViewModel ProductSearch { get; }

    public SaleTabViewModel? SelectedSale
    {
        get => _selectedSale;
        set
        {
            if (!SetProperty(ref _selectedSale, value))
            {
                return;
            }

            SelectedItem = null;
            ProductSearch.NotifySaleChanged();
            NotifyCommandStates();
            _ = LoadSaleDetailsAsync(value);
        }
    }

    public SaleItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                NotifyCommandStates();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                NotifyCommandStates();
            }
        }
    }

    public ICommand NewSaleCommand => _newSaleCommand;

    public ICommand AddManualItemCommand => _addManualItemCommand;

    public ICommand EditCustomerCommand => _editCustomerCommand;

    public ICommand EditSelectedItemCommand => _editSelectedItemCommand;

    public ICommand RemoveSelectedItemCommand => _removeSelectedItemCommand;

    public ICommand CompleteSaleCommand => _completeSaleCommand;

    public ICommand CancelSaleCommand => _cancelSaleCommand;

    public ICommand PrintSaleCommand => _printSaleCommand;

    public ICommand UpdateDiscountCommand => _updateDiscountCommand;

    public async Task UpdateItemFinancialsAsync(
        SaleItemViewModel item,
        SaleItemInlineField field,
        string? input)
    {
        var sale = SelectedSale;
        if (sale is null || !DecimalInputParser.TryParse(input, out var value))
        {
            item.ResetFinancialInputs();
            ErrorMessage = "შეიყვანეთ სწორი დადებითი რიცხვი.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var request = field switch
            {
                SaleItemInlineField.Quantity => new UpdateSaleItemFinancialsRequest(
                    Quantity: value),
                SaleItemInlineField.UnitPrice => new UpdateSaleItemFinancialsRequest(
                    UnitPrice: value),
                SaleItemInlineField.LineTotal => new UpdateSaleItemFinancialsRequest(
                    LineTotal: value),
                _ => throw new ArgumentOutOfRangeException(nameof(field))
            };
            var result = await _apiClient.UpdateSaleItemFinancialsAsync(
                sale.Id,
                item.Id,
                request,
                _lifetimeCancellation.Token);
            item.ApplyUpdate(
                item.ProductName,
                result.Quantity,
                result.UnitPrice,
                result.LineTotal,
                item.Comment);
            sale.ApplyFinancials(
                result.SaleSubtotal,
                result.SaleDiscountAmount,
                result.SaleTotalAmount);
            if (result.RequestedLineTotalAdjusted)
            {
                ErrorMessage = $"შეყვანილი ჯამი დამრგვალდა კანონიკურ მნიშვნელობამდე: {result.LineTotal:N2} ₾.";
            }
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
            item.ResetFinancialInputs();
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            item.ResetFinancialInputs();
            ErrorMessage = exception is SaleOperationException
                ? exception.Message
                : "ცვლილება ვერ შეინახა. წინა მნიშვნელობები შენარჩუნებულია.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task InitializeAsync()
    {
        var unitsTask = _apiClient.GetMeasurementUnitsAsync(_lifetimeCancellation.Token);
        var refreshTask = RefreshAsync();

        try
        {
            ManualItemInput.LoadMeasurementUnits(await unitsTask);
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "საზომი ერთეულების ჩატვირთვა ვერ მოხერხდა.";
        }

        await refreshTask;
    }

    public async Task RefreshAsync(long? preferredSaleId = null)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var drafts = await _apiClient.GetDraftSalesAsync(_lifetimeCancellation.Token);
            var loadedTabs = drafts.Select(Map).ToArray();
            var selectedSaleId = preferredSaleId ?? SelectedSale?.Id;

            OpenSales.Clear();
            foreach (var tab in loadedTabs)
            {
                OpenSales.Add(tab);
            }

            SelectedSale = selectedSaleId.HasValue
                ? OpenSales.FirstOrDefault(sale => sale.Id == selectedSaleId.Value)
                  ?? OpenSales.FirstOrDefault()
                : OpenSales.FirstOrDefault();
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "ღია გაყიდვების ჩატვირთვა ვერ მოხერხდა. შეამოწმეთ API კავშირი.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Dispose()
    {
        ManualItemInput.PropertyChanged -= OnManualItemInputPropertyChanged;
        ProductSearch.ProductAdded -= OnProductAdded;
        ProductSearch.ManualFallbackRequested -= OnManualFallbackRequested;
        ProductSearch.ErrorOccurred -= OnProductSearchError;
        ProductSearch.Dispose();
        _catalogDefaultsCancellation?.Cancel();
        _catalogDefaultsCancellation?.Dispose();
        _detailsCancellation?.Cancel();
        _detailsCancellation?.Dispose();
        _lifetimeCancellation.Cancel();
        _lifetimeCancellation.Dispose();
    }

    private async Task CreateDraftSaleAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var createdSale = await _apiClient.CreateDraftSaleAsync(
                new CreateDraftSaleRequest(),
                _lifetimeCancellation.Token);

            var newTab = new SaleTabViewModel(
                createdSale.SaleId,
                createdSale.SaleNumber,
                createdSale.TotalAmount,
                0m,
                createdSale.TotalAmount,
                createdSale.DateCreated,
                null,
                createdSale.CustomerName,
                isDetailsLoaded: true);

            newTab.SetRecentlyCreated(true);
            OpenSales.Add(newTab);
            SelectedSale = newTab;
            _ = ClearNewSaleHighlightAsync(newTab);
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "ახალი გაყიდვის შექმნა ვერ მოხერხდა. გთხოვთ, სცადოთ ხელახლა.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ClearNewSaleHighlightAsync(SaleTabViewModel sale)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(4), _lifetimeCancellation.Token);
            sale.SetRecentlyCreated(false);
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
    }

    private async Task AddManualItemAsync()
    {
        var sale = SelectedSale;
        if (sale is null ||
            !ManualItemInput.TryGetValues(
                out var productName,
                out var quantity,
                out var unitPrice))
        {
            ErrorMessage = "შეავსეთ დასახელება და ორი სწორი რიცხვითი მნიშვნელობა.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            if (ManualItemInput.SaveToCatalog)
            {
                var unit = ManualItemInput.SelectedMeasurementUnit
                    ?? throw new InvalidOperationException("Measurement unit is required.");
                var result = await _apiClient.CreateProductAndAddSaleItemAsync(
                    sale.Id,
                    new CreateProductAndAddSaleItemRequest(
                        ManualItemInput.ProductCode.Trim(),
                        productName,
                        ManualItemInput.Barcode!.Trim(),
                        unit.Id,
                        quantity,
                        unitPrice,
                        ManualItemInput.Comment),
                    _lifetimeCancellation.Token);
                ApplyCatalogResult(result);
            }
            else
            {
                var result = await _apiClient.AddManualSaleItemAsync(
                    sale.Id,
                    new AddManualSaleItemRequest(
                        productName,
                        quantity,
                        unitPrice,
                        ManualItemInput.Comment),
                    _lifetimeCancellation.Token);

                sale.AddItem(
                    new SaleItemViewModel(
                        result.SaleItemId,
                        productId: null,
                        productCode: null,
                        barcode: null,
                        result.ProductName,
                        measurementUnitId: null,
                        measurementUnitName: null,
                        result.Quantity,
                        result.UnitPrice,
                        result.LineTotal,
                        isManual: true,
                        result.Comment),
                    result.SaleTotalAmount);
            }

            ManualItemInput.Reset();
            ProductSearch.ClearAndFocus();
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (ProductConflictException exception)
        {
            Trace.TraceWarning(exception.ToString());
            ErrorMessage = exception.Kind == ProductConflictKind.Code
                ? "ეს პროდუქტის კოდი უკვე გამოყენებულია. შეცვალეთ კოდი და თავიდან სცადეთ."
                : "ეს შტრიხკოდი უკვე სხვა პროდუქტზეა გამოყენებული.";
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "პროდუქტის დამატება ვერ მოხერხდა. მონაცემები არ შენახულა.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void EditCustomer()
    {
        var sale = SelectedSale;
        if (sale is null)
        {
            return;
        }

        var result = _dialogService.ShowCustomerInfo(
            sale,
            _lifetimeCancellation.Token);

        if (result is null)
        {
            return;
        }

        sale.ApplyCustomerInfo(
            result.CustomerId,
            result.CustomerName,
            result.CustomerIdentificationNumber,
            result.SaleComment);
        ErrorMessage = null;
    }

    private void EditSelectedItem()
    {
        var sale = SelectedSale;
        var item = SelectedItem;
        if (sale is null || item is null)
        {
            return;
        }

        var result = _dialogService.ShowEditItem(
            sale.Id,
            item,
            _lifetimeCancellation.Token);

        if (result is null)
        {
            return;
        }

        sale.ApplyItemUpdate(
            result.SaleItemId,
            result.ProductName,
            result.Quantity,
            result.UnitPrice,
            result.LineTotal,
            result.Comment,
            result.SaleTotalAmount);
        ErrorMessage = null;
    }

    private async Task RemoveSelectedItemAsync()
    {
        var sale = SelectedSale;
        var item = SelectedItem;
        if (sale is null || item is null || !_dialogService.ConfirmRemoveItem(item.ProductName))
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var result = await _apiClient.RemoveSaleItemAsync(
                sale.Id,
                item.Id,
                _lifetimeCancellation.Token);

            sale.ApplyItemRemoval(result.SaleItemId, result.SaleTotalAmount);
            SelectedItem = null;
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "პროდუქტის წაშლა ვერ მოხერხდა. მონაცემები არ შეცვლილა.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdateDiscountAsync()
    {
        var sale = SelectedSale;
        if (sale is null ||
            !DecimalInputParser.TryParse(sale.DiscountText, out var discountAmount))
        {
            sale?.ApplyFinancials(
                sale.Subtotal,
                sale.DiscountAmount,
                sale.TotalAmount);
            ErrorMessage = "ფასდაკლებაში შეიყვანეთ სწორი რიცხვი.";
            return;
        }

        if (discountAmount == sale.DiscountAmount)
        {
            sale.ApplyFinancials(
                sale.Subtotal,
                sale.DiscountAmount,
                sale.TotalAmount);
            ErrorMessage = null;
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var result = await _apiClient.UpdateSaleDiscountAsync(
                sale.Id,
                discountAmount,
                _lifetimeCancellation.Token);
            sale.ApplyFinancials(
                result.Subtotal,
                result.DiscountAmount,
                result.TotalAmount);
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
            sale.ApplyFinancials(sale.Subtotal, sale.DiscountAmount, sale.TotalAmount);
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            sale.ApplyFinancials(sale.Subtotal, sale.DiscountAmount, sale.TotalAmount);
            ErrorMessage = exception is SaleOperationException
                ? exception.Message
                : "ფასდაკლების შენახვა ვერ მოხერხდა.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CompleteSale()
    {
        var sale = SelectedSale;
        if (sale is null)
        {
            return;
        }

        var result = _dialogService.ShowCompleteSale(
            sale,
            _lifetimeCancellation.Token);

        if (result is null)
        {
            return;
        }

        RemoveOpenSale(sale);
        ErrorMessage = null;
    }

    private void PrintSale()
    {
        if (SelectedSale is { } sale)
        {
            _dialogService.ShowSaleReporting(sale);
        }
    }

    private async Task CancelSaleAsync()
    {
        var sale = SelectedSale;
        if (sale is null ||
            !_dialogService.ConfirmCancelSale(sale.SaleNumber, sale.TotalAmount))
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            await _apiClient.CancelSaleAsync(
                sale.Id,
                _lifetimeCancellation.Token);

            RemoveOpenSale(sale);
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "გაყიდვის გაუქმება ვერ მოხერხდა. მონაცემები არ შეცვლილა.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadSaleDetailsAsync(SaleTabViewModel? sale)
    {
        _detailsCancellation?.Cancel();
        _detailsCancellation?.Dispose();
        _detailsCancellation = null;

        if (sale is null || sale.IsDetailsLoaded)
        {
            NotifyCommandStates();
            return;
        }

        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            _lifetimeCancellation.Token);
        _detailsCancellation = cancellation;

        try
        {
            var details = await _apiClient.GetDraftSaleDetailsAsync(
                sale.Id,
                cancellation.Token);

            if (SelectedSale?.Id != sale.Id)
            {
                return;
            }

            sale.ApplyDetails(
                details.Subtotal,
                details.DiscountAmount,
                details.TotalAmount,
                details.CustomerId,
                details.CustomerName,
                details.CustomerIdentificationNumber,
                details.Comment,
                details.Items.Select(Map),
                details.CompletionVersion,
                details.PaidAmount,
                details.OutstandingAmount,
                details.HasDebt,
                details.PreviousCompletionPaymentState);
            ErrorMessage = null;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "გაყიდვის დეტალების ჩატვირთვა ვერ მოხერხდა.";
        }
        finally
        {
            if (ReferenceEquals(_detailsCancellation, cancellation))
            {
                _detailsCancellation = null;
                cancellation.Dispose();
            }

            NotifyCommandStates();
        }
    }

    private bool CanAddManualItem()
        => !IsBusy &&
           SelectedSale?.IsDetailsLoaded == true &&
           ManualItemInput.CanSubmit;

    private bool CanEditCustomer()
        => !IsBusy && SelectedSale?.IsDetailsLoaded == true;

    private bool CanModifySelectedItem()
        => !IsBusy &&
           SelectedSale?.IsDetailsLoaded == true &&
           SelectedItem is not null;

    private bool CanCompleteSale()
        => !IsBusy &&
           SelectedSale?.IsDetailsLoaded == true &&
           SelectedSale.Items.Count > 0;

    private bool CanCancelSale()
        => !IsBusy && SelectedSale?.IsDetailsLoaded == true;

    private bool CanPrintSale()
        => !IsBusy &&
           SelectedSale?.IsDetailsLoaded == true &&
           SelectedSale.Items.Count > 0;

    private void OnManualItemInputPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SaleItemInputViewModel.SaveToCatalog))
        {
            if (ManualItemInput.SaveToCatalog)
            {
                _ = LoadProductCreationDefaultsAsync();
            }
            else
            {
                _catalogDefaultsCancellation?.Cancel();
            }
        }

        if (e.PropertyName is nameof(SaleItemInputViewModel.IsComplete) or
            nameof(SaleItemInputViewModel.CanSubmit))
        {
            _addManualItemCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task LoadProductCreationDefaultsAsync()
    {
        _catalogDefaultsCancellation?.Cancel();
        _catalogDefaultsCancellation?.Dispose();
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            _lifetimeCancellation.Token);
        _catalogDefaultsCancellation = cancellation;
        ManualItemInput.SetCatalogDefaultsLoading(true);

        try
        {
            var defaults = await _apiClient.GetProductCreationDefaultsAsync(
                cancellation.Token);
            if (ManualItemInput.SaveToCatalog && !cancellation.IsCancellationRequested)
            {
                ManualItemInput.ApplyCreationDefaults(defaults);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            if (ManualItemInput.SaveToCatalog)
            {
                ManualItemInput.SetCatalogError(
                    "პროდუქტის კოდისა და ნაგულისხმევი ერთეულის მიღება ვერ მოხერხდა.");
            }
        }
        finally
        {
            if (ReferenceEquals(_catalogDefaultsCancellation, cancellation))
            {
                _catalogDefaultsCancellation = null;
                cancellation.Dispose();
            }
        }
    }

    private void NotifyCommandStates()
    {
        _newSaleCommand.NotifyCanExecuteChanged();
        _addManualItemCommand.NotifyCanExecuteChanged();
        _editCustomerCommand.NotifyCanExecuteChanged();
        _editSelectedItemCommand.NotifyCanExecuteChanged();
        _removeSelectedItemCommand.NotifyCanExecuteChanged();
        _completeSaleCommand.NotifyCanExecuteChanged();
        _cancelSaleCommand.NotifyCanExecuteChanged();
        _updateDiscountCommand.NotifyCanExecuteChanged();
        _printSaleCommand.NotifyCanExecuteChanged();
    }

    private void RemoveOpenSale(SaleTabViewModel sale)
    {
        var removedIndex = OpenSales.IndexOf(sale);
        if (removedIndex < 0)
        {
            return;
        }

        OpenSales.RemoveAt(removedIndex);
        SelectedSale = OpenSales.Count == 0
            ? null
            : OpenSales[Math.Min(removedIndex, OpenSales.Count - 1)];
    }

    private static SaleTabViewModel Map(DraftSaleDto sale)
        => new(
            sale.Id,
            sale.SaleNumber,
            sale.Subtotal,
            sale.DiscountAmount,
            sale.TotalAmount,
            sale.DateCreated,
            sale.CustomerId,
            sale.CustomerName);

    private static SaleItemViewModel Map(DraftSaleItemDto item)
        => new(
            item.Id,
            item.ProductId,
            item.ProductCode,
            item.Barcode,
            item.ProductName,
            item.UnitId,
            item.UnitName,
            item.Quantity,
            item.UnitPrice,
            item.LineTotal,
            item.IsManual,
            item.Comment);

    private void OnProductAdded(object? sender, ProductAddedEventArgs e)
    {
        ApplyCatalogResult(e.Result);
        ErrorMessage = null;
    }

    private void ApplyCatalogResult(AddProductSaleItemResponse result)
    {
        var sale = OpenSales.SingleOrDefault(openSale => openSale.Id == result.SaleId);
        if (sale is null)
        {
            return;
        }

        sale.ApplyCatalogItem(
            new SaleItemViewModel(
                result.SaleItemId,
                result.ProductId,
                result.ProductCode,
                result.Barcode,
                result.ProductName,
                result.MeasurementUnitId,
                result.MeasurementUnitName,
                result.Quantity,
                result.UnitPrice,
                result.LineTotal,
                result.IsManual,
                result.Comment),
            result.WasNewItem,
            result.SaleTotalAmount);
    }

    private void OnManualFallbackRequested(
        object? sender,
        ManualProductFallbackEventArgs e)
    {
        ManualItemInput.PrepareManualFallback(e.Value, e.IsBarcode);
        ErrorMessage = "პროდუქტი კატალოგში ვერ მოიძებნა — შეავსეთ ხელით.";
        ProductNameFocusRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnProductSearchError(object? sender, ProductSearchErrorEventArgs e)
    {
        if (e.Exception is not null)
        {
            Trace.TraceError(e.Exception.ToString());
        }

        ErrorMessage = e.Message;
    }
}

public enum SaleItemInlineField
{
    Quantity,
    UnitPrice,
    LineTotal
}
