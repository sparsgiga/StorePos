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

        var first = sale.AddDebtPayment(Guid.NewGuid(), PaymentType.Cash, 60m);
        Assert.Equal(SalePaymentKind.DebtRepayment, first.PaymentKind);
        Assert.Equal(160m, sale.PaidAmount);
        Assert.Equal(40m, sale.OutstandingAmount);
        Assert.True(sale.HasDebt);

        var second = sale.AddDebtPayment(Guid.NewGuid(), PaymentType.Card, 40m);
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
            sale.AddDebtPayment(Guid.NewGuid(), PaymentType.Cash, amount));
    }

    [Fact]
    public void AddDebtPayment_AmountAboveOutstanding_Throws()
    {
        var sale = CreateDebtSale();

        Assert.Throws<InvalidOperationException>(() =>
            sale.AddDebtPayment(Guid.NewGuid(), PaymentType.Cash, 101m));
    }

    [Fact]
    public void AddDebtPayment_SameOperationAndPayloadReturnsExistingPayment()
    {
        var sale = CreateDebtSale();
        var operationId = Guid.NewGuid();

        var first = sale.AddDebtPayment(operationId, PaymentType.Cash, 10.001m);
        var retry = sale.AddDebtPayment(operationId, PaymentType.Cash, 10m);

        Assert.Same(first, retry);
        Assert.Equal(2, sale.Payments.Count);
        Assert.Equal(1, sale.FinancialRevision);
    }

    [Fact]
    public void AddDebtPayment_SameOperationWithDifferentPayloadIsRejected()
    {
        var sale = CreateDebtSale();
        var operationId = Guid.NewGuid();
        sale.AddDebtPayment(operationId, PaymentType.Cash, 10m);

        Assert.Throws<InvalidOperationException>(() =>
            sale.AddDebtPayment(operationId, PaymentType.Card, 10m));
        Assert.Throws<InvalidOperationException>(() =>
            sale.AddDebtPayment(operationId, PaymentType.Cash, 11m));
        Assert.Equal(2, sale.Payments.Count);
    }

    [Fact]
    public void DraftAndCancelledSalesNeverReportOutstandingDebt()
    {
        var draft = Sale.Create("20260825-0010");
        draft.AddManualItem("Item", 1m, 10m);
        var cancelled = Sale.Create("20260825-0011");
        cancelled.AddManualItem("Item", 1m, 10m);
        cancelled.Cancel(CompletionDate);

        Assert.Equal(0m, draft.OutstandingAmount);
        Assert.Equal(0m, cancelled.OutstandingAmount);
        Assert.False(draft.HasDebt);
        Assert.False(cancelled.HasDebt);
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
            draft.AddDebtPayment(Guid.NewGuid(), PaymentType.Cash, 1m));
        Assert.Throws<InvalidOperationException>(() =>
            cancelled.AddDebtPayment(Guid.NewGuid(), PaymentType.Cash, 1m));
        Assert.Throws<InvalidOperationException>(() =>
            paid.AddDebtPayment(Guid.NewGuid(), PaymentType.Cash, 1m));
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
