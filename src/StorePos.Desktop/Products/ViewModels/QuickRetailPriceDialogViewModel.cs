using System.Diagnostics;
using System.Windows.Input;
using StorePos.Desktop.Api;
using StorePos.Desktop.Common;
using StorePos.Desktop.Products.Models;
using StorePos.Desktop.Sales.Dialogs;
using StorePos.Desktop.Sales.Calculations;

namespace StorePos.Desktop.Products.ViewModels;

public sealed class QuickRetailPriceDialogViewModel : ObservableObject
{
    private readonly IStorePosApiClient _apiClient;
    private readonly long _productId;
    private readonly CancellationToken _cancellationToken;
    private readonly AsyncRelayCommand _saveCommand;
    private string _price = string.Empty;
    private string? _errorMessage;

    public QuickRetailPriceDialogViewModel(
        IStorePosApiClient apiClient,
        ProductSearchResultDto product,
        CancellationToken cancellationToken = default)
    {
        _apiClient = apiClient;
        _productId = product.Id;
        _cancellationToken = cancellationToken;
        ProductName = product.Name;
        ProductCode = product.Code;
        CurrentPrice = product.Price;
        _saveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        CancelCommand = new RelayCommand(
            () => CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(false)));
    }

    public event EventHandler<DialogCloseRequestedEventArgs>? CloseRequested;

    public string ProductName { get; }

    public string ProductCode { get; }

    public decimal CurrentPrice { get; }

    public string Price
    {
        get => _price;
        set
        {
            if (SetProperty(ref _price, value ?? string.Empty))
            {
                ErrorMessage = string.IsNullOrWhiteSpace(_price) || CanSave()
                    ? null
                    : "მიუთითეთ 0-ზე მეტი საცალო ფასი.";
                _saveCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public UpdateProductRetailPriceDto? Result { get; private set; }

    public ICommand SaveCommand => _saveCommand;

    public ICommand CancelCommand { get; }

    private bool CanSave()
        => TryGetNormalizedPrice(out _);

    private async Task SaveAsync()
    {
        if (!TryGetNormalizedPrice(out var price))
        {
            ErrorMessage = "მიუთითეთ 0-ზე მეტი საცალო ფასი.";
            return;
        }

        try
        {
            ErrorMessage = null;
            Result = await _apiClient.UpdateProductRetailPriceAsync(
                _productId,
                new UpdateProductRetailPriceRequest(price),
                _cancellationToken);
            CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(true));
        }
        catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "საცალო ფასის შენახვა ვერ მოხერხდა.";
        }
    }

    private bool TryGetNormalizedPrice(out decimal price)
    {
        price = default;
        if (!DecimalInputParser.TryParse(Price, out var parsed))
        {
            return false;
        }

        price = FinancialInputPrecision.RoundUnitPrice(parsed);
        return price > 0 && price <= FinancialInputPrecision.MaximumFiveScaleValue;
    }
}
