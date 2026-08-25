using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;

namespace StorePos.Domain.Tests.Sales;

public sealed class SaleItemMutationTests
{
    [Fact]
    public void UpdateItem_DraftSale_UpdatesItemLineTotalAndSaleTotal()
    {
        var sale = Sale.Create("20260825-0001");
        var item = sale.AddManualItem("მუხლი 20", 2m, 0.50m);

        var updatedItem = sale.UpdateItem(
            item.Id,
            "  მუხლი 25  ",
            4m,
            0.75m,
            "  შეცვლილი  ");

        Assert.Same(item, updatedItem);
        Assert.Equal("მუხლი 25", item.ProductName);
        Assert.Equal(4m, item.Quantity);
        Assert.Equal(0.75m, item.UnitPrice);
        Assert.Equal(3m, item.LineTotal);
        Assert.Equal("შეცვლილი", item.Comment);
        Assert.Equal(3m, sale.TotalAmount);
    }

    [Fact]
    public void UpdateItem_ItemBelongingToAnotherSale_Throws()
    {
        var sale = Sale.Create("20260825-0001");
        var anotherSale = Sale.Create("20260825-0002");
        var anotherItem = anotherSale.AddManualItem("შურუფი", 1m, 1m);

        Assert.Throws<KeyNotFoundException>(() =>
            sale.UpdateItem(anotherItem.Id, "შურუფი", 2m, 1m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateItem_NonPositiveQuantity_Throws(decimal quantity)
    {
        var sale = Sale.Create("20260825-0001");
        var item = sale.AddManualItem("მუხლი", 1m, 1m);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sale.UpdateItem(item.Id, "მუხლი", quantity, 1m));
    }

    [Fact]
    public void UpdateItem_NegativePrice_Throws()
    {
        var sale = Sale.Create("20260825-0001");
        var item = sale.AddManualItem("მუხლი", 1m, 1m);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sale.UpdateItem(item.Id, "მუხლი", 1m, -0.01m));
    }

    [Fact]
    public void UpdateItem_CompletedSale_Throws()
    {
        var sale = Sale.Create("20260825-0001");
        var item = sale.AddManualItem("მუხლი", 1m, 1m);
        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 1m)],
            DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            sale.UpdateItem(item.Id, "მუხლი", 2m, 1m));
    }

    [Fact]
    public void UpdateItem_CancelledSale_Throws()
    {
        var sale = Sale.Create("20260825-0001");
        var item = sale.AddManualItem("მუხლი", 1m, 1m);
        sale.Cancel(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            sale.UpdateItem(item.Id, "მუხლი", 2m, 1m));
    }

    [Fact]
    public void RemoveItem_DraftSale_RemovesItemAndRecalculatesTotal()
    {
        var sale = Sale.Create("20260825-0001");
        var item = sale.AddManualItem("მუხლი", 2m, 0.50m);

        var removedItem = sale.RemoveItem(item.Id);

        Assert.Same(item, removedItem);
        Assert.Empty(sale.Items);
        Assert.Equal(0m, sale.TotalAmount);
    }

    [Fact]
    public void RemoveItem_InvalidItemId_Throws()
    {
        var sale = Sale.Create("20260825-0001");

        Assert.Throws<KeyNotFoundException>(() => sale.RemoveItem(999));
    }

    [Fact]
    public void RemoveItem_CompletedSale_Throws()
    {
        var sale = Sale.Create("20260825-0001");
        var item = sale.AddManualItem("მუხლი", 1m, 1m);
        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 1m)],
            DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => sale.RemoveItem(item.Id));
    }

    [Fact]
    public void RemoveItem_CancelledSale_Throws()
    {
        var sale = Sale.Create("20260825-0001");
        var item = sale.AddManualItem("მუხლი", 1m, 1m);
        sale.Cancel(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => sale.RemoveItem(item.Id));
    }
}
