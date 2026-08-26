using StorePos.Desktop.Common;
using StorePos.Desktop.Reporting.Models;
using StorePos.Desktop.Sales.Calculations;

namespace StorePos.Desktop.Reporting.ViewModels;

public sealed class LoadingListItemSelectionViewModel : ObservableObject
{
    private bool _isSelected = true;
    private string _loadingQuantityText;
    private decimal _loadingQuantity;
    private string? _validationMessage;

    public LoadingListItemSelectionViewModel(FullSaleReportItemModel item)
    {
        SaleItemId = item.SaleItemId;
        ProductCode = item.ProductCode;
        Barcode = item.Barcode;
        ProductName = item.ProductName;
        MeasurementUnitName = item.MeasurementUnitName;
        SoldQuantity = item.Quantity;
        IsManual = item.IsManual;
        Comment = item.Comment;
        _loadingQuantity = item.Quantity;
        _loadingQuantityText = ReportFormatting.Quantity(item.Quantity);
    }

    public long SaleItemId { get; }
    public string? ProductCode { get; }
    public string? Barcode { get; }
    public string ProductName { get; }
    public string? MeasurementUnitName { get; }
    public decimal SoldQuantity { get; }
    public bool IsManual { get; }
    public string? Comment { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string LoadingQuantityText
    {
        get => _loadingQuantityText;
        set
        {
            if (SetProperty(ref _loadingQuantityText, value))
            {
                ValidateQuantity();
            }
        }
    }

    public decimal LoadingQuantity => _loadingQuantity;
    public bool IsValid => ValidationMessage is null;

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set
        {
            if (SetProperty(ref _validationMessage, value))
            {
                OnPropertyChanged(nameof(IsValid));
            }
        }
    }

    public LoadingListReportItemModel CreateReportItem()
    {
        if (!IsSelected || !IsValid)
        {
            throw new InvalidOperationException(
                "Only a selected item with a valid loading quantity can be printed.");
        }

        return new LoadingListReportItemModel(
            SaleItemId,
            ProductCode,
            Barcode,
            ProductName,
            MeasurementUnitName,
            LoadingQuantity,
            IsManual,
            Comment);
    }

    private void ValidateQuantity()
    {
        if (!DecimalInputParser.TryParse(LoadingQuantityText, out var parsed))
        {
            ValidationMessage = "შეიყვანეთ სწორი რაოდენობა.";
            return;
        }

        var normalized = FinancialInputPrecision.RoundQuantity(parsed);
        if (normalized <= 0)
        {
            ValidationMessage = "რაოდენობა უნდა იყოს ნულზე მეტი.";
            return;
        }

        if (normalized > SoldQuantity)
        {
            ValidationMessage = "დასატვირთი რაოდენობა გაყიდულს არ უნდა აღემატებოდეს.";
            return;
        }

        _loadingQuantity = normalized;
        OnPropertyChanged(nameof(LoadingQuantity));
        ValidationMessage = null;
    }
}
