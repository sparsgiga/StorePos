using System.Collections.ObjectModel;
using StorePos.Desktop.Common;
using StorePos.Desktop.Products.Models;
using StorePos.Desktop.Products.Barcodes;
using StorePos.Desktop.Sales.Calculations;
using System.Windows.Input;

namespace StorePos.Desktop.Sales.ViewModels;

public sealed class SaleItemInputViewModel : ObservableObject
{
    private const decimal MinimumUnitPrice = 0.00001m;
    private readonly Ean13BarcodeGenerator _barcodeGenerator = new();
    private string? _productName;
    private string _quantity = string.Empty;
    private string _unitPrice = string.Empty;
    private string _lineTotal = string.Empty;
    private string? _comment;
    private ManualItemField? _calculatedField;
    private bool _isUpdatingCalculator;
    private bool _isQuantityReadOnly;
    private bool _isUnitPriceReadOnly;
    private bool _isLineTotalReadOnly;
    private bool _isComplete;
    private bool _canSubmit;
    private bool _saveToCatalog;
    private string? _barcode;
    private string _productCode = string.Empty;
    private string? _catalogMessage;
    private bool _isLoadingCatalogDefaults;
    private MeasurementUnitDto? _selectedMeasurementUnit;
    private bool _isProductNameReadOnly;

    public SaleItemInputViewModel()
    {
        GenerateBarcodeCommand = new RelayCommand(GenerateBarcode, CanGenerateBarcode);
    }

    public ObservableCollection<MeasurementUnitDto> MeasurementUnits { get; } = [];

    public string? ProductName
    {
        get => _productName;
        set
        {
            if (SetProperty(ref _productName, value))
            {
                UpdateIsComplete();
            }
        }
    }

    public string Quantity
    {
        get => _quantity;
        set => SetNumericInput(
            ref _quantity,
            value,
            ManualItemField.Quantity,
            nameof(Quantity));
    }

    public string UnitPrice
    {
        get => _unitPrice;
        set => SetNumericInput(
            ref _unitPrice,
            value,
            ManualItemField.UnitPrice,
            nameof(UnitPrice));
    }

    public string LineTotal
    {
        get => _lineTotal;
        set => SetNumericInput(
            ref _lineTotal,
            value,
            ManualItemField.LineTotal,
            nameof(LineTotal));
    }

    public string? Comment
    {
        get => _comment;
        set => SetProperty(ref _comment, value);
    }

    public bool SaveToCatalog
    {
        get => _saveToCatalog;
        set
        {
            if (SetProperty(ref _saveToCatalog, value))
            {
                OnPropertyChanged(nameof(IsCatalogDetailsVisible));
                UpdateIsComplete();
            }
        }
    }

    public bool IsCatalogDetailsVisible => SaveToCatalog;

    public string ProductCode
    {
        get => _productCode;
        set
        {
            if (SetProperty(ref _productCode, value ?? string.Empty))
            {
                ((RelayCommand)GenerateBarcodeCommand).NotifyCanExecuteChanged();
                UpdateIsComplete();
            }
        }
    }

    public string? Barcode
    {
        get => _barcode;
        set
        {
            if (SetProperty(ref _barcode, value))
            {
                UpdateIsComplete();
            }
        }
    }

    public string? CatalogMessage
    {
        get => _catalogMessage;
        private set => SetProperty(ref _catalogMessage, value);
    }

    public bool IsLoadingCatalogDefaults
    {
        get => _isLoadingCatalogDefaults;
        private set
        {
            if (SetProperty(ref _isLoadingCatalogDefaults, value))
            {
                UpdateIsComplete();
            }
        }
    }

    public ICommand GenerateBarcodeCommand { get; }

    public MeasurementUnitDto? SelectedMeasurementUnit
    {
        get => _selectedMeasurementUnit;
        set
        {
            if (SetProperty(ref _selectedMeasurementUnit, value))
            {
                UpdateIsComplete();
            }
        }
    }

    public bool IsProductNameReadOnly
    {
        get => _isProductNameReadOnly;
        private set => SetProperty(ref _isProductNameReadOnly, value);
    }

    public bool IsQuantityReadOnly
    {
        get => _isQuantityReadOnly;
        private set => SetProperty(ref _isQuantityReadOnly, value);
    }

    public bool IsUnitPriceReadOnly
    {
        get => _isUnitPriceReadOnly;
        private set => SetProperty(ref _isUnitPriceReadOnly, value);
    }

    public bool IsLineTotalReadOnly
    {
        get => _isLineTotalReadOnly;
        private set => SetProperty(ref _isLineTotalReadOnly, value);
    }

    public bool IsComplete
    {
        get => _isComplete;
        private set => SetProperty(ref _isComplete, value);
    }

    public bool CanSubmit
    {
        get => _canSubmit;
        private set => SetProperty(ref _canSubmit, value);
    }

    public void Load(
        string productName,
        decimal quantity,
        decimal unitPrice,
        string? comment,
        bool isProductNameReadOnly = false)
    {
        Reset();

        _isUpdatingCalculator = true;
        try
        {
            ProductName = productName;
            Quantity = DecimalInputParser.Format(quantity);
            UnitPrice = DecimalInputParser.Format(unitPrice);
            Comment = comment;
            IsProductNameReadOnly = isProductNameReadOnly;
        }
        finally
        {
            _isUpdatingCalculator = false;
        }

        Recalculate(ManualItemField.UnitPrice);
        UpdateIsComplete();
    }

    public void Reset()
    {
        _isUpdatingCalculator = true;
        try
        {
            SetCalculatedField(null);
            ProductName = null;
            Quantity = string.Empty;
            UnitPrice = string.Empty;
            LineTotal = string.Empty;
            Comment = null;
            SaveToCatalog = false;
            ProductCode = string.Empty;
            Barcode = null;
            CatalogMessage = null;
            IsLoadingCatalogDefaults = false;
            SelectedMeasurementUnit = MeasurementUnits.Count == 1
                ? MeasurementUnits[0]
                : null;
            IsProductNameReadOnly = false;
        }
        finally
        {
            _isUpdatingCalculator = false;
        }

        UpdateIsComplete();
    }

    public void LoadMeasurementUnits(IEnumerable<MeasurementUnitDto> units)
    {
        MeasurementUnits.Clear();
        foreach (var unit in units)
        {
            MeasurementUnits.Add(unit);
        }

        SelectedMeasurementUnit = MeasurementUnits.Count == 1
            ? MeasurementUnits[0]
            : null;
    }

    public void SetCatalogDefaultsLoading(bool isLoading)
    {
        IsLoadingCatalogDefaults = isLoading;
        if (isLoading)
        {
            CatalogMessage = null;
        }
    }

    public void ApplyCreationDefaults(ProductCreationDefaultsDto defaults)
    {
        if (string.IsNullOrWhiteSpace(ProductCode))
        {
            ProductCode = defaults.SuggestedCode;
        }

        if (defaults.DefaultMeasurementUnitId.HasValue)
        {
            SelectedMeasurementUnit = MeasurementUnits.FirstOrDefault(unit =>
                unit.Id == defaults.DefaultMeasurementUnitId.Value);
        }

        CatalogMessage = defaults.ConfigurationMessage;
        if (defaults.DefaultMeasurementUnitId.HasValue && SelectedMeasurementUnit is null)
        {
            CatalogMessage = "ნაგულისხმევი საზომი ერთეული აქტიურ სიაში ვერ მოიძებნა.";
        }

        if (string.IsNullOrWhiteSpace(Barcode) && CanGenerateBarcode())
        {
            GenerateBarcode(clearMessageOnSuccess: false);
        }

        IsLoadingCatalogDefaults = false;
    }

    public void SetCatalogError(string message)
    {
        CatalogMessage = message;
        IsLoadingCatalogDefaults = false;
    }

    public void PrepareManualFallback(string value, bool isBarcode)
    {
        if (isBarcode)
        {
            Barcode = value;
        }
        else
        {
            ProductName = value;
        }
    }

    public bool TryGetValues(
        out string productName,
        out decimal quantity,
        out decimal unitPrice)
    {
        productName = ProductName?.Trim() ?? string.Empty;
        quantity = default;
        unitPrice = default;

        return IsComplete &&
               DecimalInputParser.TryParse(Quantity, out quantity) &&
               DecimalInputParser.TryParse(UnitPrice, out unitPrice);
    }

    private void SetNumericInput(
        ref string field,
        string? value,
        ManualItemField changedField,
        string propertyName)
    {
        if (!SetProperty(ref field, value ?? string.Empty, propertyName))
        {
            return;
        }

        if (!_isUpdatingCalculator)
        {
            Recalculate(changedField);
        }

        UpdateIsComplete();
    }

    private void Recalculate(ManualItemField changedField)
    {
        if (_calculatedField.HasValue && changedField != _calculatedField.Value)
        {
            if (TryCalculateExcluding(_calculatedField.Value, out var updatedCalculation))
            {
                ApplyCalculation(updatedCalculation!);
            }
            else
            {
                ClearCalculatedValue();
            }

            return;
        }

        if (_calculatedField.HasValue ||
            !TryReadOptionalDecimal(Quantity, out var quantity) ||
            !TryReadOptionalDecimal(UnitPrice, out var unitPrice) ||
            !TryReadOptionalDecimal(LineTotal, out var lineTotal))
        {
            return;
        }

        if (ManualItemCalculator.TryCalculate(
                quantity,
                unitPrice,
                lineTotal,
                out var calculation))
        {
            ApplyCalculation(calculation!);
        }
    }

    private bool TryCalculateExcluding(
        ManualItemField excludedField,
        out ManualItemCalculation? calculation)
    {
        calculation = null;

        if (!TryReadOptionalDecimal(
                excludedField == ManualItemField.Quantity ? null : Quantity,
                out var quantity) ||
            !TryReadOptionalDecimal(
                excludedField == ManualItemField.UnitPrice ? null : UnitPrice,
                out var unitPrice) ||
            !TryReadOptionalDecimal(
                excludedField == ManualItemField.LineTotal ? null : LineTotal,
                out var lineTotal))
        {
            return false;
        }

        return ManualItemCalculator.TryCalculate(
            quantity,
            unitPrice,
            lineTotal,
            out calculation);
    }

    private void ApplyCalculation(ManualItemCalculation calculation)
    {
        _isUpdatingCalculator = true;
        try
        {
            var formattedValue = DecimalInputParser.Format(calculation.Value);

            switch (calculation.CalculatedField)
            {
                case ManualItemField.Quantity:
                    Quantity = formattedValue;
                    LineTotal = DecimalInputParser.Format(calculation.LineTotal);
                    break;
                case ManualItemField.UnitPrice:
                    UnitPrice = formattedValue;
                    LineTotal = DecimalInputParser.Format(calculation.LineTotal);
                    break;
                case ManualItemField.LineTotal:
                    LineTotal = formattedValue;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            SetCalculatedField(calculation.CalculatedField);
        }
        finally
        {
            _isUpdatingCalculator = false;
        }

        UpdateIsComplete();
    }

    private void ClearCalculatedValue()
    {
        if (!_calculatedField.HasValue)
        {
            return;
        }

        var calculatedField = _calculatedField.Value;
        _isUpdatingCalculator = true;
        try
        {
            SetCalculatedField(null);

            switch (calculatedField)
            {
                case ManualItemField.Quantity:
                    Quantity = string.Empty;
                    break;
                case ManualItemField.UnitPrice:
                    UnitPrice = string.Empty;
                    break;
                case ManualItemField.LineTotal:
                    LineTotal = string.Empty;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        finally
        {
            _isUpdatingCalculator = false;
        }

        UpdateIsComplete();
    }

    private void SetCalculatedField(ManualItemField? field)
    {
        _calculatedField = field;
        IsQuantityReadOnly = field == ManualItemField.Quantity;
        IsUnitPriceReadOnly = field == ManualItemField.UnitPrice;
        IsLineTotalReadOnly = field == ManualItemField.LineTotal;
    }

    private void UpdateIsComplete()
    {
        IsComplete = !string.IsNullOrWhiteSpace(ProductName) &&
                     _calculatedField.HasValue &&
                     DecimalInputParser.TryParse(Quantity, out var quantity) &&
                     quantity > 0 &&
                     DecimalInputParser.TryParse(UnitPrice, out var unitPrice) &&
                     unitPrice >= MinimumUnitPrice &&
                     DecimalInputParser.TryParse(LineTotal, out var lineTotal) &&
                     lineTotal > 0;
        var normalizedCode = ProductCode.Trim();
        var hasValidProductCode = normalizedCode.Length is > 0 and <= 50;
        var hasValidBarcode = !string.IsNullOrWhiteSpace(Barcode) &&
                              Barcode.Trim().Length <= 100;
        CanSubmit = IsComplete &&
                    (!SaveToCatalog ||
                     !IsLoadingCatalogDefaults &&
                     SelectedMeasurementUnit is not null &&
                     hasValidProductCode &&
                     hasValidBarcode);
    }

    private void GenerateBarcode()
        => GenerateBarcode(clearMessageOnSuccess: true);

    private bool CanGenerateBarcode()
    {
        var code = ProductCode.Trim();
        return code.Length is > 0 and <= Ean13BarcodeGenerator.BodyLength &&
               code.All(character => character is >= '0' and <= '9');
    }

    private void GenerateBarcode(bool clearMessageOnSuccess)
    {
        try
        {
            Barcode = _barcodeGenerator.Generate(ProductCode);
            if (clearMessageOnSuccess)
            {
                CatalogMessage = null;
            }
        }
        catch (ArgumentException exception)
        {
            CatalogMessage = exception.Message;
        }
    }

    private static bool TryReadOptionalDecimal(
        string? input,
        out decimal? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        if (!DecimalInputParser.TryParse(input, out var parsedValue))
        {
            return false;
        }

        value = parsedValue;
        return true;
    }
}
