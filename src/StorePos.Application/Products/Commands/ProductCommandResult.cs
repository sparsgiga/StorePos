namespace StorePos.Application.Products.Commands;

public sealed record ProductCommandResult(
    long Id,
    string Code,
    string? Barcode,
    string Name,
    int MeasurementUnitId,
    decimal Price,
    bool IsActive,
    string? SupplierName = null,
    string? SupplierCode = null,
    decimal? CostPrice = null);
