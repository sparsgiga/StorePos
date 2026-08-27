namespace StorePos.ProductImporter.Models;

public sealed record ParsedProductRow(
    int SourceRowNumber,
    string Code,
    string? Barcode,
    string Name,
    string MeasurementUnit,
    string? SupplierName,
    string? SupplierCode,
    decimal? CostPrice,
    decimal Price);

public sealed record ImportProductRow(
    int SourceRowNumber,
    string Code,
    string? Barcode,
    string Name,
    int UnitId,
    string? SupplierName,
    string? SupplierCode,
    decimal? CostPrice,
    decimal Price);

public enum ImportIssueSeverity
{
    Information,
    Warning,
    Conflict,
    Error
}

public sealed record ImportIssue(
    int? ExcelRow,
    string? Code,
    string? ProductName,
    ImportIssueSeverity Severity,
    string Field,
    string? Value,
    string Message)
{
    public bool IsBlocking => Severity is ImportIssueSeverity.Conflict or ImportIssueSeverity.Error;
}

public sealed record WorkbookParseResult(
    int SourceRowCount,
    IReadOnlyList<ParsedProductRow> Rows,
    IReadOnlyList<ImportIssue> Issues);

public sealed record DryRunResult(
    int SourceRowCount,
    IReadOnlyList<ImportProductRow> NewProducts,
    int ExistingCount,
    int DuplicateIdenticalCount,
    int ZeroPriceCount,
    int MissingBarcodeCount,
    IReadOnlyList<ImportIssue> Issues)
{
    public bool HasBlockingIssues => Issues.Any(issue => issue.IsBlocking);
    public int WarningCount => Issues.Count(issue =>
        issue.Severity is ImportIssueSeverity.Warning or ImportIssueSeverity.Information);
    public int ConflictCount => Issues.Count(issue => issue.Severity == ImportIssueSeverity.Conflict);
    public int ErrorCount => Issues.Count(issue => issue.Severity == ImportIssueSeverity.Error);
}

public sealed record MeasurementUnitRecord(int Id, string Name, string? ShortName);

public sealed record ExistingProductRecord(string Code, string? Barcode);

public sealed record DatabaseReferenceData(
    IReadOnlyList<MeasurementUnitRecord> Units,
    IReadOnlyList<ExistingProductRecord> Products);

public sealed record ImportExecutionResult(int InsertedCount, TimeSpan Duration);
