using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using StorePos.Desktop.Api;
using StorePos.Desktop.Common;
using StorePos.Desktop.History.Dialogs;
using StorePos.Desktop.History.Models;

namespace StorePos.Desktop.History.ViewModels;

public sealed class SalesHistoryViewModel : ObservableObject, IDisposable
{
    private const int PageSize = 50;

    private readonly IStorePosApiClient _apiClient;
    private readonly IHistoryDialogService _dialogService;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly AsyncRelayCommand _searchCommand;
    private readonly AsyncRelayCommand _previousPageCommand;
    private readonly AsyncRelayCommand _nextPageCommand;
    private readonly AsyncRelayCommand _openDetailsCommand;
    private readonly AsyncRelayCommand _reopenCommand;
    private DateTime? _dateFrom = DateTime.Today;
    private DateTime? _dateTo = DateTime.Today;
    private string? _saleNumber;
    private string? _customerName;
    private SaleStatusFilterOption _selectedStatus;
    private SalesHistoryItemDto? _selectedItem;
    private int _pageNumber = 1;
    private int _totalCount;
    private bool _isBusy;
    private string? _errorMessage;

    public SalesHistoryViewModel(
        IStorePosApiClient apiClient,
        IHistoryDialogService dialogService)
    {
        _apiClient = apiClient;
        _dialogService = dialogService;
        StatusOptions =
        [
            new SaleStatusFilterOption("ყველა", null),
            new SaleStatusFilterOption("Draft", 1),
            new SaleStatusFilterOption("დასრულებული", 2),
            new SaleStatusFilterOption("გაუქმებული", 3)
        ];
        _selectedStatus = StatusOptions[2];

        _searchCommand = new AsyncRelayCommand(SearchAsync, () => !IsBusy);
        _previousPageCommand = new AsyncRelayCommand(PreviousPageAsync, CanGoPrevious);
        _nextPageCommand = new AsyncRelayCommand(NextPageAsync, CanGoNext);
        _openDetailsCommand = new AsyncRelayCommand(OpenDetailsAsync, CanOpenDetails);
        _reopenCommand = new AsyncRelayCommand(ReopenAsync, CanReopen);
    }

    public event EventHandler<SaleReopenedEventArgs>? SaleReopened;

    public ObservableCollection<SalesHistoryItemDto> Items { get; } = [];

    public IReadOnlyList<SaleStatusFilterOption> StatusOptions { get; }

    public DateTime? DateFrom
    {
        get => _dateFrom;
        set => SetProperty(ref _dateFrom, value);
    }

    public DateTime? DateTo
    {
        get => _dateTo;
        set => SetProperty(ref _dateTo, value);
    }

    public string? SaleNumber
    {
        get => _saleNumber;
        set => SetProperty(ref _saleNumber, value);
    }

    public string? CustomerName
    {
        get => _customerName;
        set => SetProperty(ref _customerName, value);
    }

    public SaleStatusFilterOption SelectedStatus
    {
        get => _selectedStatus;
        set => SetProperty(ref _selectedStatus, value);
    }

    public SalesHistoryItemDto? SelectedItem
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

    public int PageNumber
    {
        get => _pageNumber;
        private set
        {
            if (SetProperty(ref _pageNumber, value))
            {
                OnPropertyChanged(nameof(PageLabel));
                NotifyCommandStates();
            }
        }
    }

    public int TotalCount
    {
        get => _totalCount;
        private set
        {
            if (SetProperty(ref _totalCount, value))
            {
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(PageLabel));
                NotifyCommandStates();
            }
        }
    }

    public int TotalPages => Math.Max(1, (TotalCount + PageSize - 1) / PageSize);

    public string PageLabel => $"{PageNumber} / {TotalPages}";

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

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public ICommand SearchCommand => _searchCommand;
    public ICommand PreviousPageCommand => _previousPageCommand;
    public ICommand NextPageCommand => _nextPageCommand;
    public ICommand OpenDetailsCommand => _openDetailsCommand;
    public ICommand ReopenCommand => _reopenCommand;

    public Task RefreshAsync() => LoadPageAsync();

    public void Dispose()
    {
        _lifetimeCancellation.Cancel();
        _lifetimeCancellation.Dispose();
    }

    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadPageAsync();
    }

    private async Task PreviousPageAsync()
    {
        PageNumber--;
        await LoadPageAsync();
    }

    private async Task NextPageAsync()
    {
        PageNumber++;
        await LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var result = await _apiClient.GetSalesHistoryAsync(
                new SalesHistoryFilter(
                    DateFrom,
                    DateTo,
                    SaleNumber,
                    CustomerName,
                    SelectedStatus.Value,
                    PageNumber,
                    PageSize),
                _lifetimeCancellation.Token);

            Items.Clear();
            foreach (var item in result.Items)
            {
                Items.Add(item);
            }

            TotalCount = result.TotalCount;
            SelectedItem = Items.FirstOrDefault();
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "გაყიდვების ისტორიის ჩატვირთვა ვერ მოხერხდა.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OpenDetailsAsync()
    {
        var sale = SelectedItem;
        if (sale is null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var details = await _apiClient.GetSaleDetailsAsync(
                sale.Id,
                _lifetimeCancellation.Token);
            var hasFinancialChanges = _dialogService.ShowSaleDetails(
                details,
                _lifetimeCancellation.Token);
            if (hasFinancialChanges)
            {
                await LoadPageAsync();
            }
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "გაყიდვის დეტალების ჩატვირთვა ვერ მოხერხდა.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReopenAsync()
    {
        var sale = SelectedItem;
        if (sale is null || !_dialogService.ConfirmReopen(sale))
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var result = await _apiClient.ReopenSaleAsync(
                sale.Id,
                _lifetimeCancellation.Token);

            await LoadPageAsync();
            if (Items.Count == 0 && PageNumber > 1)
            {
                PageNumber--;
                await LoadPageAsync();
            }

            SaleReopened?.Invoke(this, new SaleReopenedEventArgs(result.SaleId));
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (SaleOperationException exception)
        {
            Trace.TraceWarning(exception.ToString());
            ErrorMessage = "ამ გაყიდვაზე უკვე დაფიქსირებულია ვალის შემდგომი გადახდა და Draft-ზე დაბრუნება აღარ შეიძლება.";
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "გაყიდვის Draft-ზე დაბრუნება ვერ მოხერხდა.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanGoPrevious() => !IsBusy && PageNumber > 1;
    private bool CanGoNext() => !IsBusy && PageNumber < TotalPages;
    private bool CanOpenDetails() => !IsBusy && SelectedItem is not null;
    private bool CanReopen() => !IsBusy && SelectedItem?.Status == 2;

    private void NotifyCommandStates()
    {
        _searchCommand.NotifyCanExecuteChanged();
        _previousPageCommand.NotifyCanExecuteChanged();
        _nextPageCommand.NotifyCanExecuteChanged();
        _openDetailsCommand.NotifyCanExecuteChanged();
        _reopenCommand.NotifyCanExecuteChanged();
    }

}
