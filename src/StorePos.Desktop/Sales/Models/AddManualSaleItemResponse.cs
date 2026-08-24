namespace StorePos.Desktop.Sales.Models;

public sealed record AddManualSaleItemResponse(
    long SaleId,
    long SaleItemId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    decimal SaleTotalAmount,
    string? Comment);
