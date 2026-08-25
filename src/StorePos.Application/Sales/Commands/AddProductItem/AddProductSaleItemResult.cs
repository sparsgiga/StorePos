namespace StorePos.Application.Sales.Commands.AddProductItem;

public sealed record AddProductSaleItemResult(
    long SaleId,
    long SaleItemId,
    long ProductId,
    string ProductCode,
    string? Barcode,
    string ProductName,
    int MeasurementUnitId,
    string MeasurementUnitName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    bool IsManual,
    decimal SaleTotalAmount,
    bool WasNewItem,
    string? Comment);
