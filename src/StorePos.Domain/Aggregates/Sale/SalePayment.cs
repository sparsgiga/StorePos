using StorePos.Domain.Base;
using StorePos.Domain.Enums;

namespace StorePos.Domain.Aggregates.Sale;

public sealed class SalePayment : AuditableEntity<long>
{
    internal const int AmountScale = 5;

    private SalePayment()
    {
    }

    private SalePayment(long saleId, PaymentType paymentType, decimal amount)
    {
        SaleId = saleId;
        PaymentType = paymentType;
        Amount = amount;
    }

    public long SaleId { get; private set; }

    public PaymentType PaymentType { get; private set; }

    public decimal Amount { get; private set; }

    internal static SalePayment Create(long saleId, PaymentType paymentType, decimal amount)
    {
        if (!Enum.IsDefined(paymentType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(paymentType),
                "Payment type is not supported.");
        }

        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Payment amount cannot be negative.");
        }

        return new SalePayment(saleId, paymentType, RoundAmount(amount));
    }

    internal static decimal RoundAmount(decimal amount)
        => decimal.Round(amount, AmountScale, MidpointRounding.AwayFromZero);
}
