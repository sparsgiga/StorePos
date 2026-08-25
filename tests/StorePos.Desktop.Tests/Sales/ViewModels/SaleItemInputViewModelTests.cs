using StorePos.Desktop.Sales.ViewModels;

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
}
