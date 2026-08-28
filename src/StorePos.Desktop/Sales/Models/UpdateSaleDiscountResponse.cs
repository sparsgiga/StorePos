namespace StorePos.Desktop.Sales.Models;

public sealed record UpdateSaleDiscountResponse(
    long SaleId,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TotalAmount);
