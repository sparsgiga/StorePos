using ClosedXML.Excel;
using StorePos.ProductImporter.Models;

namespace StorePos.ProductImporter.Parsing;

public sealed class ProductWorkbookParser
{
    public const string CodeHeader = "კოდი";
    public const string NameHeader = "დასახელება";
    public const string UnitHeader = "საზომი ერთეული";
    public const string BarcodeHeader = "შტრიხკოდი";
    public const string SupplierNameHeader = "გამყიდველი";
    public const string SupplierCodeHeader = "მომწოდ კოდი";
    public const string CostPriceHeader = "პირველადი ფასი";
    public const string PriceHeader = "საცალო";

    private static readonly string[] RequiredHeaders =
    [
        CodeHeader,
        NameHeader,
        UnitHeader,
        BarcodeHeader,
        SupplierNameHeader,
        SupplierCodeHeader,
        CostPriceHeader,
        PriceHeader
    ];

    public WorkbookParseResult Parse(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException("Excel workbook does not contain a worksheet.");
        var firstRow = worksheet.FirstRowUsed()
            ?? throw new InvalidDataException("Excel worksheet is empty.");
        var headers = firstRow.CellsUsed()
            .Select(cell => (Name: Normalize(cell.GetString()), Column: cell.Address.ColumnNumber))
            .Where(header => header.Name.Length > 0)
            .GroupBy(header => header.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Column, StringComparer.OrdinalIgnoreCase);

        var missingHeaders = RequiredHeaders.Where(header => !headers.ContainsKey(Normalize(header))).ToArray();
        if (missingHeaders.Length > 0)
        {
            throw new InvalidDataException(
                $"Required Excel headers are missing: {string.Join(", ", missingHeaders)}.");
        }

        var rows = new List<ParsedProductRow>();
        var issues = new List<ImportIssue>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? firstRow.RowNumber();
        var sourceCount = 0;

        for (var rowNumber = firstRow.RowNumber() + 1; rowNumber <= lastRow; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            if (row.IsEmpty())
            {
                continue;
            }

            sourceCount++;
            var code = ReadIdentifier(row, headers, CodeHeader);
            var barcode = NullIfBlank(ReadIdentifier(row, headers, BarcodeHeader));
            var name = ReadText(row, headers, NameHeader);
            var unit = ReadText(row, headers, UnitHeader);
            var supplierName = NullIfBlank(ReadText(row, headers, SupplierNameHeader));
            var supplierCode = NullIfBlank(ReadIdentifier(row, headers, SupplierCodeHeader));

            ValidateRequired(issues, rowNumber, code, name, "Code", code, 50);
            ValidateRequired(issues, rowNumber, code, name, "Name", name, 300);
            ValidateRequired(issues, rowNumber, code, name, "MeasurementUnit", unit, 100);
            ValidateLength(issues, rowNumber, code, name, "Barcode", barcode, 100);
            ValidateLength(issues, rowNumber, code, name, "SupplierName", supplierName, 300);
            ValidateLength(issues, rowNumber, code, name, "SupplierCode", supplierCode, 100);
            ValidateIdentifierRepresentation(issues, rowNumber, code, name, "Code", code);
            ValidateIdentifierRepresentation(issues, rowNumber, code, name, "Barcode", barcode);
            ValidateIdentifierRepresentation(issues, rowNumber, code, name, "SupplierCode", supplierCode);

            var costCell = row.Cell(headers[Normalize(CostPriceHeader)]);
            var priceCell = row.Cell(headers[Normalize(PriceHeader)]);
            var costValid = ImportValueParser.TryReadDecimal(
                costCell,
                allowBlank: true,
                out var costPrice,
                out var costError);
            var priceValid = ImportValueParser.TryReadDecimal(
                priceCell,
                allowBlank: false,
                out var price,
                out var priceError);

            if (!costValid)
            {
                AddError(issues, rowNumber, code, name, "CostPrice", costCell.GetString(), costError!);
            }
            if (!priceValid)
            {
                AddError(issues, rowNumber, code, name, "Price", priceCell.GetString(), priceError!);
            }

            if (issues.Any(issue => issue.ExcelRow == rowNumber && issue.IsBlocking))
            {
                continue;
            }

            rows.Add(new ParsedProductRow(
                rowNumber,
                code,
                barcode,
                name.Trim(),
                unit.Trim(),
                supplierName?.Trim(),
                supplierCode,
                costPrice,
                price!.Value));
        }

        return new WorkbookParseResult(sourceCount, rows, issues);
    }

    private static string ReadIdentifier(IXLRow row, IReadOnlyDictionary<string, int> headers, string header)
        => ImportValueParser.ReadIdentifier(row.Cell(headers[Normalize(header)]));

    private static string ReadText(IXLRow row, IReadOnlyDictionary<string, int> headers, string header)
        => row.Cell(headers[Normalize(header)]).GetString().Trim();

    private static string Normalize(string value)
        => string.Join(' ', value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string? NullIfBlank(string value) => value.Length == 0 ? null : value;

    private static void ValidateRequired(
        ICollection<ImportIssue> issues,
        int row,
        string? code,
        string? name,
        string field,
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddError(issues, row, code, name, field, value, "Value is required.");
            return;
        }

        ValidateLength(issues, row, code, name, field, value, maxLength);
    }

    private static void ValidateLength(
        ICollection<ImportIssue> issues,
        int row,
        string? code,
        string? name,
        string field,
        string? value,
        int maxLength)
    {
        if (value?.Length > maxLength)
        {
            AddError(issues, row, code, name, field, value, $"Value exceeds nvarchar({maxLength}).");
        }
    }

    private static void ValidateIdentifierRepresentation(
        ICollection<ImportIssue> issues,
        int row,
        string? code,
        string? name,
        string field,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            (value.Contains('E', StringComparison.OrdinalIgnoreCase) &&
             decimal.TryParse(value, System.Globalization.NumberStyles.Float,
                 System.Globalization.CultureInfo.InvariantCulture, out _)))
        {
            AddError(
                issues,
                row,
                code,
                name,
                field,
                value,
                "Identifier is displayed in scientific notation. Format the Excel cell as text.");
        }
    }

    private static void AddError(
        ICollection<ImportIssue> issues,
        int row,
        string? code,
        string? name,
        string field,
        string? value,
        string message)
        => issues.Add(new ImportIssue(
            row,
            code,
            name,
            ImportIssueSeverity.Error,
            field,
            value,
            message));
}
