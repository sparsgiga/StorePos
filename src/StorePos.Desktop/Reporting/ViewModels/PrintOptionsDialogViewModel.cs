using System.Windows.Input;
using StorePos.Desktop.Common;
using StorePos.Desktop.Reporting.Models;

namespace StorePos.Desktop.Reporting.ViewModels;

public sealed class PrintOptionsDialogViewModel : ObservableObject
{
    private readonly FullSaleReportModel _report;
    private readonly Func<FullSaleReportModel, Task> _previewFullSale;
    private readonly Func<FullSaleReportModel, Task> _openLoadingList;
    private readonly Func<FullSaleReportModel, Task> _exportExcel;
    private readonly AsyncRelayCommand _continueCommand;
    private readonly AsyncRelayCommand _exportCommand;
    private bool _isFullSaleSelected = true;
    private bool _isBusy;
    private string? _errorMessage;

    public PrintOptionsDialogViewModel(
        FullSaleReportModel report,
        Func<FullSaleReportModel, Task> previewFullSale,
        Func<FullSaleReportModel, Task> openLoadingList,
        Func<FullSaleReportModel, Task> exportExcel)
    {
        _report = report;
        _previewFullSale = previewFullSale;
        _openLoadingList = openLoadingList;
        _exportExcel = exportExcel;
        _continueCommand = new AsyncRelayCommand(ContinueAsync, () => !IsBusy);
        _exportCommand = new AsyncRelayCommand(ExportAsync, () => !IsBusy && IsFullSaleSelected);
    }

    public string SaleNumber => _report.SaleNumber;

    public bool IsFullSaleSelected
    {
        get => _isFullSaleSelected;
        set
        {
            if (SetProperty(ref _isFullSaleSelected, value))
            {
                OnPropertyChanged(nameof(IsLoadingListSelected));
                _exportCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsLoadingListSelected
    {
        get => !IsFullSaleSelected;
        set
        {
            if (value)
            {
                IsFullSaleSelected = false;
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                _continueCommand.NotifyCanExecuteChanged();
                _exportCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public ICommand ContinueCommand => _continueCommand;
    public ICommand ExportExcelCommand => _exportCommand;

    private async Task ContinueAsync()
        => await ExecuteAsync(() => IsFullSaleSelected
            ? _previewFullSale(_report)
            : _openLoadingList(_report));

    private async Task ExportAsync()
        => await ExecuteAsync(() => _exportExcel(_report));

    private async Task ExecuteAsync(Func<Task> action)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await action();
        }
        catch (Exception exception)
        {
            ErrorMessage = $"ოპერაცია ვერ შესრულდა: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
