namespace StorePos.Application.Products.Queries.Search;

public sealed record ProductSearchResult(
    long Id,
    string Code,
    string? Barcode,
    string Name,
    int MeasurementUnitId,
    string MeasurementUnitName,
    string? MeasurementUnitShortName,
    decimal Price);
