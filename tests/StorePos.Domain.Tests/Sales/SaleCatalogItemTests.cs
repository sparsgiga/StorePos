using StorePos.Domain.Aggregates.Sale;

namespace StorePos.Domain.Tests.Sales;

public sealed class SaleCatalogItemTests
{
    [Fact]
    public void AddProductItem_CreatesCatalogSnapshot()
    {
        var sale = Sale.Create("20260825-0001");

        var addition = sale.AddProductItem(
            10,
            "PRD-10",
            "12345678",
            "Cement",
            1,
            "Piece",
            2m,
            3.50m);

        Assert.True(addition.WasNewItem);
        Assert.False(addition.Item.IsManual);
        Assert.Equal(10, addition.Item.ProductId);
        Assert.Equal("PRD-10", addition.Item.ProductCode);
        Assert.Equal(7m, addition.Item.LineTotal);
        Assert.Equal(7m, sale.TotalAmount);
    }

    [Fact]
    public void AddSameProduct_IncreasesQuantityAndPreservesExistingPrice()
    {
        var sale = Sale.Create("20260825-0001");
        var first = sale.AddProductItem(
            10, "PRD-10", null, "Cement", 1, "Piece", 1m, 3.50m);

        var second = sale.AddProductItem(
            10, "PRD-10", null, "Changed catalog name", 1, "Piece", 1m, 9m);

        Assert.False(second.WasNewItem);
        Assert.Same(first.Item, second.Item);
        Assert.Single(sale.Items);
        Assert.Equal(2m, second.Item.Quantity);
        Assert.Equal(3.50m, second.Item.UnitPrice);
        Assert.Equal(7m, second.Item.LineTotal);
        Assert.Equal("Cement", second.Item.ProductName);
    }

    [Fact]
    public void CatalogAndManualItem_WithSameName_DoNotMerge()
    {
        var sale = Sale.Create("20260825-0001");
        sale.AddManualItem("Cement", 1m, 3.50m);

        sale.AddProductItem(
            10, "PRD-10", null, "Cement", 1, "Piece", 1m, 3.50m);

        Assert.Equal(2, sale.Items.Count);
    }

    [Fact]
    public void UpdateCatalogItem_DoesNotChangeProductSnapshotName()
    {
        var sale = Sale.Create("20260825-0001");
        var item = sale.AddProductItem(
            10, "PRD-10", null, "Cement", 1, "Piece", 1m, 3.50m).Item;

        sale.UpdateItem(item.Id, "Changed", 2m, 4m, "Updated");

        Assert.Equal("Cement", item.ProductName);
        Assert.Equal(2m, item.Quantity);
        Assert.Equal(4m, item.UnitPrice);
        Assert.Equal("Updated", item.Comment);
    }
}
