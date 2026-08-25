namespace StorePos.Desktop.Sales.Models;

public sealed record AddProductSaleItemResponse(
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
    decimal SaleTotalAmount,
    bool WasNewItem,
    string? Comment);
