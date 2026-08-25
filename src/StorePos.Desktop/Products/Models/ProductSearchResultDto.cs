namespace StorePos.Desktop.Products.Models;

public sealed record ProductSearchResultDto(
    long Id,
    string Code,
    string? Barcode,
    string Name,
    int MeasurementUnitId,
    string MeasurementUnitName,
    string? MeasurementUnitShortName,
    decimal Price);
