namespace StorePos.Application.Products.Queries.GetList;

public sealed record ProductListItem(
    long Id,
    string Code,
    string? Barcode,
    string Name,
    int MeasurementUnitId,
    string MeasurementUnitName,
    string? MeasurementUnitShortName,
    decimal Price,
    bool IsActive);
