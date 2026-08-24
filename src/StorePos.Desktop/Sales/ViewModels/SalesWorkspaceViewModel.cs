using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using StorePos.Desktop.Api;
using StorePos.Desktop.Common;
using StorePos.Desktop.Sales.Models;

namespace StorePos.Desktop.Sales.ViewModels;

public sealed class SalesWorkspaceViewModel : ObservableObject, IDisposable
{
    private readonly IStorePosApiClient _apiClient;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly AsyncRelayCommand _newSaleCommand;
    private readonly AsyncRelayCommand _addManualItemCommand;
    private CancellationTokenSource? _detailsCancellation;
    private SaleTabViewModel? _selectedSale;
    private string? _manualProductName;
    private string _manualQuantity = "1";
    private string _manualUnitPrice = "0";
    private string? _manualComment;
    private string? _errorMessage;
    private bool _isBusy;

    public SalesWorkspaceViewModel(IStorePosApiClient apiClient)
    {
        _apiClient = apiClient;
        _newSaleCommand = new AsyncRelayCommand(CreateDraftSaleAsync, () => !IsBusy);
        _addManualItemCommand = new AsyncRelayCommand(
            AddManualItemAsync,
            () => !IsBusy && SelectedSale?.IsDetailsLoaded == true);
    }

    public event EventHandler? ProductNameFocusRequested;

    public ObservableCollection<SaleTabViewModel> OpenSales { get; } = [];

    public SaleTabViewModel? SelectedSale
    {
        get => _selectedSale;
        set
        {
            if (!SetProperty(ref _selectedSale, value))
            {
                return;
            }

            _addManualItemCommand.NotifyCanExecuteChanged();
            _ = LoadSaleDetailsAsync(value);
        }
    }

    public string? ManualProductName
    {
        get => _manualProductName;
        set => SetProperty(ref _manualProductName, value);
    }

    public string ManualQuantity
    {
        get => _manualQuantity;
        set => SetProperty(ref _manualQuantity, value);
    }

    public string ManualUnitPrice
    {
        get => _manualUnitPrice;
        set => SetProperty(ref _manualUnitPrice, value);
    }

    public string? ManualComment
    {
        get => _manualComment;
        set => SetProperty(ref _manualComment, value);
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
                _newSaleCommand.NotifyCanExecuteChanged();
                _addManualItemCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public ICommand NewSaleCommand => _newSaleCommand;

    public ICommand AddManualItemCommand => _addManualItemCommand;

    public async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var drafts = await _apiClient.GetDraftSalesAsync(_lifetimeCancellation.Token);
            var loadedTabs = drafts.Select(Map).ToArray();

            OpenSales.Clear();
            foreach (var tab in loadedTabs)
            {
                OpenSales.Add(tab);
            }

            SelectedSale = OpenSales.FirstOrDefault();
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
                createdSale.DateCreated,
                createdSale.CustomerName,
                isDetailsLoaded: true);

            OpenSales.Add(newTab);
            SelectedSale = newTab;
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

    private async Task AddManualItemAsync()
    {
        var sale = SelectedSale;
        if (sale is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ManualProductName))
        {
            ErrorMessage = "შეიყვანეთ პროდუქტის დასახელება.";
            ProductNameFocusRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (!DecimalInputParser.TryParse(ManualQuantity, out var quantity) || quantity <= 0)
        {
            ErrorMessage = "რაოდენობა უნდა იყოს ნულზე მეტი.";
            return;
        }

        if (!DecimalInputParser.TryParse(ManualUnitPrice, out var unitPrice) || unitPrice < 0)
        {
            ErrorMessage = "ფასი არ შეიძლება იყოს უარყოფითი.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var result = await _apiClient.AddManualSaleItemAsync(
                sale.Id,
                new AddManualSaleItemRequest(
                    ManualProductName,
                    quantity,
                    unitPrice,
                    ManualComment),
                _lifetimeCancellation.Token);

            sale.AddItem(
                new SaleItemViewModel(
                    result.SaleItemId,
                    result.ProductName,
                    result.Quantity,
                    result.UnitPrice,
                    result.LineTotal,
                    IsManual: true,
                    result.Comment),
                result.SaleTotalAmount);

            ManualProductName = null;
            ManualQuantity = "1";
            ManualUnitPrice = "0";
            ManualComment = null;

            ProductNameFocusRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
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

    private async Task LoadSaleDetailsAsync(SaleTabViewModel? sale)
    {
        _detailsCancellation?.Cancel();
        _detailsCancellation?.Dispose();
        _detailsCancellation = null;

        if (sale is null || sale.IsDetailsLoaded)
        {
            _addManualItemCommand.NotifyCanExecuteChanged();
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
                details.TotalAmount,
                details.Items.Select(Map));

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

            _addManualItemCommand.NotifyCanExecuteChanged();
        }
    }

    private static SaleTabViewModel Map(DraftSaleDto sale)
        => new(
            sale.Id,
            sale.SaleNumber,
            sale.TotalAmount,
            sale.DateCreated,
            sale.CustomerName);

    private static SaleItemViewModel Map(DraftSaleItemDto item)
        => new(
            item.Id,
            item.ProductName,
            item.Quantity,
            item.UnitPrice,
            item.LineTotal,
            item.IsManual,
            item.Comment);
}
