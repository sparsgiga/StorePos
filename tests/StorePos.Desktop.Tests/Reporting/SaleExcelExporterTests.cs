using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
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

        var document = XDocument.Parse(sheet);
        XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = document.Descendants(spreadsheet + "row")
            .ToDictionary(row => (string)row.Attribute("r")!);

        Assert.Equal("კოდი", CellText(rows["1"], spreadsheet, "A1"));
        Assert.Equal("პროდუქტი", CellText(rows["1"], spreadsheet, "C1"));
        Assert.Equal("P1", CellText(rows["2"], spreadsheet, "A2"));
        Assert.Equal("123", CellText(rows["2"], spreadsheet, "B2"));
        Assert.Equal("Snapshot Product", CellText(rows["2"], spreadsheet, "C2"));
        Assert.Equal("Long comment", CellText(rows["2"], spreadsheet, "H2"));
        Assert.DoesNotContain("3", rows.Keys);
        Assert.Equal("გაყიდვა", CellText(rows["4"], spreadsheet, "A4"));
        Assert.Equal(report.SaleNumber, CellText(rows["4"], spreadsheet, "B4"));
        Assert.Equal("მყიდველი", CellText(rows["5"], spreadsheet, "A5"));
        Assert.Equal(report.CustomerName, CellText(rows["5"], spreadsheet, "B5"));
        Assert.Contains("პროდუქტების ჯამი", sheet);
        Assert.Contains("ფასდაკლება", sheet);
        Assert.Contains("გადასახდელი", sheet);

        var pane = Assert.Single(document.Descendants(spreadsheet + "pane"));
        Assert.Equal("1", (string?)pane.Attribute("ySplit"));
        Assert.Equal("A2", (string?)pane.Attribute("topLeftCell"));
        var autoFilter = Assert.Single(document.Descendants(spreadsheet + "autoFilter"));
        Assert.Equal("A1:H2", (string?)autoFilter.Attribute("ref"));
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
            1001m,
            1m,
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

    private static string? CellText(XElement row, XNamespace spreadsheet, string reference)
        => row.Elements(spreadsheet + "c")
            .Single(cell => (string?)cell.Attribute("r") == reference)
            .Descendants(spreadsheet + "t")
            .SingleOrDefault()
            ?.Value;
}
