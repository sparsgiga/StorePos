using StorePos.Domain.Base;
using StorePos.Domain.Common;

namespace StorePos.Domain.Aggregates.Product;

public sealed class Product : AuditableEntity<long>, IAggregateRoot
{
    public const decimal MinimumPrice = 0.00001m;
    public const int CodeMaxLength = 50;
    public const int BarcodeMaxLength = 100;
    public const int NameMaxLength = 300;

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
        ApplyDetails(code, barcode, name, measurementUnitId, price, requireBarcode: false);
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

    public void UpdateDetails(
        string code,
        string barcode,
        string name,
        int measurementUnitId,
        decimal price)
        => ApplyDetails(code, barcode, name, measurementUnitId, price, requireBarcode: true);

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private void ApplyDetails(
        string code,
        string? barcode,
        string name,
        int measurementUnitId,
        decimal price,
        bool requireBarcode)
    {
        var normalizedCode = code?.Trim();
        var normalizedBarcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        var normalizedName = name?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            throw new ArgumentException("Product code is required.", nameof(code));
        }

        if (normalizedCode.Length > CodeMaxLength)
        {
            throw new ArgumentException(
                $"Product code cannot exceed {CodeMaxLength} characters.",
                nameof(code));
        }

        if (normalizedCode.Any(character => character is < '0' or > '9'))
        {
            throw new ArgumentException(
                "Product code must contain ASCII digits only.",
                nameof(code));
        }

        if (normalizedBarcode?.Length > BarcodeMaxLength)
        {
            throw new ArgumentException(
                $"Barcode cannot exceed {BarcodeMaxLength} characters.",
                nameof(barcode));
        }

        if (requireBarcode && normalizedBarcode is null)
        {
            throw new ArgumentException("Product barcode is required.", nameof(barcode));
        }

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (normalizedName.Length > NameMaxLength)
        {
            throw new ArgumentException(
                $"Product name cannot exceed {NameMaxLength} characters.",
                nameof(name));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(measurementUnitId);

        var normalizedPrice = FinancialPrecision.RoundUnitPrice(price);
        if (normalizedPrice < MinimumPrice ||
            normalizedPrice > FinancialPrecision.MaximumFiveScaleValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price),
                $"Price must be at least {MinimumPrice}.");
        }

        Code = normalizedCode;
        Barcode = normalizedBarcode;
        Name = normalizedName;
        MeasurementUnitId = measurementUnitId;
        Price = normalizedPrice;
    }
}
