using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using StorePos.Desktop.Api;
using StorePos.Desktop.Common;
using StorePos.Desktop.Sales.Calculations;
using StorePos.Desktop.Sales.Dialogs;
using StorePos.Desktop.Sales.Models;

namespace StorePos.Desktop.Sales.ViewModels;

public sealed class CompleteSaleDialogViewModel : ObservableObject
{
    private const int CashPaymentType = 1;
    private const int CardPaymentType = 2;
    private const int BankTransferPaymentType = 3;
    private const int OtherPaymentType = 4;

    private readonly IStorePosApiClient _apiClient;
    private readonly long _saleId;
    private readonly CancellationToken _cancellationToken;
    private readonly AsyncRelayCommand _completeCommand;
    private string? _cashAmount;
    private string? _cardAmount;
    private string? _bankTransferAmount;
    private string? _otherAmount;
    private decimal _paidAmount;
    private decimal _remainingAmount;
    private bool _canComplete;
    private string? _errorMessage;

    public CompleteSaleDialogViewModel(
        IStorePosApiClient apiClient,
        long saleId,
        decimal totalAmount,
        CancellationToken cancellationToken)
    {
        _apiClient = apiClient;
        _saleId = saleId;
        _cancellationToken = cancellationToken;
        TotalAmount = totalAmount;
        _completeCommand = new AsyncRelayCommand(CompleteAsync, () => CanComplete);
        CancelCommand = new RelayCommand(
            () => CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(false)));
        Recalculate();
    }

    public event EventHandler<DialogCloseRequestedEventArgs>? CloseRequested;

    public decimal TotalAmount { get; }

    public string? CashAmount
    {
        get => _cashAmount;
        set => SetPaymentInput(ref _cashAmount, value);
    }

    public string? CardAmount
    {
        get => _cardAmount;
        set => SetPaymentInput(ref _cardAmount, value);
    }

    public string? BankTransferAmount
    {
        get => _bankTransferAmount;
        set => SetPaymentInput(ref _bankTransferAmount, value);
    }

    public string? OtherAmount
    {
        get => _otherAmount;
        set => SetPaymentInput(ref _otherAmount, value);
    }

    public decimal PaidAmount
    {
        get => _paidAmount;
        private set => SetProperty(ref _paidAmount, value);
    }

    public decimal RemainingAmount
    {
        get => _remainingAmount;
        private set => SetProperty(ref _remainingAmount, value);
    }

    public bool CanComplete
    {
        get => _canComplete;
        private set
        {
            if (SetProperty(ref _canComplete, value))
            {
                _completeCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public CompleteSaleResponse? Result { get; private set; }

    public ICommand CompleteCommand => _completeCommand;

    public ICommand CancelCommand { get; }

    private async Task CompleteAsync()
    {
        if (!TryGetPayments(out var payments))
        {
            ErrorMessage = "შეიყვანეთ სწორი, არაუარყოფითი თანხები.";
            return;
        }

        try
        {
            ErrorMessage = null;
            Result = await _apiClient.CompleteSaleAsync(
                _saleId,
                new CompleteSaleRequest(payments),
                _cancellationToken);

            CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(true));
        }
        catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "გაყიდვის დასრულება ვერ მოხერხდა. მონაცემები არ შეცვლილა.";
        }
    }

    private void SetPaymentInput(
        ref string? field,
        string? value,
        [CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref field, value, propertyName))
        {
            return;
        }

        ErrorMessage = null;
        Recalculate();
    }

    private void Recalculate()
    {
        var inputs = GetInputs();
        var amounts = new decimal[inputs.Length];
        var inputsAreValid = true;
        var hasEnteredPayment = false;

        for (var index = 0; index < inputs.Length; index++)
        {
            var input = inputs[index].Value;
            if (string.IsNullOrWhiteSpace(input))
            {
                amounts[index] = 0;
                continue;
            }

            hasEnteredPayment = true;
            if (!DecimalInputParser.TryParse(input, out amounts[index]))
            {
                inputsAreValid = false;
                amounts[index] = 0;
            }
        }

        var calculation = PaymentCalculator.Calculate(TotalAmount, amounts);
        PaidAmount = calculation.PaidAmount;
        RemainingAmount = calculation.RemainingAmount;
        CanComplete = hasEnteredPayment && inputsAreValid && calculation.CanComplete;
    }

    private bool TryGetPayments(out IReadOnlyList<CompleteSalePaymentRequest> payments)
    {
        var result = new List<CompleteSalePaymentRequest>();

        foreach (var input in GetInputs())
        {
            if (string.IsNullOrWhiteSpace(input.Value))
            {
                continue;
            }

            if (!DecimalInputParser.TryParse(input.Value, out var amount) || amount < 0)
            {
                payments = [];
                return false;
            }

            result.Add(new CompleteSalePaymentRequest(input.PaymentType, amount));
        }

        payments = result;
        return result.Count > 0;
    }

    private (int PaymentType, string? Value)[] GetInputs()
        =>
        [
            (CashPaymentType, CashAmount),
            (CardPaymentType, CardAmount),
            (BankTransferPaymentType, BankTransferAmount),
            (OtherPaymentType, OtherAmount)
        ];
}
