using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;

namespace StorePos.Domain.Tests.Sales;

public sealed class SaleDebtPaymentTests
{
    private static readonly DateTime CompletionDate = new(2026, 8, 25, 12, 0, 0);

    [Fact]
    public void AddDebtPayment_PartiallyThenFullyPaysDebtWithoutChangingStatus()
    {
        var sale = CreateDebtSale();

        var first = sale.AddDebtPayment(PaymentType.Cash, 60m);
        Assert.Equal(SalePaymentKind.DebtRepayment, first.PaymentKind);
        Assert.Equal(160m, sale.PaidAmount);
        Assert.Equal(40m, sale.OutstandingAmount);
        Assert.True(sale.HasDebt);

        var second = sale.AddDebtPayment(PaymentType.Card, 40m);
        Assert.Equal(SalePaymentKind.DebtRepayment, second.PaymentKind);
        Assert.Equal(200m, sale.PaidAmount);
        Assert.Equal(0m, sale.OutstandingAmount);
        Assert.False(sale.HasDebt);
        Assert.Equal(SaleStatus.Completed, sale.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void AddDebtPayment_NonpositiveAmount_Throws(decimal amount)
    {
        var sale = CreateDebtSale();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sale.AddDebtPayment(PaymentType.Cash, amount));
    }

    [Fact]
    public void AddDebtPayment_AmountAboveOutstanding_Throws()
    {
        var sale = CreateDebtSale();

        Assert.Throws<InvalidOperationException>(() =>
            sale.AddDebtPayment(PaymentType.Cash, 101m));
    }

    [Fact]
    public void AddDebtPayment_OnDraftCancelledOrFullyPaidSale_Throws()
    {
        var draft = Sale.Create("20260825-0002");
        draft.AddManualItem("Item", 1m, 10m);
        var cancelled = Sale.Create("20260825-0003");
        cancelled.AddManualItem("Item", 1m, 10m);
        cancelled.Cancel(CompletionDate);
        var paid = Sale.Create("20260825-0004");
        paid.AddManualItem("Item", 1m, 10m);
        paid.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 10m)],
            CompletionDate);

        Assert.Throws<InvalidOperationException>(() =>
            draft.AddDebtPayment(PaymentType.Cash, 1m));
        Assert.Throws<InvalidOperationException>(() =>
            cancelled.AddDebtPayment(PaymentType.Cash, 1m));
        Assert.Throws<InvalidOperationException>(() =>
            paid.AddDebtPayment(PaymentType.Cash, 1m));
    }

    private static Sale CreateDebtSale()
    {
        var sale = Sale.Create("20260825-0001");
        sale.AssignCustomer(10, "Customer", null);
        sale.AddManualItem("Item", 1m, 200m);
        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 100m)],
            CompletionDate,
            allowDebt: true);
        return sale;
    }
}
