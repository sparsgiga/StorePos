namespace StorePos.Desktop.Sales.Models;

public sealed record CreateProductAndAddSaleItemRequest(
    string Name,
    string? Barcode,
    int MeasurementUnitId,
    decimal Quantity,
    decimal UnitPrice,
    string? Comment);
