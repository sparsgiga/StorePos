using StorePos.Domain.Base;

namespace StorePos.Domain.Aggregates.Sale;

public sealed class SaleItem : AuditableEntity<long>
{
    public const int ProductNameMaxLength = 300;
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
        ProductName = productName;
        MeasurementUnitId = measurementUnitId;
        MeasurementUnitName = measurementUnitName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = quantity * unitPrice;
        IsManual = isManual;
        Comment = note;
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
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product name is required.", nameof(productName));
        }

        if (productName.Length > ProductNameMaxLength)
        {
            throw new ArgumentException(
                $"Product name cannot exceed {ProductNameMaxLength} characters.",
                nameof(productName));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                "Unit price cannot be negative.");
        }

        if (comment?.Length > CommentMaxLength)
        {
            throw new ArgumentException(
                $"Comment cannot exceed {CommentMaxLength} characters.",
                nameof(comment));
        }

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
}
