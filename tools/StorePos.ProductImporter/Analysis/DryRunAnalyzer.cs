using StorePos.ProductImporter.Models;

namespace StorePos.ProductImporter.Analysis;

public sealed class DryRunAnalyzer
{
    private static readonly StringComparer IdentifierComparer = StringComparer.OrdinalIgnoreCase;

    public DryRunResult Analyze(
        WorkbookParseResult workbook,
        DatabaseReferenceData database)
    {
        var issues = workbook.Issues.ToList();
        var resolved = ResolveUnits(workbook.Rows, database.Units, issues);
        var duplicateIdenticalCount = AnalyzeSourceDuplicates(resolved, issues, out var uniqueRows);
        AnalyzeSourceBarcodes(uniqueRows, issues);

        var existingCodes = database.Products
            .Select(product => product.Code)
            .ToHashSet(IdentifierComparer);
        var existingBarcodes = database.Products
            .Where(product => !string.IsNullOrWhiteSpace(product.Barcode))
            .ToDictionary(product => product.Barcode!, product => product.Code, IdentifierComparer);
        var newProducts = new List<ImportProductRow>();
        var existingCount = 0;

        foreach (var row in uniqueRows)
        {
            if (existingCodes.Contains(row.Code))
            {
                existingCount++;
                continue;
            }

            if (row.Barcode is not null && existingBarcodes.TryGetValue(row.Barcode, out var ownerCode))
            {
                issues.Add(new ImportIssue(
                    row.SourceRowNumber,
                    row.Code,
                    row.Name,
                    ImportIssueSeverity.Conflict,
                    "Barcode",
                    row.Barcode,
                    $"Barcode already belongs to database Product Code '{ownerCode}'."));
                continue;
            }

            newProducts.Add(row);
        }

        foreach (var row in workbook.Rows.Where(row => row.Price == 0))
        {
            issues.Add(new ImportIssue(
                row.SourceRowNumber,
                row.Code,
                row.Name,
                ImportIssueSeverity.Information,
                "Price",
                "0",
                "Zero retail price is valid catalog data but the Product cannot be sold until priced."));
        }

        foreach (var row in workbook.Rows.Where(row => row.Barcode is null))
        {
            issues.Add(new ImportIssue(
                row.SourceRowNumber,
                row.Code,
                row.Name,
                ImportIssueSeverity.Warning,
                "Barcode",
                null,
                "Barcode is missing; NULL will be imported."));
        }

        return new DryRunResult(
            workbook.SourceRowCount,
            newProducts,
            existingCount,
            duplicateIdenticalCount,
            workbook.Rows.Count(row => row.Price == 0),
            workbook.Rows.Count(row => row.Barcode is null),
            issues);
    }

    private static IReadOnlyList<ImportProductRow> ResolveUnits(
        IReadOnlyList<ParsedProductRow> rows,
        IReadOnlyList<MeasurementUnitRecord> units,
        ICollection<ImportIssue> issues)
    {
        var byShortName = BuildUnitLookup(units, unit => unit.ShortName);
        var byName = BuildUnitLookup(units, unit => unit.Name);
        var result = new List<ImportProductRow>();

        foreach (var row in rows)
        {
            var key = NormalizeUnit(row.MeasurementUnit);
            MeasurementUnitRecord? unit = null;
            var ambiguous = false;

            if (byShortName.TryGetValue(key, out var shortMatches))
            {
                ambiguous = shortMatches.Count != 1;
                unit = shortMatches.Count == 1 ? shortMatches[0] : null;
            }
            else if (byName.TryGetValue(key, out var nameMatches))
            {
                ambiguous = nameMatches.Count != 1;
                unit = nameMatches.Count == 1 ? nameMatches[0] : null;
            }

            if (unit is null)
            {
                issues.Add(new ImportIssue(
                    row.SourceRowNumber,
                    row.Code,
                    row.Name,
                    ImportIssueSeverity.Error,
                    "MeasurementUnit",
                    row.MeasurementUnit,
                    ambiguous
                        ? "Measurement unit matches more than one database Unit."
                        : "Unknown measurement unit."));
                continue;
            }

            result.Add(new ImportProductRow(
                row.SourceRowNumber,
                row.Code,
                row.Barcode,
                row.Name,
                unit.Id,
                row.SupplierName,
                row.SupplierCode,
                row.CostPrice,
                row.Price));
        }

        return result;
    }

    private static Dictionary<string, List<MeasurementUnitRecord>> BuildUnitLookup(
        IEnumerable<MeasurementUnitRecord> units,
        Func<MeasurementUnitRecord, string?> selector)
        => units
            .Where(unit => !string.IsNullOrWhiteSpace(selector(unit)))
            .GroupBy(unit => NormalizeUnit(selector(unit)!), IdentifierComparer)
            .ToDictionary(group => group.Key, group => group.ToList(), IdentifierComparer);

    private static int AnalyzeSourceDuplicates(
        IReadOnlyList<ImportProductRow> rows,
        ICollection<ImportIssue> issues,
        out IReadOnlyList<ImportProductRow> uniqueRows)
    {
        var result = new List<ImportProductRow>();
        var identicalCount = 0;

        foreach (var group in rows.GroupBy(row => row.Code, IdentifierComparer))
        {
            var first = group.First();
            var duplicates = group.Skip(1).ToArray();
            if (duplicates.Length == 0)
            {
                result.Add(first);
                continue;
            }

            if (duplicates.All(row => LogicallyEqual(first, row)))
            {
                identicalCount += duplicates.Length;
                result.Add(first);
                foreach (var duplicate in duplicates)
                {
                    issues.Add(new ImportIssue(
                        duplicate.SourceRowNumber,
                        duplicate.Code,
                        duplicate.Name,
                        ImportIssueSeverity.Warning,
                        "Code",
                        duplicate.Code,
                        "Identical duplicate source row; only the first row is a candidate."));
                }
                continue;
            }

            foreach (var row in group)
            {
                issues.Add(new ImportIssue(
                    row.SourceRowNumber,
                    row.Code,
                    row.Name,
                    ImportIssueSeverity.Conflict,
                    "Code",
                    row.Code,
                    "Duplicate Code has conflicting source data."));
            }
        }

        uniqueRows = result;
        return identicalCount;
    }

    private static void AnalyzeSourceBarcodes(
        IReadOnlyList<ImportProductRow> rows,
        ICollection<ImportIssue> issues)
    {
        foreach (var group in rows
                     .Where(row => row.Barcode is not null)
                     .GroupBy(row => row.Barcode!, IdentifierComparer)
                     .Where(group => group.Select(row => row.Code).Distinct(IdentifierComparer).Count() > 1))
        {
            foreach (var row in group)
            {
                issues.Add(new ImportIssue(
                    row.SourceRowNumber,
                    row.Code,
                    row.Name,
                    ImportIssueSeverity.Conflict,
                    "Barcode",
                    row.Barcode,
                    "Barcode is used by different source Products."));
            }
        }
    }

    private static bool LogicallyEqual(ImportProductRow left, ImportProductRow right)
        => IdentifierComparer.Equals(left.Code, right.Code) &&
           IdentifierComparer.Equals(left.Barcode, right.Barcode) &&
           string.Equals(left.Name, right.Name, StringComparison.Ordinal) &&
           left.UnitId == right.UnitId &&
           string.Equals(left.SupplierName, right.SupplierName, StringComparison.Ordinal) &&
           IdentifierComparer.Equals(left.SupplierCode, right.SupplierCode) &&
           left.CostPrice == right.CostPrice &&
           left.Price == right.Price;

    private static string NormalizeUnit(string value)
        => string.Join(' ', value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
