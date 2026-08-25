using StorePos.Domain.Base;
using StorePos.Domain.Enums;

namespace StorePos.Domain.Aggregates.Sale;

public sealed class SalePayment : AuditableEntity<long>
{
    internal const int AmountScale = 5;

    private SalePayment()
    {
    }

    private SalePayment(
        long saleId,
        PaymentType paymentType,
        SalePaymentKind paymentKind,
        decimal amount)
    {
        SaleId = saleId;
        PaymentType = paymentType;
        PaymentKind = paymentKind;
        Amount = amount;
    }

    public long SaleId { get; private set; }

    public PaymentType PaymentType { get; private set; }

    public SalePaymentKind PaymentKind { get; private set; }

    public decimal Amount { get; private set; }

    internal static SalePayment Create(
        long saleId,
        PaymentType paymentType,
        SalePaymentKind paymentKind,
        decimal amount)
    {
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

        return new SalePayment(saleId, paymentType, paymentKind, roundedAmount);
    }

    public static decimal RoundAmount(decimal amount)
        => decimal.Round(amount, AmountScale, MidpointRounding.AwayFromZero);
}
