using StorePos.Domain.Base;
using StorePos.Domain.Common;
using StorePos.Domain.Enums;

namespace StorePos.Domain.Aggregates.Sale;

public sealed class SalePayment : AuditableEntity<long>
{
    private SalePayment()
    {
    }

    private SalePayment(
        long saleId,
        int completionVersion,
        PaymentType paymentType,
        SalePaymentKind paymentKind,
        decimal amount,
        Guid? operationId)
    {
        SaleId = saleId;
        CompletionVersion = completionVersion;
        PaymentType = paymentType;
        PaymentKind = paymentKind;
        Amount = amount;
        OperationId = operationId;
    }

    public long SaleId { get; private set; }

    public int CompletionVersion { get; private set; }

    public PaymentType PaymentType { get; private set; }

    public SalePaymentKind PaymentKind { get; private set; }

    public decimal Amount { get; private set; }

    public Guid? OperationId { get; private set; }

    internal static SalePayment CreateCompletion(
        long saleId,
        int completionVersion,
        PaymentType paymentType,
        decimal amount)
        => Create(
            saleId,
            completionVersion,
            paymentType,
            SalePaymentKind.Completion,
            amount,
            null);

    internal static SalePayment CreateDebtRepayment(
        long saleId,
        int completionVersion,
        PaymentType paymentType,
        decimal amount,
        Guid operationId)
    {
        if (operationId == Guid.Empty)
        {
            throw new ArgumentException("Operation ID is required.", nameof(operationId));
        }

        return Create(
            saleId,
            completionVersion,
            paymentType,
            SalePaymentKind.DebtRepayment,
            amount,
            operationId);
    }

    private static SalePayment Create(
        long saleId,
        int completionVersion,
        PaymentType paymentType,
        SalePaymentKind paymentKind,
        decimal amount,
        Guid? operationId)
    {
        if (completionVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(completionVersion),
                "Completion version must be greater than zero.");
        }

        if (!Enum.IsDefined(paymentType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(paymentType),
                "Payment type is not supported.");
        }

        if (!Enum.IsDefined(paymentKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(paymentKind),
                "Payment kind is not supported.");
        }

        var roundedAmount = RoundAmount(amount);
        if (roundedAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Payment amount must be greater than zero.");
        }

        return new SalePayment(
            saleId,
            completionVersion,
            paymentType,
            paymentKind,
            roundedAmount,
            operationId);
    }

    public static decimal RoundAmount(decimal amount)
        => FinancialPrecision.RoundMoney(amount);
}
