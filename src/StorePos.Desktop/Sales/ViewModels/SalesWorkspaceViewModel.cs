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
    private SaleTabViewModel? _selectedSale;
    private string? _errorMessage;
    private bool _isBusy;

    public SalesWorkspaceViewModel(IStorePosApiClient apiClient)
    {
        _apiClient = apiClient;
        _newSaleCommand = new AsyncRelayCommand(CreateDraftSaleAsync, () => !IsBusy);
    }

    public ObservableCollection<SaleTabViewModel> OpenSales { get; } = [];

    public SaleTabViewModel? SelectedSale
    {
        get => _selectedSale;
        set => SetProperty(ref _selectedSale, value);
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
            }
        }
    }

    public ICommand NewSaleCommand => _newSaleCommand;

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
                createdSale.CustomerName);

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

    private static SaleTabViewModel Map(DraftSaleDto sale)
        => new(
            sale.Id,
            sale.SaleNumber,
            sale.TotalAmount,
            sale.DateCreated,
            sale.CustomerName);
}
