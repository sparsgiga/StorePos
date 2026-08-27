namespace StorePos.Api.Contracts.Products;

public sealed class SaveProductRequest
{
    public string Code { get; init; } = string.Empty;

    public string? Barcode { get; init; }

    public string Name { get; init; } = string.Empty;

    public int MeasurementUnitId { get; init; }

    public decimal Price { get; init; }

    public string? SupplierName { get; init; }

    public string? SupplierCode { get; init; }

    public decimal? CostPrice { get; init; }
}
