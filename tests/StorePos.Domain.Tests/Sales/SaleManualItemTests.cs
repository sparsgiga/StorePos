using StorePos.Domain.Aggregates.Sale;

namespace StorePos.Domain.Tests.Sales;

public sealed class SaleManualItemTests
{
    [Fact]
    public void AddManualItem_AddsManualItemAndUpdatesTotal()
    {
        var sale = Sale.Create("20260824-0001");

        var item = sale.AddManualItem("მუხლი 20", 2m, 0.50000m, "Manual item");

        Assert.Same(item, Assert.Single(sale.Items));
        Assert.Null(item.ProductId);
        Assert.Null(item.ProductCode);
        Assert.Null(item.Barcode);
        Assert.True(item.IsManual);
        Assert.Equal("მუხლი 20", item.ProductName);
        Assert.Equal(2m, item.Quantity);
        Assert.Equal(0.50000m, item.UnitPrice);
        Assert.Equal(1.00000m, item.LineTotal);
        Assert.Equal("Manual item", item.Comment);
        Assert.Equal(1.00000m, sale.TotalAmount);
    }

    [Fact]
    public void AddManualItem_MultipleItems_RecalculatesTotal()
    {
        var sale = Sale.Create("20260824-0001");

        sale.AddManualItem("მუხლი 20", 2m, 0.50000m);
        sale.AddManualItem("შურუფი", 10m, 0.20000m);

        Assert.Equal(2, sale.Items.Count);
        Assert.Equal(3.00000m, sale.TotalAmount);
    }

    [Fact]
    public void AddManualItem_CompletedSale_Throws()
    {
        var sale = Sale.Create("20260824-0001");
        sale.Complete(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            sale.AddManualItem("მუხლი", 1m, 1m));
    }

    [Fact]
    public void AddManualItem_CancelledSale_Throws()
    {
        var sale = Sale.Create("20260824-0001");
        sale.Cancel(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            sale.AddManualItem("მუხლი", 1m, 1m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddManualItem_NonPositiveQuantity_Throws(decimal quantity)
    {
        var sale = Sale.Create("20260824-0001");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sale.AddManualItem("მუხლი", quantity, 1m));
    }

    [Fact]
    public void AddManualItem_NegativePrice_Throws()
    {
        var sale = Sale.Create("20260824-0001");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sale.AddManualItem("მუხლი", 1m, -0.00001m));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AddManualItem_EmptyProductName_Throws(string? productName)
    {
        var sale = Sale.Create("20260824-0001");

        Assert.Throws<ArgumentException>(() =>
            sale.AddManualItem(productName!, 1m, 1m));
    }
}
