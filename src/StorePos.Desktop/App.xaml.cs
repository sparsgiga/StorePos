using System.Configuration;
using System.Net.Http;
using System.Diagnostics;
using System.Windows;
using StorePos.Desktop.Api;
using StorePos.Desktop.Common;
using StorePos.Desktop.Configuration;
using StorePos.Desktop.History.Dialogs;
using StorePos.Desktop.History.ViewModels;
using StorePos.Desktop.Products.Dialogs;
using StorePos.Desktop.Products.ViewModels;
using StorePos.Desktop.Sales.Dialogs;
using StorePos.Desktop.Sales.ViewModels;
using StorePos.Desktop.Startup;

namespace StorePos.Desktop;

public partial class App : Application
{
    private HttpClient? _httpClient;
    private MainWindowViewModel? _mainWindowViewModel;
    private StartupWindow? _startupWindow;
    private ApiReadinessService? _apiReadinessService;
    private readonly CancellationTokenSource _startupCancellation = new();
    private bool _isStarting;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            var apiBaseAddress = DesktopConfiguration.LoadApiBaseAddress();
            _httpClient = new HttpClient { BaseAddress = apiBaseAddress };
            _apiReadinessService = new ApiReadinessService(_httpClient);
            _startupWindow = new StartupWindow();
            _startupWindow.RetryRequested += OnRetryRequested;
            _startupWindow.CloseRequested += OnStartupCloseRequested;
            MainWindow = _startupWindow;
            _startupWindow.Show();

            await TryStartApplicationAsync();
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
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
        _startupCancellation.Cancel();
        _startupCancellation.Dispose();
        _mainWindowViewModel?.Dispose();
        _httpClient?.Dispose();
        base.OnExit(e);
    }

    private async Task TryStartApplicationAsync()
    {
        if (_isStarting || _apiReadinessService is null || _startupWindow is null)
        {
            return;
        }

        _isStarting = true;
        _startupWindow.ShowWaiting();

        try
        {
            var isReady = await _apiReadinessService.WaitUntilReadyAsync(
                _startupCancellation.Token);
            if (!isReady)
            {
                _startupWindow.ShowFailure();
                return;
            }

            await StartMainWindowAsync();
        }
        catch (OperationCanceledException) when (_startupCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            _startupWindow.ShowFailure();
        }
        finally
        {
            _isStarting = false;
        }
    }

    private async Task StartMainWindowAsync()
    {
        var apiClient = new StorePosApiClient(_httpClient!);
        var dialogService = new SalesDialogService(apiClient);
        var historyDialogService = new HistoryDialogService(
            apiClient,
            new WindowsClipboardService());
        var salesWorkspace = new SalesWorkspaceViewModel(apiClient, dialogService);
        var salesHistory = new SalesHistoryViewModel(apiClient, historyDialogService);
        var soldProducts = new SoldProductsViewModel(apiClient, historyDialogService);
        var productDialogService = new ProductDialogService(apiClient);
        _mainWindowViewModel = new MainWindowViewModel(
            salesWorkspace,
            salesHistory,
            soldProducts,
            () => new ProductsViewModel(apiClient, productDialogService));

        await _mainWindowViewModel.InitializeAsync();

        var mainWindow = new MainWindow(_mainWindowViewModel);
        MainWindow = mainWindow;
        _startupWindow!.RetryRequested -= OnRetryRequested;
        _startupWindow.CloseRequested -= OnStartupCloseRequested;
        _startupWindow.CloseAfterStartup();
        _startupWindow = null;
        mainWindow.Show();
        ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    private async void OnRetryRequested(object? sender, EventArgs e)
        => await TryStartApplicationAsync();

    private void OnStartupCloseRequested(object? sender, EventArgs e)
        => Shutdown();
}

