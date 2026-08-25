using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;

namespace StorePos.Domain.Tests.Sales;

public sealed class SaleInfoTests
{
    [Fact]
    public void UpdateInfo_DraftSale_UpdatesAndTrimsAllValues()
    {
        var sale = Sale.Create("20260825-0001");

        sale.UpdateInfo("  გიორგი  ", "  01000000000 ", "  ხელოსანი  ");

        Assert.Equal("გიორგი", sale.CustomerName);
        Assert.Equal("01000000000", sale.CustomerIdentificationNumber);
        Assert.Equal("ხელოსანი", sale.Comment);
    }

    [Fact]
    public void UpdateInfo_EmptyOptionalValues_NormalizesThemToNull()
    {
        var sale = Sale.Create(
            "20260825-0001",
            customerName: "გიორგი",
            customerIdentificationNumber: "01000000000",
            comment: "ხელოსანი");

        sale.UpdateInfo(" ", "", null);

        Assert.Null(sale.CustomerName);
        Assert.Null(sale.CustomerIdentificationNumber);
        Assert.Null(sale.Comment);
    }

    [Fact]
    public void UpdateInfo_CompletedSale_Throws()
    {
        var sale = Sale.Create("20260825-0001");
        sale.AddManualItem("პროდუქტი", 1m, 1m);
        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 1m)],
            DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            sale.UpdateInfo("გიორგი", "01000000000", null));
    }

    [Fact]
    public void UpdateInfo_CancelledSale_Throws()
    {
        var sale = Sale.Create("20260825-0001");
        sale.Cancel(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            sale.UpdateInfo("გიორგი", "01000000000", null));
    }
}
