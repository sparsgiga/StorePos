using ClosedXML.Excel;
using StorePos.ProductImporter.Parsing;

namespace StorePos.ProductImporter.Tests.Parsing;

public sealed class ProductWorkbookParserTests
{
    [Fact]
    public void Parse_MapsGeorgianHeadersByNameAndPreservesIdentifiers()
    {
        using var file = WorkbookFile.Create(worksheet =>
        {
            var headers = new[]
            {
                ProductWorkbookParser.PriceHeader,
                ProductWorkbookParser.SupplierCodeHeader,
                ProductWorkbookParser.NameHeader,
                ProductWorkbookParser.CodeHeader,
                ProductWorkbookParser.UnitHeader,
                ProductWorkbookParser.BarcodeHeader,
                ProductWorkbookParser.SupplierNameHeader,
                ProductWorkbookParser.CostPriceHeader
            };
            for (var index = 0; index < headers.Length; index++)
            {
                worksheet.Cell(1, index + 1).Value = headers[index];
            }

            worksheet.Cell(2, 1).Value = "7,5";
            worksheet.Cell(2, 2).Value = "00077";
            worksheet.Cell(2, 3).Value = "პროდუქტი";
            worksheet.Cell(2, 4).Value = "12.01კოდი";
            worksheet.Cell(2, 5).Value = "ც";
            worksheet.Cell(2, 6).Value = "0012345678901";
            worksheet.Cell(2, 7).Value = "გამყიდველი";
            worksheet.Cell(2, 8).Value = "0,06";
        });

        var result = new ProductWorkbookParser().Parse(file.Path);

        var row = Assert.Single(result.Rows);
        Assert.Equal("12.01კოდი", row.Code);
        Assert.Equal("0012345678901", row.Barcode);
        Assert.Equal("00077", row.SupplierCode);
        Assert.Equal(7.5m, row.Price);
        Assert.Equal(0.06m, row.CostPrice);
        Assert.Empty(result.Issues.Where(issue => issue.IsBlocking));
    }

    [Fact]
    public void Parse_AllowsZeroPriceAndBlankCostPrice()
    {
        using var file = WorkbookFile.CreateStandard("GMTEK-40012", "123", "0", null);

        var row = Assert.Single(new ProductWorkbookParser().Parse(file.Path).Rows);

        Assert.Equal(0m, row.Price);
        Assert.Null(row.CostPrice);
    }

    [Fact]
    public void Parse_MissingRequiredHeaderStopsParsing()
    {
        using var file = WorkbookFile.Create(worksheet =>
        {
            worksheet.Cell("A1").Value = ProductWorkbookParser.CodeHeader;
            worksheet.Cell("A2").Value = "P1";
        });

        var error = Assert.Throws<InvalidDataException>(
            () => new ProductWorkbookParser().Parse(file.Path));

        Assert.Contains(ProductWorkbookParser.NameHeader, error.Message);
    }

    [Theory]
    [InlineData("-1", null)]
    [InlineData("abc", null)]
    [InlineData("1", "-0,01")]
    public void Parse_InvalidPricesProduceBlockingIssue(string price, string? costPrice)
    {
        using var file = WorkbookFile.CreateStandard("P1", "123", price, costPrice);

        var result = new ProductWorkbookParser().Parse(file.Path);

        Assert.Empty(result.Rows);
        Assert.Contains(result.Issues, issue => issue.IsBlocking);
    }

    private sealed class WorkbookFile : IDisposable
    {
        private WorkbookFile(string path) => Path = path;
        public string Path { get; }

        public static WorkbookFile Create(Action<IXLWorksheet> populate)
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
            using var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("Products");
            populate(worksheet);
            workbook.SaveAs(path);
            return new WorkbookFile(path);
        }

        public static WorkbookFile CreateStandard(
            string code,
            string? barcode,
            string price,
            string? costPrice)
            => Create(worksheet =>
            {
                var headers = new[]
                {
                    ProductWorkbookParser.CodeHeader,
                    ProductWorkbookParser.NameHeader,
                    ProductWorkbookParser.UnitHeader,
                    ProductWorkbookParser.BarcodeHeader,
                    ProductWorkbookParser.SupplierNameHeader,
                    ProductWorkbookParser.SupplierCodeHeader,
                    ProductWorkbookParser.CostPriceHeader,
                    ProductWorkbookParser.PriceHeader
                };
                for (var index = 0; index < headers.Length; index++)
                {
                    worksheet.Cell(1, index + 1).Value = headers[index];
                }
                worksheet.Cell("A2").Value = code;
                worksheet.Cell("B2").Value = "Product";
                worksheet.Cell("C2").Value = "ც";
                worksheet.Cell("D2").Value = barcode;
                worksheet.Cell("E2").Value = "Supplier";
                worksheet.Cell("F2").Value = "00077";
                worksheet.Cell("G2").Value = costPrice;
                worksheet.Cell("H2").Value = price;
            });

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
