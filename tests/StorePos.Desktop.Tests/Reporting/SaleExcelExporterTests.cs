using System.IO.Compression;
using System.Text;
using StorePos.Desktop.Reporting.Excel;
using StorePos.Desktop.Reporting.Models;

namespace StorePos.Desktop.Tests.Reporting;

public sealed class SaleExcelExporterTests
{
    [Fact]
    public void CreateWorkbook_ProducesValidXlsxPackageWithSnapshotAndCurrentTotals()
    {
        var report = CreateReport();

        var bytes = new SaleExcelExporter().CreateWorkbook(report);

        Assert.True(bytes.Length > 0);
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.NotNull(archive.GetEntry("[Content_Types].xml"));
        Assert.NotNull(archive.GetEntry("xl/workbook.xml"));
        Assert.NotNull(archive.GetEntry("xl/styles.xml"));
        var sheet = ReadEntry(archive, "xl/worksheets/sheet1.xml");
        Assert.Contains(report.SaleNumber, sheet);
        Assert.Contains("Snapshot Product", sheet);
        Assert.Contains("18.12345", sheet);
        Assert.Contains("1000", sheet);
        Assert.Contains("700", sheet);
        Assert.Contains("300", sheet);
        Assert.Equal(1, CountOccurrences(sheet, "Snapshot Product"));
    }

    [Fact]
    public void DefaultFileName_IsSaleBasedAndWindowsSafe()
    {
        var fileName = ReportFileName.ForSale("20260826/0015:Draft");

        Assert.StartsWith("Sale_", fileName);
        Assert.EndsWith(".xlsx", fileName);
        Assert.DoesNotContain('/', fileName);
        Assert.DoesNotContain(':', fileName);
    }

    private static FullSaleReportModel CreateReport()
        => new(
            1,
            "20260826-0015",
            2,
            "გიორგი",
            "0101",
            "Comment",
            DateTime.Now,
            DateTime.Now,
            null,
            DateTime.Now,
            1000m,
            700m,
            300m,
            500m,
            200m,
            0m,
            0m,
            [new FullSaleReportItemModel(1, "P1", "123", "Snapshot Product", "ცალი", 2.5m, 18.12345m, 45.31m, false, "Long comment")]);

    private static string ReadEntry(ZipArchive archive, string path)
    {
        using var reader = new StreamReader(
            archive.GetEntry(path)!.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static int CountOccurrences(string value, string search)
        => (value.Length - value.Replace(search, string.Empty).Length) / search.Length;
}
