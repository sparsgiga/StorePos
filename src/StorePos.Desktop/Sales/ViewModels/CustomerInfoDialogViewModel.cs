using System.Diagnostics;
using System.Windows.Input;
using StorePos.Desktop.Api;
using StorePos.Desktop.Common;
using StorePos.Desktop.Sales.Dialogs;
using StorePos.Desktop.Sales.Models;

namespace StorePos.Desktop.Sales.ViewModels;

public sealed class CustomerInfoDialogViewModel : ObservableObject
{
    private readonly IStorePosApiClient _apiClient;
    private readonly long _saleId;
    private readonly CancellationToken _cancellationToken;
    private readonly AsyncRelayCommand _saveCommand;
    private string? _customerName;
    private string? _customerIdentificationNumber;
    private string? _comment;
    private string? _errorMessage;

    public CustomerInfoDialogViewModel(
        IStorePosApiClient apiClient,
        long saleId,
        string? customerName,
        string? customerIdentificationNumber,
        string? comment,
        CancellationToken cancellationToken)
    {
        _apiClient = apiClient;
        _saleId = saleId;
        _cancellationToken = cancellationToken;
        _customerName = customerName;
        _customerIdentificationNumber = customerIdentificationNumber;
        _comment = comment;
        _saveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(
            () => CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(false)));
    }

    public event EventHandler<DialogCloseRequestedEventArgs>? CloseRequested;

    public string? CustomerName
    {
        get => _customerName;
        set => SetProperty(ref _customerName, value);
    }

    public string? CustomerIdentificationNumber
    {
        get => _customerIdentificationNumber;
        set => SetProperty(ref _customerIdentificationNumber, value);
    }

    public string? Comment
    {
        get => _comment;
        set => SetProperty(ref _comment, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public UpdateDraftSaleInfoResponse? Result { get; private set; }

    public ICommand SaveCommand => _saveCommand;

    public ICommand CancelCommand { get; }

    private async Task SaveAsync()
    {
        try
        {
            ErrorMessage = null;
            Result = await _apiClient.UpdateDraftSaleInfoAsync(
                _saleId,
                new UpdateDraftSaleInfoRequest(
                    CustomerName,
                    CustomerIdentificationNumber,
                    Comment),
                _cancellationToken);

            CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(true));
        }
        catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "მყიდველის ინფორმაციის შენახვა ვერ მოხერხდა.";
        }
    }
}
