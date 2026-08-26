using System.Windows.Input;
using StorePos.Desktop.Common;
using StorePos.Desktop.History.ViewModels;
using StorePos.Desktop.Products.ViewModels;
using StorePos.Desktop.Sales.ViewModels;

namespace StorePos.Desktop;

public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly AsyncRelayCommand _showSalesCommand;
    private readonly AsyncRelayCommand _showHistoryCommand;
    private readonly AsyncRelayCommand _showSoldProductsCommand;
    private readonly AsyncRelayCommand _showProductsCommand;
    private readonly Func<ProductsViewModel> _productsFactory;
    private ProductsViewModel? _products;
    private object _currentPage;

    public MainWindowViewModel(
        SalesWorkspaceViewModel salesWorkspace,
        SalesHistoryViewModel salesHistory,
        SoldProductsViewModel soldProducts,
        Func<ProductsViewModel> productsFactory)
    {
        SalesWorkspace = salesWorkspace;
        SalesHistory = salesHistory;
        SoldProducts = soldProducts;
        _productsFactory = productsFactory;
        _currentPage = salesWorkspace;

        _showSalesCommand = new AsyncRelayCommand(ShowSalesAsync);
        _showHistoryCommand = new AsyncRelayCommand(ShowHistoryAsync);
        _showSoldProductsCommand = new AsyncRelayCommand(ShowSoldProductsAsync);
        _showProductsCommand = new AsyncRelayCommand(ShowProductsAsync);
        SalesHistory.SaleReopened += OnSaleReopened;
    }

    public SalesWorkspaceViewModel SalesWorkspace { get; }
    public SalesHistoryViewModel SalesHistory { get; }
    public SoldProductsViewModel SoldProducts { get; }
    public ProductsViewModel? Products => _products;

    public object CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public ICommand ShowSalesCommand => _showSalesCommand;
    public ICommand ShowHistoryCommand => _showHistoryCommand;
    public ICommand ShowSoldProductsCommand => _showSoldProductsCommand;
    public ICommand ShowProductsCommand => _showProductsCommand;

    public Task InitializeAsync() => SalesWorkspace.InitializeAsync();

    public void Dispose()
    {
        SalesHistory.SaleReopened -= OnSaleReopened;
        SalesWorkspace.Dispose();
        SalesHistory.Dispose();
        SoldProducts.Dispose();
        _products?.Dispose();
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

    private async Task ShowProductsAsync()
    {
        if (_products is null)
        {
            _products = _productsFactory();
            OnPropertyChanged(nameof(Products));
        }

        CurrentPage = _products;
        await _products.RefreshAsync();
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
