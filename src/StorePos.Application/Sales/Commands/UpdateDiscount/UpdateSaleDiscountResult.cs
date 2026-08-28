namespace StorePos.Application.Sales.Commands.UpdateDiscount;

public sealed record UpdateSaleDiscountResult(
    long SaleId,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TotalAmount);
