using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;

namespace StorePos.Domain.Tests.Sales;

public sealed class SaleInfoTests
{
    [Fact]
    public void UpdateComment_DraftSale_TrimsValue()
    {
        var sale = Sale.Create("20260825-0001");
        sale.UpdateComment("  კონკრეტული გაყიდვის ინფორმაცია  ");
        Assert.Equal("კონკრეტული გაყიდვის ინფორმაცია", sale.Comment);
    }

    [Fact]
    public void UpdateComment_EmptyValue_NormalizesToNull()
    {
        var sale = Sale.Create("20260825-0001", comment: "Comment");
        sale.UpdateComment(" ");
        Assert.Null(sale.Comment);
    }

    [Fact]
    public void UpdateComment_CompletedSale_Throws()
    {
        var sale = CreateCompletedSale();
        Assert.Throws<InvalidOperationException>(() => sale.UpdateComment("Comment"));
    }

    [Fact]
    public void UpdateComment_CancelledSale_Throws()
    {
        var sale = Sale.Create("20260825-0001");
        sale.Cancel(DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => sale.UpdateComment("Comment"));
    }

    private static Sale CreateCompletedSale()
    {
        var sale = Sale.Create("20260825-0001");
        sale.AddManualItem("Product", 1m, 1m);
        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 1m)],
            DateTime.UtcNow);
        return sale;
    }
}
