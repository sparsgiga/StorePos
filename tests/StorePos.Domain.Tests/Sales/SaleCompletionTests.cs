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
        Assert.Equal(272m, payment.Amount);
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
    [InlineData(271.99999)]
    [InlineData(272.00001)]
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
    public void Complete_DraftWithNoItems_Throws()
    {
        var sale = Sale.Create("20260825-0001");

        Assert.Throws<InvalidOperationException>(() => sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 0m)],
            CompletionDate));
    }

    [Fact]
    public void Complete_UsesFiveDecimalPrecision()
    {
        var sale = Sale.Create("20260825-0001");
        sale.AddManualItem("პროდუქტი", 3m, 0.111111m);

        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 0.33333m)],
            CompletionDate);

        Assert.Equal(SaleStatus.Completed, sale.Status);
        Assert.Equal(0.33333m, Assert.Single(sale.Payments).Amount);
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
