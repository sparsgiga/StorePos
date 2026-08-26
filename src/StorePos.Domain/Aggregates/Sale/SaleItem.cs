using StorePos.Domain.Base;
using StorePos.Domain.Common;

namespace StorePos.Domain.Aggregates.Sale;

public sealed class SaleItem : AuditableEntity<long>
{
    public const decimal MinimumUnitPrice = 0.00001m;
    public const int ProductCodeMaxLength = 50;
    public const int BarcodeMaxLength = 100;
    public const int ProductNameMaxLength = 300;
    public const int MeasurementUnitNameMaxLength = 100;
    public const int CommentMaxLength = 500;

    private SaleItem()
    {
    }

    private SaleItem(
        long saleId,
        long? productId,
        string? productCode,
        string? barcode,
        string productName,
        int? measurementUnitId,
        string? measurementUnitName,
        decimal quantity,
        decimal unitPrice,
        bool isManual,
        string? note)
    {
        SaleId = saleId;
        ProductId = productId;
        ProductCode = productCode;
        Barcode = barcode;
        MeasurementUnitId = measurementUnitId;
        MeasurementUnitName = measurementUnitName;
        IsManual = isManual;
        UpdateDetails(productName, quantity, unitPrice, note);
    }

    public long SaleId { get; private set; }

    public long? ProductId { get; private set; }

    public string? ProductCode { get; private set; }

    public string? Barcode { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public int? MeasurementUnitId { get; private set; }

    public string? MeasurementUnitName { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal { get; private set; }

    public bool IsManual { get; private set; }

    public string? Comment { get; private set; }

    internal static SaleItem CreateManual(
        long saleId,
        string productName,
        decimal quantity,
        decimal unitPrice,
        string? comment = null)
    {
        return new SaleItem(
            saleId,
            productId: null,
            productCode: null,
            barcode: null,
            productName,
            measurementUnitId: null,
            measurementUnitName: null,
            quantity,
            unitPrice,
            isManual: true,
            comment);
    }

    internal static SaleItem CreateCatalog(
        long saleId,
        long productId,
        string productCode,
        string? barcode,
        string productName,
        int measurementUnitId,
        string measurementUnitName,
        decimal quantity,
        decimal unitPrice,
        string? comment = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(measurementUnitId);

        var normalizedProductCode = NormalizeRequiredText(
            productCode,
            ProductCodeMaxLength,
            nameof(productCode));
        var normalizedBarcode = NormalizeOptionalText(
            barcode,
            BarcodeMaxLength,
            nameof(barcode));
        var normalizedMeasurementUnitName = NormalizeRequiredText(
            measurementUnitName,
            MeasurementUnitNameMaxLength,
            nameof(measurementUnitName));

        return new SaleItem(
            saleId,
            productId,
            normalizedProductCode,
            normalizedBarcode,
            productName,
            measurementUnitId,
            normalizedMeasurementUnitName,
            quantity,
            unitPrice,
            isManual: false,
            comment);
    }

    internal void IncreaseQuantity(decimal quantity)
    {
        var normalizedQuantity = FinancialPrecision.RoundQuantity(quantity);
        if (normalizedQuantity <= 0 ||
            normalizedQuantity > FinancialPrecision.MaximumFiveScaleValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        decimal combinedQuantity;
        try
        {
            combinedQuantity = checked(Quantity + normalizedQuantity);
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity is too large.");
        }

        combinedQuantity = FinancialPrecision.RoundQuantity(combinedQuantity);
        if (combinedQuantity > FinancialPrecision.MaximumFiveScaleValue)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity is too large.");
        }

        Quantity = combinedQuantity;
        LineTotal = FinancialPrecision.CalculateLineTotal(Quantity, UnitPrice);
    }

    internal void UpdateDetails(
        string productName,
        decimal quantity,
        decimal unitPrice,
        string? comment)
    {
        var normalizedProductName = productName?.Trim();
        var normalizedComment = string.IsNullOrWhiteSpace(comment)
            ? null
            : comment.Trim();

        if (string.IsNullOrWhiteSpace(normalizedProductName))
        {
            throw new ArgumentException("Product name is required.", nameof(productName));
        }

        if (normalizedProductName.Length > ProductNameMaxLength)
        {
            throw new ArgumentException(
                $"Product name cannot exceed {ProductNameMaxLength} characters.",
                nameof(productName));
        }

        var normalizedQuantity = FinancialPrecision.RoundQuantity(quantity);
        if (normalizedQuantity <= 0 ||
            normalizedQuantity > FinancialPrecision.MaximumFiveScaleValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        var normalizedUnitPrice = FinancialPrecision.RoundUnitPrice(unitPrice);
        if (normalizedUnitPrice < MinimumUnitPrice ||
            normalizedUnitPrice > FinancialPrecision.MaximumFiveScaleValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                $"Unit price must be at least {MinimumUnitPrice}.");
        }

        if (normalizedComment?.Length > CommentMaxLength)
        {
            throw new ArgumentException(
                $"Comment cannot exceed {CommentMaxLength} characters.",
                nameof(comment));
        }

        if (IsManual || string.IsNullOrEmpty(ProductName))
        {
            ProductName = normalizedProductName;
        }

        Quantity = normalizedQuantity;
        UnitPrice = normalizedUnitPrice;
        LineTotal = FinancialPrecision.CalculateLineTotal(Quantity, UnitPrice);
        Comment = normalizedComment;
    }

    private static string NormalizeRequiredText(
        string value,
        int maxLength,
        string parameterName)
    {
        var normalizedValue = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        if (normalizedValue.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalizedValue;
    }

    private static string? NormalizeOptionalText(
        string? value,
        int maxLength,
        string parameterName)
    {
        var normalizedValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalizedValue?.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalizedValue;
    }
}
