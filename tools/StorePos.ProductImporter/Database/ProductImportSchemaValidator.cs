namespace StorePos.ProductImporter.Database;

public sealed class ProductImportSchemaValidator
{
    public SchemaValidationResult Validate(DatabaseSchema schema)
    {
        var errors = new List<string>();
        RequireColumn(schema, errors, "Products", "Id", "bigint", 0, 0, false, identity: true);
        RequireColumn(schema, errors, "Products", "Code", "nvarchar", 50, 0, false);
        RequireColumn(schema, errors, "Products", "Barcode", "nvarchar", 100, 0, true);
        RequireColumn(schema, errors, "Products", "Name", "nvarchar", 300, 0, false);
        RequireColumn(schema, errors, "Products", "UnitId", "int", 0, 0, false);
        RequireColumn(schema, errors, "Products", "SupplierName", "nvarchar", 300, 0, true);
        RequireColumn(schema, errors, "Products", "SupplierCode", "nvarchar", 100, 0, true);
        RequireColumn(schema, errors, "Products", "CostPrice", "decimal", 18, 5, true);
        RequireColumn(schema, errors, "Products", "Price", "decimal", 18, 5, false);
        RequireColumn(schema, errors, "Products", "IsActive", "bit", 0, 0, false);
        RequireColumn(schema, errors, "Products", "DateCreated", "datetime2", 0, 0, false);
        RequireColumn(schema, errors, "Products", "DateUpdated", "datetime2", 0, 0, true);

        RequireColumn(schema, errors, "Units", "Id", "int", 0, 0, false, identity: true);
        RequireColumn(schema, errors, "Units", "Name", "nvarchar", 100, 0, false);
        RequireColumn(schema, errors, "Units", "ShortName", "nvarchar", 20, 0, true);

        RequireUniqueIndex(schema, errors, "Products", "Code", filterRequired: false);
        RequireUniqueIndex(schema, errors, "Products", "Barcode", filterRequired: true);

        if (!schema.ForeignKeys.Any(key =>
                key.ParentTable == "Products" &&
                key.ParentColumn == "UnitId" &&
                key.ReferencedTable == "Units" &&
                key.ReferencedColumn == "Id"))
        {
            errors.Add("Products.UnitId -> Units.Id foreign key is missing.");
        }

        if (!schema.CheckConstraints.Contains("CK_Products_Price_NonNegative"))
        {
            errors.Add("CK_Products_Price_NonNegative check constraint is missing.");
        }
        if (!schema.CheckConstraints.Contains("CK_Products_CostPrice_NonNegative"))
        {
            errors.Add("CK_Products_CostPrice_NonNegative check constraint is missing.");
        }

        return new SchemaValidationResult(errors);
    }

    private static void RequireColumn(
        DatabaseSchema schema,
        ICollection<string> errors,
        string table,
        string column,
        string type,
        int sizeOrPrecision,
        int scale,
        bool nullable,
        bool identity = false)
    {
        var actual = schema.Columns.SingleOrDefault(definition =>
            definition.TableName == table && definition.ColumnName == column);
        if (actual is null)
        {
            errors.Add($"dbo.{table}.{column} is missing.");
            return;
        }

        var compatible = string.Equals(actual.TypeName, type, StringComparison.OrdinalIgnoreCase) &&
                         actual.IsNullable == nullable &&
                         actual.IsIdentity == identity;
        compatible &= type switch
        {
            "nvarchar" => actual.MaxLength == sizeOrPrecision,
            "decimal" => actual.Precision == sizeOrPrecision && actual.Scale == scale,
            _ => true
        };

        if (!compatible)
        {
            errors.Add($"dbo.{table}.{column} has an incompatible SQL definition.");
        }
    }

    private static void RequireUniqueIndex(
        DatabaseSchema schema,
        ICollection<string> errors,
        string table,
        string column,
        bool filterRequired)
    {
        var index = schema.Indexes.FirstOrDefault(candidate =>
            candidate.TableName == table &&
            candidate.IsUnique &&
            candidate.Columns.Count == 1 &&
            candidate.Columns[0] == column);
        if (index is null || filterRequired && !IsNotNullFilter(index.FilterDefinition, column))
        {
            errors.Add($"Expected unique index for dbo.{table}.{column} is missing or incompatible.");
        }
    }

    private static bool IsNotNullFilter(string? filterDefinition, string column)
    {
        if (string.IsNullOrWhiteSpace(filterDefinition))
        {
            return false;
        }

        var normalized = filterDefinition
            .Replace(" ", string.Empty)
            .Replace("[", string.Empty)
            .Replace("]", string.Empty)
            .Replace("(", string.Empty)
            .Replace(")", string.Empty);

        return string.Equals(
            normalized,
            $"{column}ISNOTNULL",
            StringComparison.OrdinalIgnoreCase);
    }
}
