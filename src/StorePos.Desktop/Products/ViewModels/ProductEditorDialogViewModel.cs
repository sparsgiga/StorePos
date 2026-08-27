using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using StorePos.Desktop.Api;
using StorePos.Desktop.Common;
using StorePos.Desktop.Products.Barcodes;
using StorePos.Desktop.Products.Models;
using StorePos.Desktop.Sales.Calculations;
using StorePos.Desktop.Sales.Dialogs;

namespace StorePos.Desktop.Products.ViewModels;

public sealed class ProductEditorDialogViewModel : ObservableObject
{
    private readonly IStorePosApiClient _apiClient;
    private readonly long? _productId;
    private readonly CancellationToken _cancellationToken;
    private readonly Ean13BarcodeGenerator _barcodeGenerator = new();
    private readonly AsyncRelayCommand _saveCommand;
    private string _code = string.Empty;
    private string _barcode = string.Empty;
    private string _name = string.Empty;
    private string _price = string.Empty;
    private string _supplierName = string.Empty;
    private string _supplierCode = string.Empty;
    private string _costPrice = string.Empty;
    private MeasurementUnitDto? _selectedMeasurementUnit;
    private string? _errorMessage;
    private bool _isBusy;

    public ProductEditorDialogViewModel(
        IStorePosApiClient apiClient,
        long? productId,
        CancellationToken cancellationToken)
    {
        _apiClient = apiClient;
        _productId = productId;
        _cancellationToken = cancellationToken;
        _saveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        GenerateBarcodeCommand = new RelayCommand(GenerateBarcode, CanGenerateBarcode);
        CancelCommand = new RelayCommand(() =>
            CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(false)));
    }

    public event EventHandler<DialogCloseRequestedEventArgs>? CloseRequested;

    public ObservableCollection<MeasurementUnitDto> MeasurementUnits { get; } = [];

    public string Title => _productId.HasValue ? "პროდუქტის რედაქტირება" : "ახალი პროდუქტი";

    public string Code
    {
        get => _code;
        set => SetInput(ref _code, value);
    }

    public string Barcode
    {
        get => _barcode;
        set => SetInput(ref _barcode, value);
    }

    public string Name
    {
        get => _name;
        set => SetInput(ref _name, value);
    }

    public string Price
    {
        get => _price;
        set => SetInput(ref _price, value);
    }

    public string SupplierName
    {
        get => _supplierName;
        set => SetInput(ref _supplierName, value);
    }

    public string SupplierCode
    {
        get => _supplierCode;
        set => SetInput(ref _supplierCode, value);
    }

    public string CostPrice
    {
        get => _costPrice;
        set => SetInput(ref _costPrice, value);
    }

    public MeasurementUnitDto? SelectedMeasurementUnit
    {
        get => _selectedMeasurementUnit;
        set
        {
            if (SetProperty(ref _selectedMeasurementUnit, value))
            {
                ErrorMessage = null;
                _saveCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                _saveCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public ICommand SaveCommand => _saveCommand;
    public ICommand GenerateBarcodeCommand { get; }
    public ICommand CancelCommand { get; }

    public async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            if (_productId.HasValue)
            {
                var unitsTask = _apiClient.GetMeasurementUnitsAsync(_cancellationToken);
                var productTask = _apiClient.GetProductAsync(_productId.Value, _cancellationToken);
                await Task.WhenAll(unitsTask, productTask);
                LoadUnits(unitsTask.Result);
                var product = productTask.Result;
                Code = product.Code;
                Barcode = product.Barcode ?? string.Empty;
                Name = product.Name;
                Price = DecimalInputParser.Format(product.Price);
                SupplierName = product.SupplierName ?? string.Empty;
                SupplierCode = product.SupplierCode ?? string.Empty;
                CostPrice = product.CostPrice.HasValue
                    ? DecimalInputParser.Format(product.CostPrice.Value)
                    : string.Empty;
                SelectedMeasurementUnit = MeasurementUnits.FirstOrDefault(unit =>
                    unit.Id == product.MeasurementUnitId);
            }
            else
            {
                var unitsTask = _apiClient.GetMeasurementUnitsAsync(_cancellationToken);
                var defaultsTask = _apiClient.GetProductCreationDefaultsAsync(_cancellationToken);
                await Task.WhenAll(unitsTask, defaultsTask);
                LoadUnits(unitsTask.Result);
                var defaults = defaultsTask.Result;
                Code = defaults.SuggestedCode;
                SelectedMeasurementUnit = defaults.DefaultMeasurementUnitId.HasValue
                    ? MeasurementUnits.FirstOrDefault(unit =>
                        unit.Id == defaults.DefaultMeasurementUnitId.Value)
                    : null;
                if (!string.IsNullOrWhiteSpace(defaults.ConfigurationMessage))
                {
                    ErrorMessage = defaults.ConfigurationMessage;
                }

                if (string.IsNullOrWhiteSpace(Barcode) && CanGenerateBarcode())
                {
                    var configurationMessage = ErrorMessage;
                    GenerateBarcode();
                    if (!string.IsNullOrWhiteSpace(configurationMessage))
                    {
                        ErrorMessage = configurationMessage;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "პროდუქტის ფორმის ჩატვირთვა ვერ მოხერხდა.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveAsync()
    {
        if (!TryGetPrice(out var price) ||
            !TryGetCostPrice(out var costPrice) ||
            SelectedMeasurementUnit is null)
        {
            ErrorMessage = "შეავსეთ ყველა სავალდებულო ველი სწორად.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var request = new SaveProductRequest(
                Code.Trim(),
                string.IsNullOrWhiteSpace(Barcode) ? null : Barcode.Trim(),
                Name.Trim(),
                SelectedMeasurementUnit.Id,
                FinancialInputPrecision.RoundUnitPrice(price),
                string.IsNullOrWhiteSpace(SupplierName) ? null : SupplierName.Trim(),
                string.IsNullOrWhiteSpace(SupplierCode) ? null : SupplierCode.Trim(),
                costPrice.HasValue
                    ? FinancialInputPrecision.RoundUnitPrice(costPrice.Value)
                    : null);
            if (_productId.HasValue)
            {
                await _apiClient.UpdateProductAsync(
                    _productId.Value,
                    request,
                    _cancellationToken);
            }
            else
            {
                await _apiClient.CreateProductAsync(request, _cancellationToken);
            }

            CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(true));
        }
        catch (ProductConflictException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "პროდუქტის შენახვა ვერ მოხერხდა.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave()
        => !IsBusy &&
           !string.IsNullOrWhiteSpace(Name) &&
           !string.IsNullOrWhiteSpace(Code) &&
           Code.Trim().Length <= 50 &&
           (_productId.HasValue || IsAsciiDigits(Code.Trim())) &&
           Barcode.Trim().Length <= 100 &&
           SupplierName.Trim().Length <= 300 &&
           SupplierCode.Trim().Length <= 100 &&
           SelectedMeasurementUnit is not null &&
           TryGetPrice(out _) &&
           TryGetCostPrice(out _);

    private static bool IsAsciiDigits(string value)
        => value.Length > 0 &&
           value.All(character => character is >= '0' and <= '9');

    private bool TryGetPrice(out decimal price)
        => DecimalInputParser.TryParse(Price, out price) &&
           FinancialInputPrecision.RoundUnitPrice(price) >= 0m &&
           FinancialInputPrecision.RoundUnitPrice(price) <=
               FinancialInputPrecision.MaximumFiveScaleValue;

    private bool TryGetCostPrice(out decimal? costPrice)
    {
        costPrice = null;
        if (string.IsNullOrWhiteSpace(CostPrice))
        {
            return true;
        }

        if (!DecimalInputParser.TryParse(CostPrice, out var parsed))
        {
            return false;
        }

        var normalized = FinancialInputPrecision.RoundUnitPrice(parsed);
        if (normalized < 0 || normalized > FinancialInputPrecision.MaximumFiveScaleValue)
        {
            return false;
        }

        costPrice = normalized;
        return true;
    }

    private bool CanGenerateBarcode()
    {
        var code = Code.Trim();
        return code.Length is > 0 and <= Ean13BarcodeGenerator.BodyLength &&
               code.All(character => character is >= '0' and <= '9');
    }

    private void GenerateBarcode()
    {
        try
        {
            Barcode = _barcodeGenerator.Generate(Code);
            ErrorMessage = null;
        }
        catch (ArgumentException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    private void LoadUnits(IEnumerable<MeasurementUnitDto> units)
    {
        MeasurementUnits.Clear();
        foreach (var unit in units)
        {
            MeasurementUnits.Add(unit);
        }
    }

    private void SetInput(
        ref string field,
        string? value,
        [CallerMemberName] string? propertyName = null)
    {
        if (SetProperty(ref field, value ?? string.Empty, propertyName))
        {
            ErrorMessage = null;
            _saveCommand.NotifyCanExecuteChanged();
            if (propertyName == nameof(Code))
            {
                ((RelayCommand)GenerateBarcodeCommand).NotifyCanExecuteChanged();
            }
        }
    }
}
