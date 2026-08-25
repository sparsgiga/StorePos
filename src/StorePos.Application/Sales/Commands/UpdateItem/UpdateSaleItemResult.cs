namespace StorePos.Application.Sales.Commands.UpdateItem;

public sealed record UpdateSaleItemResult(
    long SaleId,
    long SaleItemId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    decimal SaleTotalAmount,
    string? Comment);
