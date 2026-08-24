using System.Configuration;
using System.Net.Http;
using System.Windows;
using StorePos.Desktop.Api;
using StorePos.Desktop.Configuration;
using StorePos.Desktop.Sales.ViewModels;

namespace StorePos.Desktop;

public partial class App : Application
{
    private HttpClient? _httpClient;
    private SalesWorkspaceViewModel? _workspaceViewModel;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var apiBaseAddress = DesktopConfiguration.LoadApiBaseAddress();
            _httpClient = new HttpClient { BaseAddress = apiBaseAddress };

            var apiClient = new StorePosApiClient(_httpClient);
            _workspaceViewModel = new SalesWorkspaceViewModel(apiClient);

            var mainWindow = new MainWindow(_workspaceViewModel);
            MainWindow = mainWindow;
            mainWindow.Show();

            await _workspaceViewModel.InitializeAsync();
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
        _workspaceViewModel?.Dispose();
        _httpClient?.Dispose();
        base.OnExit(e);
    }
}

