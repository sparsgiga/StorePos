using StorePos.Desktop.History.Models;
using StorePos.Desktop.Reporting;
using StorePos.Desktop.Sales.Models;
using StorePos.Desktop.Sales.ViewModels;

namespace StorePos.Desktop.Tests.Reporting;

public sealed class SaleReportModelFactoryTests
{
    [Fact]
    public void HistoryReport_UsesItemSnapshotsAndOnlyCurrentPaymentVersion()
    {
        var details = CreateDetails(
            status: 2,
            completionVersion: 2,
            payments:
            [
                new SaleDetailsPaymentDto(1, 1, 1, 500m, DateTime.Now.AddDays(-1)),
                new SaleDetailsPaymentDto(1, 2, 2, 200m, DateTime.Now.AddHours(-2)),
                new SaleDetailsPaymentDto(2, 1, 1, 500m, DateTime.Now.AddHours(-1)),
                new SaleDetailsPaymentDto(2, 2, 1, 200m, DateTime.Now)
            ]);

        var report = SaleReportModelFactory.FromSaleDetails(details, DateTime.Now);

        var item = Assert.Single(report.Items);
        Assert.Equal("Snapshot Cement M500", item.ProductName);
        Assert.Equal(18.12345m, item.UnitPrice);
        Assert.Equal(2.5m, item.Quantity);
        Assert.Equal(45.31m, item.LineTotal);
        Assert.Equal(1000m, report.TotalAmount);
        Assert.Equal(700m, report.PaidAmount);
        Assert.Equal(300m, report.OutstandingAmount);
        Assert.Equal(500m, report.CashAmount);
        Assert.Equal(200m, report.CardAmount);
    }

    [Theory]
    [InlineData(1, "დაუსრულებელი გაყიდვა")]
    [InlineData(3, "გაუქმებული")]
    public void Status_IsClearlyRepresented(int status, string expected)
        => Assert.Equal(expected, ReportFormatting.Status(status));

    [Theory]
    [InlineData(10, "10")]
    [InlineData(2.5, "2.5")]
    [InlineData(1.125, "1.125")]
    public void QuantityFormatting_RemovesOnlyUnnecessaryZeroes(
        decimal quantity,
        string expected)
        => Assert.Equal(expected, ReportFormatting.Quantity(quantity));

    [Fact]
    public void CurrentReopenedDraft_UsesPersistedSnapshotAndEffectiveAllocation()
    {
        var sale = new SaleTabViewModel(
            1, "20260826-0002", 1000m, DateTime.Now, 10, "Customer");
        sale.ApplyDetails(
            1000m,
            10,
            "Customer",
            "0101",
            "Sale comment",
            [new SaleItemViewModel(1, null, null, null, "Manual item", null, "ცალი", 1m, 1000m, 1000m, true, null)],
            1,
            700m,
            300m,
            true,
            new PreviousCompletionPaymentStateDto(1, 500m, 200m, 0m, 0m));

        var report = SaleReportModelFactory.FromCurrentSale(sale, DateTime.Now);

        Assert.Equal(1, report.Status);
        Assert.Equal(700m, report.PaidAmount);
        Assert.Equal(300m, report.OutstandingAmount);
        Assert.Equal(500m, report.CashAmount);
        Assert.Equal(200m, report.CardAmount);
        Assert.True(Assert.Single(report.Items).IsManual);
    }

    private static SaleDetailsDto CreateDetails(
        int status,
        int completionVersion,
        IReadOnlyList<SaleDetailsPaymentDto> payments)
        => new(
            1,
            "20260826-0001",
            status,
            completionVersion,
            10,
            "Customer",
            "0101",
            "Comment",
            1000m,
            700m,
            300m,
            true,
            DateTime.Now,
            DateTime.Now,
            null,
            [new SaleDetailsItemDto(1, 99, "OLD-CODE", "OLD-BARCODE", "Snapshot Cement M500", 1, "კგ", 2.5m, 18.12345m, 45.31m, false, "Snapshot comment")],
            payments);
}
