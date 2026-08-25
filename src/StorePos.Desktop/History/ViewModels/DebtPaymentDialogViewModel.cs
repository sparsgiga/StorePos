using System.Diagnostics;
using System.Windows.Input;
using StorePos.Desktop.Api;
using StorePos.Desktop.Common;
using StorePos.Desktop.History.Models;
using StorePos.Desktop.Sales.Calculations;
using StorePos.Desktop.Sales.Dialogs;

namespace StorePos.Desktop.History.ViewModels;

public sealed class DebtPaymentDialogViewModel : ObservableObject
{
    private readonly IStorePosApiClient _apiClient;
    private readonly long _saleId;
    private readonly CancellationToken _cancellationToken;
    private readonly AsyncRelayCommand _payCommand;
    private string? _amount;
    private PaymentTypeOption _selectedPaymentType;
    private string? _errorMessage;

    public DebtPaymentDialogViewModel(
        IStorePosApiClient apiClient,
        long saleId,
        decimal outstandingAmount,
        CancellationToken cancellationToken)
    {
        _apiClient = apiClient;
        _saleId = saleId;
        _cancellationToken = cancellationToken;
        OutstandingAmount = outstandingAmount;
        PaymentTypes =
        [
            new PaymentTypeOption(1, "ნაღდი"),
            new PaymentTypeOption(2, "ბარათი"),
            new PaymentTypeOption(3, "გადარიცხვა"),
            new PaymentTypeOption(4, "სხვა")
        ];
        _selectedPaymentType = PaymentTypes[0];
        _payCommand = new AsyncRelayCommand(PayAsync, CanPay);
        CancelCommand = new RelayCommand(
            () => CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(false)));
    }

    public event EventHandler<DialogCloseRequestedEventArgs>? CloseRequested;

    public decimal OutstandingAmount { get; }

    public IReadOnlyList<PaymentTypeOption> PaymentTypes { get; }

    public string? Amount
    {
        get => _amount;
        set
        {
            if (SetProperty(ref _amount, value))
            {
                ErrorMessage = null;
                _payCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public PaymentTypeOption SelectedPaymentType
    {
        get => _selectedPaymentType;
        set
        {
            if (SetProperty(ref _selectedPaymentType, value))
            {
                _payCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public AddDebtPaymentResponse? Result { get; private set; }

    public ICommand PayCommand => _payCommand;

    public ICommand CancelCommand { get; }

    private bool CanPay()
        => DecimalInputParser.TryParse(Amount, out var amount) &&
           amount > 0 &&
           decimal.Round(amount, 5, MidpointRounding.AwayFromZero) <= OutstandingAmount;

    private async Task PayAsync()
    {
        if (!DecimalInputParser.TryParse(Amount, out var amount))
        {
            ErrorMessage = "შეიყვანეთ სწორი თანხა.";
            return;
        }

        amount = decimal.Round(amount, 5, MidpointRounding.AwayFromZero);
        if (amount <= 0 || amount > OutstandingAmount)
        {
            ErrorMessage = "თანხა უნდა იყოს ნულზე მეტი და არ აღემატებოდეს დარჩენილ ვალს.";
            return;
        }

        try
        {
            ErrorMessage = null;
            Result = await _apiClient.AddDebtPaymentAsync(
                _saleId,
                new AddDebtPaymentRequest(SelectedPaymentType.Value, amount),
                _cancellationToken);
            CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(true));
        }
        catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
        {
        }
        catch (SaleOperationException exception)
        {
            Trace.TraceWarning(exception.ToString());
            ErrorMessage = exception.Message;
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "ვალის გადახდა ვერ დაფიქსირდა.";
        }
    }
}
