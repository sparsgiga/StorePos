using StorePos.Desktop.Common;
using StorePos.Desktop.Sales.Calculations;

namespace StorePos.Desktop.Sales.ViewModels;

public sealed class SaleItemInputViewModel : ObservableObject
{
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

    public void Load(
        string productName,
        decimal quantity,
        decimal unitPrice,
        string? comment)
    {
        Reset();

        _isUpdatingCalculator = true;
        try
        {
            ProductName = productName;
            Quantity = DecimalInputParser.Format(quantity);
            UnitPrice = DecimalInputParser.Format(unitPrice);
            Comment = comment;
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
        }
        finally
        {
            _isUpdatingCalculator = false;
        }

        UpdateIsComplete();
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
                    break;
                case ManualItemField.UnitPrice:
                    UnitPrice = formattedValue;
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
                     unitPrice >= 0 &&
                     DecimalInputParser.TryParse(LineTotal, out var lineTotal) &&
                     lineTotal >= 0;
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
