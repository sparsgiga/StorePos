using StorePos.Domain.Base;

namespace StorePos.Domain.Aggregates.Product;

public sealed class Product : AuditableEntity<long>, IAggregateRoot
{
    private Product()
    {
    }

    private Product(
        string code,
        string? barcode,
        string name,
        int measurementUnitId,
        decimal price)
    {
        Code = code;
        Barcode = barcode;
        Name = name;
        MeasurementUnitId = measurementUnitId;
        Price = price;
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;

    public string? Barcode { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int MeasurementUnitId { get; private set; }

    public decimal Price { get; private set; }

    public bool IsActive { get; private set; }

    public static Product Create(
        string code,
        string? barcode,
        string name,
        int measurementUnitId,
        decimal price)
        => new(code, barcode, name, measurementUnitId, price);
}
