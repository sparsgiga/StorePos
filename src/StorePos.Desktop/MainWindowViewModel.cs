using System.Windows.Input;
using StorePos.Desktop.Common;
using StorePos.Desktop.History.ViewModels;
using StorePos.Desktop.Sales.ViewModels;

namespace StorePos.Desktop;

public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly AsyncRelayCommand _showSalesCommand;
    private readonly AsyncRelayCommand _showHistoryCommand;
    private readonly AsyncRelayCommand _showSoldProductsCommand;
    private object _currentPage;

    public MainWindowViewModel(
        SalesWorkspaceViewModel salesWorkspace,
        SalesHistoryViewModel salesHistory,
        SoldProductsViewModel soldProducts)
    {
        SalesWorkspace = salesWorkspace;
        SalesHistory = salesHistory;
        SoldProducts = soldProducts;
        _currentPage = salesWorkspace;

        _showSalesCommand = new AsyncRelayCommand(ShowSalesAsync);
        _showHistoryCommand = new AsyncRelayCommand(ShowHistoryAsync);
        _showSoldProductsCommand = new AsyncRelayCommand(ShowSoldProductsAsync);
        SalesHistory.SaleReopened += OnSaleReopened;
    }

    public SalesWorkspaceViewModel SalesWorkspace { get; }
    public SalesHistoryViewModel SalesHistory { get; }
    public SoldProductsViewModel SoldProducts { get; }

    public object CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public ICommand ShowSalesCommand => _showSalesCommand;
    public ICommand ShowHistoryCommand => _showHistoryCommand;
    public ICommand ShowSoldProductsCommand => _showSoldProductsCommand;

    public Task InitializeAsync() => SalesWorkspace.InitializeAsync();

    public void Dispose()
    {
        SalesHistory.SaleReopened -= OnSaleReopened;
        SalesWorkspace.Dispose();
        SalesHistory.Dispose();
        SoldProducts.Dispose();
    }

    private async Task ShowSalesAsync()
    {
        CurrentPage = SalesWorkspace;
        await SalesWorkspace.RefreshAsync();
    }

    private async Task ShowHistoryAsync()
    {
        CurrentPage = SalesHistory;
        await SalesHistory.RefreshAsync();
    }

    private async Task ShowSoldProductsAsync()
    {
        CurrentPage = SoldProducts;
        await SoldProducts.RefreshAsync();
    }

    private async void OnSaleReopened(
        object? sender,
        SaleReopenedEventArgs e)
        => await SalesWorkspace.RefreshAsync(e.SaleId);
}
