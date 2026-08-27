using StorePos.Desktop.Sales.ViewModels;
using StorePos.Desktop.Products.Models;

namespace StorePos.Desktop.Tests.Sales.ViewModels;

public sealed class SaleItemInputViewModelTests
{
    [Fact]
    public void QuantityAndPrice_UpdatesCalculatedTotalAndCompleteness()
    {
        var input = new SaleItemInputViewModel
        {
            ProductName = "სამაგრი",
            Quantity = "500",
            UnitPrice = "0,44"
        };

        Assert.Equal("220", input.LineTotal);
        Assert.True(input.IsLineTotalReadOnly);
        Assert.True(input.IsComplete);
    }

    [Fact]
    public void PriceAndTotal_UpdatesCalculatedQuantity()
    {
        var input = new SaleItemInputViewModel
        {
            ProductName = "სამაგრი",
            UnitPrice = "0.44",
            LineTotal = "220"
        };

        Assert.Equal("500", input.Quantity);
        Assert.True(input.IsQuantityReadOnly);
        Assert.True(input.IsComplete);
    }

    [Fact]
    public void Load_ForEdit_UsesSameCalculatorState()
    {
        var input = new SaleItemInputViewModel();

        input.Load("მუხლი", 4m, 0.50m, "კომენტარი");

        Assert.Equal("2", input.LineTotal);
        Assert.True(input.IsLineTotalReadOnly);
        Assert.True(input.IsComplete);
    }

    [Fact]
    public void SaveToCatalog_RequiresMeasurementUnitAndBarcode()
    {
        var input = new SaleItemInputViewModel
        {
            ProductName = "Cement",
            Quantity = "1",
            UnitPrice = "0.444",
            SaveToCatalog = true
        };

        Assert.True(input.IsComplete);
        Assert.False(input.CanSubmit);

        input.LoadMeasurementUnits([new MeasurementUnitDto(1, "Piece", "pc", null)]);
        input.ProductCode = "10526";

        Assert.False(input.CanSubmit);

        input.Barcode = "0000000105262";

        Assert.True(input.CanSubmit);
        Assert.Equal(1, input.SelectedMeasurementUnit?.Id);
    }

    [Fact]
    public void ManualItem_DoesNotRequireBarcode()
    {
        var input = new SaleItemInputViewModel
        {
            ProductName = "Cement",
            Quantity = "1",
            UnitPrice = "2"
        };

        Assert.True(input.CanSubmit);
        Assert.Null(input.Barcode);
    }

    [Fact]
    public void CreationDefaults_SelectSemanticUnitByReturnedIdAndPreserveManualCodeOverride()
    {
        var input = new SaleItemInputViewModel();
        input.LoadMeasurementUnits(
        [
            new MeasurementUnitDto(1, "Kilogram", "kg", null),
            new MeasurementUnitDto(24, "ცალი", "ც", null)
        ]);
        input.ProductCode = "20000";

        input.ApplyCreationDefaults(new ProductCreationDefaultsDto(
            "10526", 24, "ცალი", "ც", null));

        Assert.Equal("20000", input.ProductCode);
        Assert.Equal(24, input.SelectedMeasurementUnit?.Id);
        Assert.Equal("0000000200004", input.Barcode);
    }

    [Fact]
    public void CreationDefaults_GeneratesBarcodeFromSuggestedCodeWhenEmpty()
    {
        var input = new SaleItemInputViewModel();

        input.ApplyCreationDefaults(new ProductCreationDefaultsDto(
            "10525", null, null, null, null));

        Assert.Equal("10525", input.ProductCode);
        Assert.Equal("0000000105255", input.Barcode);
    }

    [Fact]
    public void CreationDefaults_DoesNotOverwriteScannedBarcode()
    {
        var input = new SaleItemInputViewModel();
        input.PrepareManualFallback("4860123456789", isBarcode: true);

        input.ApplyCreationDefaults(new ProductCreationDefaultsDto(
            "10525", null, null, null, null));

        Assert.Equal("10525", input.ProductCode);
        Assert.Equal("4860123456789", input.Barcode);
    }

    [Fact]
    public void CreationDefaults_NonnumericProductCodeDoesNotGenerateBarcode()
    {
        var input = new SaleItemInputViewModel();

        input.ApplyCreationDefaults(new ProductCreationDefaultsDto(
            "GMTEK-40012", null, null, null, null));

        Assert.Null(input.Barcode);
        Assert.False(input.GenerateBarcodeCommand.CanExecute(null));
    }

    [Fact]
    public void GenerateBarcode_RecalculatesFromCurrentEditedCode()
    {
        var input = new SaleItemInputViewModel { ProductCode = "10525" };

        input.GenerateBarcodeCommand.Execute(null);
        Assert.Equal("0000000105255", input.Barcode);

        input.ProductCode = "21000";
        input.GenerateBarcodeCommand.Execute(null);
        Assert.Equal("0000000210003", input.Barcode);
    }

    [Fact]
    public void BarcodeFallback_DoesNotPutBarcodeIntoProductName()
    {
        var input = new SaleItemInputViewModel();

        input.PrepareManualFallback("12345678", isBarcode: true);

        Assert.Equal("12345678", input.Barcode);
        Assert.Null(input.ProductName);
    }
}
