namespace StorePos.Api.Contracts.Sales;

public sealed record CreateProductAndAddSaleItemRequest(
    string ProductCode,
    string Name,
    string Barcode,
    int MeasurementUnitId,
    decimal Quantity,
    decimal UnitPrice,
    string? Comment);
