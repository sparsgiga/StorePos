using System.Configuration;
using System.Net.Http;
using System.Windows;
using StorePos.Desktop.Api;
using StorePos.Desktop.Configuration;
using StorePos.Desktop.History.Dialogs;
using StorePos.Desktop.History.ViewModels;
using StorePos.Desktop.Sales.Dialogs;
using StorePos.Desktop.Sales.ViewModels;

namespace StorePos.Desktop;

public partial class App : Application
{
    private HttpClient? _httpClient;
    private MainWindowViewModel? _mainWindowViewModel;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var apiBaseAddress = DesktopConfiguration.LoadApiBaseAddress();
            _httpClient = new HttpClient { BaseAddress = apiBaseAddress };

            var apiClient = new StorePosApiClient(_httpClient);
            var dialogService = new SalesDialogService(apiClient);
            var historyDialogService = new HistoryDialogService();
            var salesWorkspace = new SalesWorkspaceViewModel(apiClient, dialogService);
            var salesHistory = new SalesHistoryViewModel(apiClient, historyDialogService);
            var soldProducts = new SoldProductsViewModel(apiClient, historyDialogService);
            _mainWindowViewModel = new MainWindowViewModel(
                salesWorkspace,
                salesHistory,
                soldProducts);

            var mainWindow = new MainWindow(_mainWindowViewModel);
            MainWindow = mainWindow;
            mainWindow.Show();

            await _mainWindowViewModel.InitializeAsync();
        }
        catch (Exception)
        {
            MessageBox.Show(
                "პროგრამის კონფიგურაციის ჩატვირთვა ვერ მოხერხდა.",
                "StorePos",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mainWindowViewModel?.Dispose();
        _httpClient?.Dispose();
        base.OnExit(e);
    }
}

