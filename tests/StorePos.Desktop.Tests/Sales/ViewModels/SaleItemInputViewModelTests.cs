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
    public void SaveToCatalog_RequiresMeasurementUnit()
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

        Assert.True(input.CanSubmit);
        Assert.Equal(1, input.SelectedMeasurementUnit?.Id);
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
