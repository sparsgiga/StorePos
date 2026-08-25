namespace StorePos.Application.Sales.Queries.GetSoldProducts;

public sealed record SoldProductModel(
    long SaleId,
    long SaleItemId,
    string SaleNumber,
    DateTime DateCompleted,
    string? CustomerName,
    long? ProductId,
    string? ProductCode,
    string? Barcode,
    string ProductName,
    string? UnitName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    bool IsManual,
    string? Comment);
