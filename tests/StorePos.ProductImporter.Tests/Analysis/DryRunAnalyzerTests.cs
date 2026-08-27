using StorePos.ProductImporter.Analysis;
using StorePos.ProductImporter.Models;

namespace StorePos.ProductImporter.Tests.Analysis;

public sealed class DryRunAnalyzerTests
{
    private static readonly DatabaseReferenceData Database = new(
        [new MeasurementUnitRecord(1, "ცალი", "ც")],
        [new ExistingProductRecord("EXISTING", "900")]);

    [Fact]
    public void Analyze_ClassifiesNewExistingZeroPriceAndMissingBarcode()
    {
        var workbook = Workbook(
            Row(2, "NEW", null, price: 0),
            Row(3, "EXISTING", "900", price: 10));

        var result = new DryRunAnalyzer().Analyze(workbook, Database);

        Assert.Single(result.NewProducts);
        Assert.Equal(1, result.ExistingCount);
        Assert.Equal(1, result.ZeroPriceCount);
        Assert.Equal(1, result.MissingBarcodeCount);
        Assert.False(result.HasBlockingIssues);
    }

    [Fact]
    public void Analyze_IdenticalDuplicateKeepsOneCandidate()
    {
        var workbook = Workbook(Row(2, "P1", "101"), Row(3, "P1", "101"));

        var result = new DryRunAnalyzer().Analyze(workbook, new DatabaseReferenceData(Database.Units, []));

        Assert.Single(result.NewProducts);
        Assert.Equal(1, result.DuplicateIdenticalCount);
        Assert.False(result.HasBlockingIssues);
    }

    [Fact]
    public void Analyze_ConflictingCodeAndBarcodeBlockImport()
    {
        var workbook = Workbook(
            Row(2, "P1", "101", name: "One"),
            Row(3, "P1", "101", name: "Different"),
            Row(4, "P2", "202"),
            Row(5, "P3", "202"));

        var result = new DryRunAnalyzer().Analyze(workbook, new DatabaseReferenceData(Database.Units, []));

        Assert.True(result.HasBlockingIssues);
        Assert.Contains(result.Issues, issue => issue.Field == "Code" && issue.IsBlocking);
        Assert.Contains(result.Issues, issue => issue.Field == "Barcode" && issue.IsBlocking);
    }

    [Fact]
    public void Analyze_NewCodeWithExistingBarcodeBlocksImport()
    {
        var result = new DryRunAnalyzer().Analyze(Workbook(Row(2, "NEW", "900")), Database);

        Assert.True(result.HasBlockingIssues);
        Assert.Empty(result.NewProducts);
    }

    [Fact]
    public void Analyze_UnknownUnitIsBlocking()
    {
        var row = Row(2, "P1", "101") with { MeasurementUnit = "უცნობი" };

        var result = new DryRunAnalyzer().Analyze(Workbook(row), Database);

        Assert.True(result.HasBlockingIssues);
        Assert.Empty(result.NewProducts);
    }

    private static WorkbookParseResult Workbook(params ParsedProductRow[] rows)
        => new(rows.Length, rows, []);

    private static ParsedProductRow Row(
        int row,
        string code,
        string? barcode,
        decimal price = 10,
        string name = "Product")
        => new(row, code, barcode, name, "ც", "Supplier", "00077", null, price);
}
