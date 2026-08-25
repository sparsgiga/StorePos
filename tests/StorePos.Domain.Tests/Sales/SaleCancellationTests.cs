using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;

namespace StorePos.Domain.Tests.Sales;

public sealed class SaleCancellationTests
{
    private static readonly DateTime CancellationDate =
        new(2026, 8, 25, 13, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Cancel_DraftSale_CancelsAndKeepsItems()
    {
        var sale = Sale.Create("20260825-0001");
        var item = sale.AddManualItem("პროდუქტი", 2m, 5m);

        sale.Cancel(CancellationDate);

        Assert.Equal(SaleStatus.Cancelled, sale.Status);
        Assert.Equal(CancellationDate, sale.DateCancelled);
        Assert.Null(sale.DateCompleted);
        Assert.Same(item, Assert.Single(sale.Items));
    }

    [Fact]
    public void Cancel_CompletedSale_Throws()
    {
        var sale = Sale.Create("20260825-0001");
        sale.AddManualItem("პროდუქტი", 1m, 10m);
        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Card, 10m)],
            CancellationDate.AddMinutes(-1));

        Assert.Throws<InvalidOperationException>(() => sale.Cancel(CancellationDate));
    }

    [Fact]
    public void Cancel_CancelledSale_Throws()
    {
        var sale = Sale.Create("20260825-0001");
        sale.Cancel(CancellationDate);

        Assert.Throws<InvalidOperationException>(() =>
            sale.Cancel(CancellationDate.AddMinutes(1)));
    }
}
