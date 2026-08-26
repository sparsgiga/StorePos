namespace StorePos.Desktop.Products.Models;

public sealed record ProductListItemDto(
    long Id,
    string Code,
    string? Barcode,
    string Name,
    int MeasurementUnitId,
    string MeasurementUnitName,
    string? MeasurementUnitShortName,
    decimal Price,
    bool IsActive)
{
    public string UnitDisplayName => string.IsNullOrWhiteSpace(MeasurementUnitShortName)
        ? MeasurementUnitName
        : $"{MeasurementUnitName} ({MeasurementUnitShortName})";

    public string StatusName => IsActive ? "აქტიური" : "არააქტიური";
}

public sealed record ProductDetailsDto(
    long Id,
    string Code,
    string? Barcode,
    string Name,
    int MeasurementUnitId,
    string MeasurementUnitName,
    string? MeasurementUnitShortName,
    decimal Price,
    bool IsActive);

public sealed record ProductMutationDto(
    long Id,
    string Code,
    string? Barcode,
    string Name,
    int MeasurementUnitId,
    decimal Price,
    bool IsActive);

public sealed record SaveProductRequest(
    string Code,
    string Barcode,
    string Name,
    int MeasurementUnitId,
    decimal Price);

public sealed record ProductListFilter(
    string? Search,
    int Status,
    int? MeasurementUnitId,
    decimal? PriceFrom,
    decimal? PriceTo,
    int PageNumber,
    int PageSize);

public sealed record ProductStatusOption(string Name, int Value);

public sealed record MeasurementUnitFilterOption(int? Id, string Name);
