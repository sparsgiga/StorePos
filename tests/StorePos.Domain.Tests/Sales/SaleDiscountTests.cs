using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Common;
using StorePos.Domain.Enums;

namespace StorePos.Domain.Tests.Sales;

public sealed class SaleDiscountTests
{
    [Fact]
    public void UpdateDiscount_UsesSubtotalAndProducesFinalPayableTotal()
    {
        var sale = CreateSale(601m);

        sale.UpdateDiscount(1m);

        Assert.Equal(601m, sale.Subtotal);
        Assert.Equal(1m, sale.DiscountAmount);
        Assert.Equal(600m, sale.TotalAmount);
    }

    [Fact]
    public void ZeroAndFullDiscount_AreValid()
    {
        var sale = CreateSale(50m);

        sale.UpdateDiscount(0m);
        Assert.Equal(50m, sale.TotalAmount);

        sale.UpdateDiscount(50m);
        Assert.Equal(0m, sale.TotalAmount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(51)]
    public void InvalidDiscount_IsRejectedAndPreviousStateIsPreserved(decimal discount)
    {
        var sale = CreateSale(50m);

        Assert.ThrowsAny<Exception>(() => sale.UpdateDiscount(discount));
        Assert.Equal(0m, sale.DiscountAmount);
        Assert.Equal(50m, sale.TotalAmount);
    }

    [Fact]
    public void ItemUpdateBelowDiscount_IsRejectedWithoutPartialMutation()
    {
        var sale = CreateSale(601m);
        var item = Assert.Single(sale.Items);
        sale.UpdateDiscount(100m);

        Assert.Throws<InvalidOperationException>(() =>
            sale.UpdateItem(item.Id, item.ProductName, 1m, 80m));

        Assert.Equal(601m, item.LineTotal);
        Assert.Equal(601m, sale.Subtotal);
        Assert.Equal(100m, sale.DiscountAmount);
        Assert.Equal(501m, sale.TotalAmount);
    }

    [Fact]
    public void ItemRemovalBelowDiscount_IsRejectedWithoutRemovingItem()
    {
        var sale = CreateSale(100m);
        var item = Assert.Single(sale.Items);
        sale.UpdateDiscount(50m);

        Assert.Throws<InvalidOperationException>(() => sale.RemoveItem(item.Id));

        Assert.Single(sale.Items);
        Assert.Equal(100m, sale.Subtotal);
        Assert.Equal(50m, sale.TotalAmount);
    }

    [Fact]
    public void InlineFinancialChanges_KeepCanonicalFieldsConsistent()
    {
        var sale = Sale.Create("20260828-0002");
        var item = sale.AddManualItem("Product", 2m, 10m);

        sale.UpdateItemQuantity(item.Id, 3m);
        Assert.Equal(10m, item.UnitPrice);
        Assert.Equal(30m, item.LineTotal);

        sale.UpdateItemUnitPrice(item.Id, 9m);
        Assert.Equal(27m, item.LineTotal);

        sale.UpdateItemLineTotal(item.Id, 24m);
        Assert.Equal(8m, item.UnitPrice);
        Assert.Equal(24m, item.LineTotal);
        Assert.Equal(item.Quantity * item.UnitPrice, item.LineTotal);
    }

    [Fact]
    public void LineTotalUpdate_ReturnsCanonicalRoundedValuesWhenExactValueCannotBeRepresented()
    {
        var sale = Sale.Create("20260828-0003");
        var item = sale.AddManualItem("Product", 12_345m, 1m);

        sale.UpdateItemLineTotal(item.Id, 1m);

        Assert.Equal(0.00008m, item.UnitPrice);
        Assert.Equal(0.99m, item.LineTotal);
        Assert.Equal(
            FinancialPrecision.CalculateLineTotal(item.Quantity, item.UnitPrice),
            item.LineTotal);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void InvalidInlineFinancialChange_PreservesPreviousItemAndSaleState(int field)
    {
        var sale = CreateSale(10m);
        var item = Assert.Single(sale.Items);

        Assert.ThrowsAny<ArgumentException>(() => field switch
        {
            1 => sale.UpdateItemQuantity(item.Id, 0m),
            2 => sale.UpdateItemUnitPrice(item.Id, 0m),
            _ => sale.UpdateItemLineTotal(item.Id, 0m)
        });

        Assert.Equal(1m, item.Quantity);
        Assert.Equal(10m, item.UnitPrice);
        Assert.Equal(10m, item.LineTotal);
        Assert.Equal(10m, sale.Subtotal);
        Assert.Equal(10m, sale.TotalAmount);
    }

    [Fact]
    public void ItemChangeThatKeepsSubtotalAboveDiscount_IsAccepted()
    {
        var sale = CreateSale(100m);
        var item = Assert.Single(sale.Items);
        sale.UpdateDiscount(50m);

        sale.UpdateItemUnitPrice(item.Id, 80m);

        Assert.Equal(80m, sale.Subtotal);
        Assert.Equal(50m, sale.DiscountAmount);
        Assert.Equal(30m, sale.TotalAmount);
    }

    [Fact]
    public void DiscountedCompletionAndDebtUseFinalTotal()
    {
        var paidSale = CreateSale(601m);
        paidSale.UpdateDiscount(1m);
        paidSale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 600m)],
            DateTime.Now);
        Assert.Equal(600m, paidSale.PaidAmount);
        Assert.Equal(0m, paidSale.OutstandingAmount);

        var debtSale = CreateSale(601m);
        debtSale.AssignCustomer(1, "Customer", null);
        debtSale.UpdateDiscount(1m);
        debtSale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 400m)],
            DateTime.Now,
            allowDebt: true);
        Assert.Equal(200m, debtSale.OutstandingAmount);
    }

    [Fact]
    public void ReopenPreservesDiscountAndPaymentHistory()
    {
        var sale = CreateSale(601m);
        sale.UpdateDiscount(1m);
        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 600m)],
            DateTime.Now);
        var payment = Assert.Single(sale.Payments);

        sale.Reopen();

        Assert.Equal(1m, sale.DiscountAmount);
        Assert.Equal(600m, sale.TotalAmount);
        Assert.Contains(payment, sale.Payments);
    }

    private static Sale CreateSale(decimal unitPrice)
    {
        var sale = Sale.Create("20260828-0001");
        sale.AddManualItem("Product", 1m, unitPrice);
        return sale;
    }
}
