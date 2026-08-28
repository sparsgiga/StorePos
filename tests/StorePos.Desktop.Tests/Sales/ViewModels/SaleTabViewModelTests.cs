using StorePos.Desktop.Sales.ViewModels;

namespace StorePos.Desktop.Tests.Sales.ViewModels;

public sealed class SaleTabViewModelTests
{
    [Fact]
    public void CustomerState_UsesExplicitMissingAndSelectedSemantics()
    {
        var sale = new SaleTabViewModel(
            1,
            "20260828-0001",
            10m,
            0m,
            10m,
            DateTime.Now,
            null,
            null);

        Assert.False(sale.HasAssignedCustomer);
        Assert.Equal("მყიდველი არ არის მითითებული", sale.CustomerStatusText);

        sale.ApplyCustomerInfo(5, "გიორგი გიორგაძე", null, null);

        Assert.True(sale.HasAssignedCustomer);
        Assert.Equal("გიორგი გიორგაძე", sale.CustomerStatusText);
    }

    [Fact]
    public void ApplyFinancials_UpdatesSubtotalDiscountAndFinalTotalTogether()
    {
        var sale = new SaleTabViewModel(
            1,
            "20260828-0001",
            601m,
            0m,
            601m,
            DateTime.Now,
            null,
            null);

        sale.ApplyFinancials(601m, 1m, 600m);

        Assert.Equal(601m, sale.Subtotal);
        Assert.Equal(1m, sale.DiscountAmount);
        Assert.Equal(600m, sale.TotalAmount);
        Assert.Equal("1", sale.DiscountText);
    }
}
