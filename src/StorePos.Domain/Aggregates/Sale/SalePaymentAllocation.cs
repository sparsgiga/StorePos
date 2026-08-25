using StorePos.Domain.Enums;

namespace StorePos.Domain.Aggregates.Sale;

public readonly record struct SalePaymentAllocation(
    PaymentType PaymentType,
    decimal Amount);
