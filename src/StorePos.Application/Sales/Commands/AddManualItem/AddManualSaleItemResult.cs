namespace StorePos.Application.Sales.Commands.AddManualItem;

public sealed record AddManualSaleItemResult(
    long SaleId,
    long SaleItemId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    decimal SaleTotalAmount,
    string? Comment);
