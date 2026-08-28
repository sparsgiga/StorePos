using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using StorePos.Desktop.Api;
using StorePos.Desktop.Common;
using StorePos.Desktop.Sales.Dialogs;
using StorePos.Desktop.Sales.Models;

namespace StorePos.Desktop.Sales.ViewModels;

public sealed class EditSaleItemDialogViewModel : ObservableObject
{
    private readonly IStorePosApiClient _apiClient;
    private readonly long _saleId;
    private readonly long _saleItemId;
    private readonly CancellationToken _cancellationToken;
    private readonly AsyncRelayCommand _saveCommand;
    private string? _errorMessage;

    public EditSaleItemDialogViewModel(
        IStorePosApiClient apiClient,
        long saleId,
        SaleItemViewModel item,
        CancellationToken cancellationToken)
    {
        _apiClient = apiClient;
        _saleId = saleId;
        _saleItemId = item.Id;
        _cancellationToken = cancellationToken;

        Input.Load(
            item.ProductName,
            item.Quantity,
            item.UnitPrice,
            item.Comment,
            isProductNameReadOnly: !item.IsManual);
        Input.PropertyChanged += OnInputPropertyChanged;

        _saveCommand = new AsyncRelayCommand(SaveAsync, () => Input.IsComplete);
        CancelCommand = new RelayCommand(
            () => CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(false)));
    }

    public event EventHandler<DialogCloseRequestedEventArgs>? CloseRequested;

    public SaleItemInputViewModel Input { get; } = new();

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public UpdateSaleItemResponse? Result { get; private set; }

    public ICommand SaveCommand => _saveCommand;

    public ICommand CancelCommand { get; }

    private async Task SaveAsync()
    {
        if (!Input.TryGetValues(out var productName, out var quantity, out var unitPrice))
        {
            ErrorMessage = "შეავსეთ დასახელება და ორი სწორი რიცხვითი მნიშვნელობა.";
            return;
        }

        try
        {
            ErrorMessage = null;
            Result = await _apiClient.UpdateSaleItemAsync(
                _saleId,
                _saleItemId,
                new UpdateSaleItemRequest(
                    productName,
                    quantity,
                    unitPrice,
                    Input.Comment),
                _cancellationToken);

            CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(true));
        }
        catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = exception is SaleOperationException
                ? exception.Message
                : "პროდუქტის ცვლილების შენახვა ვერ მოხერხდა.";
        }
    }

    private void OnInputPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SaleItemInputViewModel.IsComplete))
        {
            _saveCommand.NotifyCanExecuteChanged();
        }
    }
}
