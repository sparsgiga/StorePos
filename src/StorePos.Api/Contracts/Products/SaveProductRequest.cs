namespace StorePos.Api.Contracts.Products;

public sealed class SaveProductRequest
{
    public string Code { get; init; } = string.Empty;

    public string Barcode { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public int MeasurementUnitId { get; init; }

    public decimal Price { get; init; }
}
