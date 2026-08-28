using System.Windows;
using StorePos.Desktop.Reporting.Models;
using StorePos.Desktop.Reporting.Printing;

namespace StorePos.Desktop.Tests.Reporting;

public sealed class SaleDocumentFactoryTests
{
    [Fact]
    public void LargeFullSaleAndLoadingList_CreateMultiplePages()
    {
        var report = CreateReport(100);

        var result = RunOnSta(() =>
        {
            var factory = new SaleDocumentFactory();
            var full = factory.CreateFullSale(report);
            var loading = factory.CreateLoadingList(new LoadingListReportModel(
                report.SaleId,
                report.SaleNumber,
                report.Status,
                report.CustomerName,
                report.PrintedAt,
                "ჯერ ეს ნივთები დატვირთეთ",
                report.Items.Select(item => new LoadingListReportItemModel(
                    item.SaleItemId,
                    item.ProductCode,
                    item.Barcode,
                    item.ProductName,
                    item.MeasurementUnitName,
                    item.Quantity,
                    item.IsManual,
                    item.Comment)).ToArray()));
            return (FullPages: full.Pages.Count, LoadingPages: loading.Pages.Count);
        });

        Assert.True(result.FullPages > 1);
        Assert.True(result.LoadingPages > 1);
    }

    private static FullSaleReportModel CreateReport(int count)
        => new(
            1,
            "20260826-0099",
            2,
            "Customer",
            "0101",
            "Comment",
            DateTime.Now,
            DateTime.Now,
            null,
            DateTime.Now,
            count * 10m,
            0m,
            count * 10m,
            count * 10m,
            0m,
            count * 10m,
            0m,
            0m,
            0m,
            Enumerable.Range(1, count).Select(index => new FullSaleReportItemModel(
                index,
                $"P{index}",
                $"B{index}",
                $"მილი პოლიპროპილენის PN20 25მმ თეთრი — პროდუქტი {index}",
                "ცალი",
                1m,
                10m,
                10m,
                false,
                index % 3 == 0 ? "გრძელი კომენტარი, რომელიც შემდეგ ხაზზე უნდა გადავიდეს" : null))
                .ToArray());

    private static T RunOnSta<T>(Func<T> action)
    {
        T? result = default;
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception caught)
            {
                exception = caught;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (exception is not null)
        {
            throw exception;
        }
        return result!;
    }
}
