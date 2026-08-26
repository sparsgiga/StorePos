using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;

namespace StorePos.Domain.Tests.Sales;

public sealed class SaleCompletionTests
{
    private static readonly DateTime CompletionDate =
        new(2026, 8, 25, 12, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Complete_DraftWithItemAndSinglePayment_CompletesSale()
    {
        var sale = CreateSaleWithTotal(272m);

        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 272m)],
            CompletionDate);

        Assert.Equal(SaleStatus.Completed, sale.Status);
        Assert.Equal(CompletionDate, sale.DateCompleted);
        Assert.Null(sale.DateCancelled);
        var payment = Assert.Single(sale.Payments);
        Assert.Equal(PaymentType.Cash, payment.PaymentType);
        Assert.Equal(SalePaymentKind.Completion, payment.PaymentKind);
        Assert.Equal(272m, payment.Amount);
        Assert.Equal(272m, sale.PaidAmount);
        Assert.Equal(0m, sale.OutstandingAmount);
        Assert.False(sale.HasDebt);
    }

    [Fact]
    public void Complete_MultiplePaymentsWithExactTotal_CompletesSale()
    {
        var sale = CreateSaleWithTotal(272m);

        sale.Complete(
            [
                new SalePaymentAllocation(PaymentType.Cash, 100m),
                new SalePaymentAllocation(PaymentType.Card, 172m)
            ],
            CompletionDate);

        Assert.Equal(SaleStatus.Completed, sale.Status);
        Assert.Collection(
            sale.Payments,
            payment =>
            {
                Assert.Equal(PaymentType.Cash, payment.PaymentType);
                Assert.Equal(100m, payment.Amount);
            },
            payment =>
            {
                Assert.Equal(PaymentType.Card, payment.PaymentType);
                Assert.Equal(172m, payment.Amount);
            });
    }

    [Theory]
    [InlineData(271.994)]
    [InlineData(272.006)]
    public void Complete_PaymentTotalDoesNotMatch_ThrowsWithoutChangingSale(
        decimal paymentAmount)
    {
        var sale = CreateSaleWithTotal(272m);

        Assert.Throws<InvalidOperationException>(() => sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, paymentAmount)],
            CompletionDate));

        Assert.Equal(SaleStatus.Draft, sale.Status);
        Assert.Null(sale.DateCompleted);
        Assert.Empty(sale.Payments);
    }

    [Fact]
    public void Complete_NegativePayment_ThrowsWithoutChangingSale()
    {
        var sale = CreateSaleWithTotal(272m);

        Assert.Throws<ArgumentOutOfRangeException>(() => sale.Complete(
            [
                new SalePaymentAllocation(PaymentType.Cash, -1m),
                new SalePaymentAllocation(PaymentType.Card, 273m)
            ],
            CompletionDate));

        Assert.Equal(SaleStatus.Draft, sale.Status);
        Assert.Empty(sale.Payments);
    }

    [Fact]
    public void Complete_EmptyPayments_Throws()
    {
        var sale = CreateSaleWithTotal(272m);

        Assert.Throws<InvalidOperationException>(() =>
            sale.Complete([], CompletionDate));
    }

    [Fact]
    public void Complete_PartialCreditWithCustomer_CompletesWithDebt()
    {
        var sale = CreateSaleWithTotal(200m);
        sale.AssignCustomer(10, "Customer", "01000000000");

        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 100m)],
            CompletionDate,
            allowDebt: true);

        Assert.Equal(SaleStatus.Completed, sale.Status);
        Assert.Equal(100m, sale.PaidAmount);
        Assert.Equal(100m, sale.OutstandingAmount);
        Assert.True(sale.HasDebt);
        Assert.Equal(SalePaymentKind.Completion, Assert.Single(sale.Payments).PaymentKind);
    }

    [Fact]
    public void Complete_FullDebtWithCustomer_DoesNotCreateZeroPayment()
    {
        var sale = CreateSaleWithTotal(200m);
        sale.AssignCustomer(10, "Customer", null);

        sale.Complete([], CompletionDate, allowDebt: true);

        Assert.Equal(SaleStatus.Completed, sale.Status);
        Assert.Equal(0m, sale.PaidAmount);
        Assert.Equal(200m, sale.OutstandingAmount);
        Assert.True(sale.HasDebt);
        Assert.Empty(sale.Payments);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Complete_DebtWithoutAssignedCustomer_Throws(decimal paidAmount)
    {
        var sale = CreateSaleWithTotal(200m);
        var payments = paidAmount == 0
            ? Array.Empty<SalePaymentAllocation>()
            : [new SalePaymentAllocation(PaymentType.Cash, paidAmount)];

        Assert.Throws<InvalidOperationException>(() =>
            sale.Complete(payments, CompletionDate, allowDebt: true));

        Assert.Equal(SaleStatus.Draft, sale.Status);
        Assert.Empty(sale.Payments);
    }

    [Fact]
    public void Complete_OverpaymentWithDebtPermission_Throws()
    {
        var sale = CreateSaleWithTotal(200m);
        sale.AssignCustomer(10, "Customer", null);

        Assert.Throws<InvalidOperationException>(() => sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 201m)],
            CompletionDate,
            allowDebt: true));
    }

    [Fact]
    public void Complete_DraftWithNoItems_Throws()
    {
        var sale = Sale.Create("20260825-0001");

        Assert.Throws<InvalidOperationException>(() => sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 0m)],
            CompletionDate));
    }

    [Fact]
    public void Complete_UsesCanonicalMoneyPrecision()
    {
        var sale = Sale.Create("20260825-0001");
        sale.AddManualItem("პროდუქტი", 3m, 0.111111m);

        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 0.33333m)],
            CompletionDate);

        Assert.Equal(SaleStatus.Completed, sale.Status);
        Assert.Equal(0.33m, Assert.Single(sale.Payments).Amount);
        Assert.Equal(0.33m, sale.TotalAmount);
    }

    [Fact]
    public void Complete_RoundsEverySplitPaymentBeforeSumming()
    {
        var sale = CreateSaleWithTotal(0.02m);

        sale.Complete(
            [
                new SalePaymentAllocation(PaymentType.Cash, 0.005m),
                new SalePaymentAllocation(PaymentType.Card, 0.005m)
            ],
            CompletionDate);

        Assert.Equal([0.01m, 0.01m], sale.Payments.Select(payment => payment.Amount));
        Assert.Equal(0.02m, sale.PaidAmount);
    }

    [Fact]
    public void Complete_CompletedSale_Throws()
    {
        var sale = CreateSaleWithTotal(1m);
        var payments = new[] { new SalePaymentAllocation(PaymentType.Cash, 1m) };
        sale.Complete(payments, CompletionDate);

        Assert.Throws<InvalidOperationException>(() =>
            sale.Complete(payments, CompletionDate.AddMinutes(1)));
    }

    [Fact]
    public void Complete_CancelledSale_Throws()
    {
        var sale = CreateSaleWithTotal(1m);
        sale.Cancel(CompletionDate);

        Assert.Throws<InvalidOperationException>(() => sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 1m)],
            CompletionDate.AddMinutes(1)));
    }

    private static Sale CreateSaleWithTotal(decimal totalAmount)
    {
        var sale = Sale.Create("20260825-0001");
        sale.AddManualItem("პროდუქტი", 1m, totalAmount);
        return sale;
    }
}
