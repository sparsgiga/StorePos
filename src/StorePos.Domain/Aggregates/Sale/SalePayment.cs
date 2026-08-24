using StorePos.Domain.Base;
using StorePos.Domain.Enums;

namespace StorePos.Domain.Aggregates.Sale;

public sealed class SalePayment : AuditableEntity<long>
{
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
        => new(saleId, paymentType, amount);
}
