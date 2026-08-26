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
        GenerateBarcodeCommand = new RelayCommand(GenerateBarcode);
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

                if (string.IsNullOrWhiteSpace(Barcode))
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
        if (!TryGetPrice(out var price) || SelectedMeasurementUnit is null)
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
                Barcode.Trim(),
                Name.Trim(),
                SelectedMeasurementUnit.Id,
                FinancialInputPrecision.RoundUnitPrice(price));
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
           Code.Trim().All(character => character is >= '0' and <= '9') &&
           !string.IsNullOrWhiteSpace(Barcode) &&
           Barcode.Trim().Length <= 100 &&
           SelectedMeasurementUnit is not null &&
           TryGetPrice(out _);

    private bool TryGetPrice(out decimal price)
        => DecimalInputParser.TryParse(Price, out price) &&
           FinancialInputPrecision.RoundUnitPrice(price) >= 0.00001m &&
           FinancialInputPrecision.RoundUnitPrice(price) <=
               FinancialInputPrecision.MaximumFiveScaleValue;

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
        }
    }
}
