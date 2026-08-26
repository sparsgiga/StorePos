using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using StorePos.Desktop.Api;
using StorePos.Desktop.Common;
using StorePos.Desktop.Products.Dialogs;
using StorePos.Desktop.Products.Models;

namespace StorePos.Desktop.Products.ViewModels;

public sealed class ProductsViewModel : ObservableObject, IDisposable
{
    private const int PageSize = 50;
    private readonly IStorePosApiClient _apiClient;
    private readonly IProductDialogService _dialogService;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly AsyncRelayCommand _searchCommand;
    private readonly AsyncRelayCommand _refreshCommand;
    private readonly AsyncRelayCommand _createCommand;
    private readonly AsyncRelayCommand _editCommand;
    private readonly AsyncRelayCommand _toggleActiveCommand;
    private readonly AsyncRelayCommand _previousPageCommand;
    private readonly AsyncRelayCommand _nextPageCommand;
    private bool _filtersLoaded;
    private string? _search;
    private ProductStatusOption _selectedStatus;
    private MeasurementUnitFilterOption _selectedMeasurementUnit;
    private string? _priceFrom;
    private string? _priceTo;
    private ProductListItemDto? _selectedItem;
    private int _pageNumber = 1;
    private int _totalCount;
    private bool _isBusy;
    private string? _errorMessage;

    public ProductsViewModel(
        IStorePosApiClient apiClient,
        IProductDialogService dialogService)
    {
        _apiClient = apiClient;
        _dialogService = dialogService;
        StatusOptions =
        [
            new ProductStatusOption("აქტიური", 1),
            new ProductStatusOption("არააქტიური", 2),
            new ProductStatusOption("ყველა", 0)
        ];
        _selectedStatus = StatusOptions[0];
        _selectedMeasurementUnit = new MeasurementUnitFilterOption(null, "ყველა");
        MeasurementUnitOptions.Add(_selectedMeasurementUnit);
        _searchCommand = new AsyncRelayCommand(SearchAsync, () => !IsBusy);
        _refreshCommand = new AsyncRelayCommand(LoadPageAsync, () => !IsBusy);
        _createCommand = new AsyncRelayCommand(CreateAsync, () => !IsBusy);
        _editCommand = new AsyncRelayCommand(EditAsync, CanEdit);
        _toggleActiveCommand = new AsyncRelayCommand(ToggleActiveAsync, CanEdit);
        _previousPageCommand = new AsyncRelayCommand(PreviousPageAsync, CanGoPrevious);
        _nextPageCommand = new AsyncRelayCommand(NextPageAsync, CanGoNext);
    }

    public ObservableCollection<ProductListItemDto> Items { get; } = [];
    public IReadOnlyList<ProductStatusOption> StatusOptions { get; }
    public ObservableCollection<MeasurementUnitFilterOption> MeasurementUnitOptions { get; } = [];

    public string? Search
    {
        get => _search;
        set => SetProperty(ref _search, value);
    }

    public ProductStatusOption SelectedStatus
    {
        get => _selectedStatus;
        set => SetProperty(ref _selectedStatus, value);
    }

    public MeasurementUnitFilterOption SelectedMeasurementUnit
    {
        get => _selectedMeasurementUnit;
        set => SetProperty(ref _selectedMeasurementUnit, value);
    }

    public string? PriceFrom
    {
        get => _priceFrom;
        set => SetProperty(ref _priceFrom, value);
    }

    public string? PriceTo
    {
        get => _priceTo;
        set => SetProperty(ref _priceTo, value);
    }

    public ProductListItemDto? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                OnPropertyChanged(nameof(ToggleActiveText));
                NotifyCommandStates();
            }
        }
    }

    public string ToggleActiveText => SelectedItem?.IsActive == true
        ? "დეაქტივაცია"
        : "აქტივაცია";

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
    public ICommand RefreshCommand => _refreshCommand;
    public ICommand CreateCommand => _createCommand;
    public ICommand EditCommand => _editCommand;
    public ICommand ToggleActiveCommand => _toggleActiveCommand;
    public ICommand PreviousPageCommand => _previousPageCommand;
    public ICommand NextPageCommand => _nextPageCommand;

    public async Task RefreshAsync()
    {
        try
        {
            if (!_filtersLoaded)
            {
                await LoadMeasurementUnitsAsync();
            }

            await LoadPageAsync();
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "პროდუქტების ჩატვირთვა ვერ მოხერხდა.";
        }
    }

    public void Dispose()
    {
        _lifetimeCancellation.Cancel();
        _lifetimeCancellation.Dispose();
    }

    private async Task LoadMeasurementUnitsAsync()
    {
        var units = await _apiClient.GetMeasurementUnitsAsync(_lifetimeCancellation.Token);
        MeasurementUnitOptions.Clear();
        MeasurementUnitOptions.Add(new MeasurementUnitFilterOption(null, "ყველა"));
        foreach (var unit in units)
        {
            var name = string.IsNullOrWhiteSpace(unit.ShortName)
                ? unit.Name
                : $"{unit.Name} ({unit.ShortName})";
            MeasurementUnitOptions.Add(new MeasurementUnitFilterOption(unit.Id, name));
        }

        SelectedMeasurementUnit = MeasurementUnitOptions[0];
        _filtersLoaded = true;
    }

    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        if (!TryParsePrice(PriceFrom, out var priceFrom) ||
            !TryParsePrice(PriceTo, out var priceTo) ||
            priceFrom.HasValue && priceTo.HasValue && priceFrom > priceTo)
        {
            ErrorMessage = "მიუთითეთ სწორი ფასის დიაპაზონი.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var result = await _apiClient.GetProductsAsync(
                new ProductListFilter(
                    Search,
                    SelectedStatus.Value,
                    SelectedMeasurementUnit.Id,
                    priceFrom,
                    priceTo,
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
            ErrorMessage = "პროდუქტების ჩატვირთვა ვერ მოხერხდა.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateAsync()
    {
        if (await _dialogService.ShowCreateAsync(_lifetimeCancellation.Token))
        {
            PageNumber = 1;
            await LoadPageAsync();
        }
    }

    private async Task EditAsync()
    {
        var item = SelectedItem;
        if (item is not null &&
            await _dialogService.ShowEditAsync(item.Id, _lifetimeCancellation.Token))
        {
            await LoadPageAsync();
        }
    }

    private async Task ToggleActiveAsync()
    {
        var item = SelectedItem;
        if (item is null)
        {
            return;
        }

        if (item.IsActive && !_dialogService.ConfirmDeactivate(item.Name))
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            if (item.IsActive)
            {
                await _apiClient.DeactivateProductAsync(item.Id, _lifetimeCancellation.Token);
            }
            else
            {
                await _apiClient.ActivateProductAsync(item.Id, _lifetimeCancellation.Token);
            }

            await LoadPageAsync();
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "პროდუქტის სტატუსის შეცვლა ვერ მოხერხდა.";
        }
        finally
        {
            IsBusy = false;
        }
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

    private bool CanEdit() => !IsBusy && SelectedItem is not null;
    private bool CanGoPrevious() => !IsBusy && PageNumber > 1;
    private bool CanGoNext() => !IsBusy && PageNumber < TotalPages;

    private void NotifyCommandStates()
    {
        _searchCommand.NotifyCanExecuteChanged();
        _refreshCommand.NotifyCanExecuteChanged();
        _createCommand.NotifyCanExecuteChanged();
        _editCommand.NotifyCanExecuteChanged();
        _toggleActiveCommand.NotifyCanExecuteChanged();
        _previousPageCommand.NotifyCanExecuteChanged();
        _nextPageCommand.NotifyCanExecuteChanged();
    }

    private static bool TryParsePrice(string? value, out decimal? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!DecimalInputParser.TryParse(value, out var parsed) || parsed < 0)
        {
            return false;
        }

        result = parsed;
        return true;
    }
}
