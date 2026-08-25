using StorePos.Desktop.Common;

namespace StorePos.Desktop.Sales.ViewModels;

public sealed class SaleItemViewModel : ObservableObject
{
    private string _productName;
    private decimal _quantity;
    private decimal _unitPrice;
    private decimal _lineTotal;
    private string? _comment;

    public SaleItemViewModel(
        long id,
        long? productId,
        string? productCode,
        string? barcode,
        string productName,
        int? measurementUnitId,
        string? measurementUnitName,
        decimal quantity,
        decimal unitPrice,
        decimal lineTotal,
        bool isManual,
        string? comment)
    {
        Id = id;
        ProductId = productId;
        ProductCode = productCode;
        Barcode = barcode;
        _productName = productName;
        MeasurementUnitId = measurementUnitId;
        MeasurementUnitName = measurementUnitName;
        _quantity = quantity;
        _unitPrice = unitPrice;
        _lineTotal = lineTotal;
        IsManual = isManual;
        _comment = comment;
    }

    public long Id { get; }

    public long? ProductId { get; }

    public string? ProductCode { get; }

    public string? Barcode { get; }

    public int? MeasurementUnitId { get; }

    public string? MeasurementUnitName { get; }

    public string ProductName
    {
        get => _productName;
        private set => SetProperty(ref _productName, value);
    }

    public decimal Quantity
    {
        get => _quantity;
        private set => SetProperty(ref _quantity, value);
    }

    public decimal UnitPrice
    {
        get => _unitPrice;
        private set => SetProperty(ref _unitPrice, value);
    }

    public decimal LineTotal
    {
        get => _lineTotal;
        private set => SetProperty(ref _lineTotal, value);
    }

    public bool IsManual { get; }

    public string? Comment
    {
        get => _comment;
        private set => SetProperty(ref _comment, value);
    }

    public void ApplyUpdate(
        string productName,
        decimal quantity,
        decimal unitPrice,
        decimal lineTotal,
        string? comment)
    {
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = lineTotal;
        Comment = comment;
    }
}
