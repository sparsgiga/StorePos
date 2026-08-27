using StorePos.Domain.Base;
using StorePos.Domain.Common;

namespace StorePos.Domain.Aggregates.Product;

public sealed class Product : AuditableEntity<long>, IAggregateRoot
{
    public const decimal MinimumPrice = 0m;
    public const int CodeMaxLength = 50;
    public const int BarcodeMaxLength = 100;
    public const int NameMaxLength = 300;
    public const int SupplierNameMaxLength = 300;
    public const int SupplierCodeMaxLength = 100;

    private Product()
    {
    }

    private Product(
        string code,
        string? barcode,
        string name,
        int measurementUnitId,
        decimal price,
        string? supplierName,
        string? supplierCode,
        decimal? costPrice)
    {
        ApplyDetails(
            code,
            barcode,
            name,
            measurementUnitId,
            price,
            supplierName,
            supplierCode,
            costPrice);
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;

    public string? Barcode { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int MeasurementUnitId { get; private set; }

    public decimal Price { get; private set; }

    public string? SupplierName { get; private set; }

    public string? SupplierCode { get; private set; }

    public decimal? CostPrice { get; private set; }

    public bool IsActive { get; private set; }

    public static Product Create(
        string code,
        string? barcode,
        string name,
        int measurementUnitId,
        decimal price,
        string? supplierName = null,
        string? supplierCode = null,
        decimal? costPrice = null)
        => new(
            code,
            barcode,
            name,
            measurementUnitId,
            price,
            supplierName,
            supplierCode,
            costPrice);

    public void UpdateDetails(
        string code,
        string? barcode,
        string name,
        int measurementUnitId,
        decimal price,
        string? supplierName = null,
        string? supplierCode = null,
        decimal? costPrice = null)
        => ApplyDetails(
            code,
            barcode,
            name,
            measurementUnitId,
            price,
            supplierName,
            supplierCode,
            costPrice);

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void UpdateRetailPrice(decimal price)
    {
        var normalizedPrice = FinancialPrecision.RoundUnitPrice(price);
        if (normalizedPrice <= 0 ||
            normalizedPrice > FinancialPrecision.MaximumFiveScaleValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price),
                "Retail price must be greater than zero and within the supported range.");
        }

        Price = normalizedPrice;
    }

    private void ApplyDetails(
        string code,
        string? barcode,
        string name,
        int measurementUnitId,
        decimal price,
        string? supplierName,
        string? supplierCode,
        decimal? costPrice)
    {
        var normalizedCode = code?.Trim();
        var normalizedBarcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        var normalizedName = name?.Trim();
        var normalizedSupplierName = string.IsNullOrWhiteSpace(supplierName)
            ? null
            : supplierName.Trim();
        var normalizedSupplierCode = string.IsNullOrWhiteSpace(supplierCode)
            ? null
            : supplierCode.Trim();

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

        if (normalizedBarcode?.Length > BarcodeMaxLength)
        {
            throw new ArgumentException(
                $"Barcode cannot exceed {BarcodeMaxLength} characters.",
                nameof(barcode));
        }

        if (normalizedSupplierName?.Length > SupplierNameMaxLength)
        {
            throw new ArgumentException(
                $"Supplier name cannot exceed {SupplierNameMaxLength} characters.",
                nameof(supplierName));
        }

        if (normalizedSupplierCode?.Length > SupplierCodeMaxLength)
        {
            throw new ArgumentException(
                $"Supplier code cannot exceed {SupplierCodeMaxLength} characters.",
                nameof(supplierCode));
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

        decimal? normalizedCostPrice = null;
        if (costPrice.HasValue)
        {
            normalizedCostPrice = FinancialPrecision.RoundUnitPrice(costPrice.Value);
            if (normalizedCostPrice < 0 ||
                normalizedCostPrice > FinancialPrecision.MaximumFiveScaleValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(costPrice),
                    "Cost price cannot be negative or exceed the supported range.");
            }
        }

        Code = normalizedCode;
        Barcode = normalizedBarcode;
        Name = normalizedName;
        MeasurementUnitId = measurementUnitId;
        Price = normalizedPrice;
        SupplierName = normalizedSupplierName;
        SupplierCode = normalizedSupplierCode;
        CostPrice = normalizedCostPrice;
    }
}
