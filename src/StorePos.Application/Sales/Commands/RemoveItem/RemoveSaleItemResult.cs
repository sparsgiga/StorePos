namespace StorePos.Application.Sales.Commands.RemoveItem;

public sealed record RemoveSaleItemResult(
    long SaleId,
    long SaleItemId,
    decimal SaleTotalAmount);
