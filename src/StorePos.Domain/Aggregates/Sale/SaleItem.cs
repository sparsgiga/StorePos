using StorePos.Domain.Base;

namespace StorePos.Domain.Aggregates.Sale;

public sealed class SaleItem : AuditableEntity<long>
{
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

    internal static SaleItem Create(
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
        string? note = null)
        => new(
            saleId,
            productId,
            productCode,
            barcode,
            productName,
            measurementUnitId,
            measurementUnitName,
            quantity,
            unitPrice,
            isManual,
            note);
}
