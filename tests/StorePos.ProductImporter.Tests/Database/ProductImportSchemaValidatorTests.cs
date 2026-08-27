using StorePos.ProductImporter.Database;

namespace StorePos.ProductImporter.Tests.Database;

public sealed class ProductImportSchemaValidatorTests
{
    [Fact]
    public void CompatibleSchemaPasses()
        => Assert.True(new ProductImportSchemaValidator().Validate(CreateSchema()).IsCompatible);

    [Fact]
    public void CompatibleSchema_AllowsSqlServerParenthesizedBarcodeFilter()
    {
        var schema = CreateSchema();
        schema = schema with
        {
            Indexes = schema.Indexes.Select(index => index.Columns.Contains("Barcode")
                ? index with { FilterDefinition = "([Barcode] IS NOT NULL)" }
                : index).ToArray()
        };

        Assert.True(new ProductImportSchemaValidator().Validate(schema).IsCompatible);
    }

    [Theory]
    [InlineData("SupplierName")]
    [InlineData("Units.ShortName")]
    public void MissingRequiredColumnFails(string target)
    {
        var schema = CreateSchema();
        var parts = target.Split('.');
        var table = parts.Length == 2 ? parts[0] : "Products";
        var column = parts[^1];
        schema = schema with
        {
            Columns = schema.Columns.Where(item =>
                item.TableName != table || item.ColumnName != column).ToArray()
        };

        Assert.False(new ProductImportSchemaValidator().Validate(schema).IsCompatible);
    }

    [Fact]
    public void IncompatiblePricePrecisionFails()
    {
        var schema = CreateSchema();
        schema = schema with
        {
            Columns = schema.Columns.Select(item => item.ColumnName == "Price"
                ? item with { Precision = 12 }
                : item).ToArray()
        };

        Assert.False(new ProductImportSchemaValidator().Validate(schema).IsCompatible);
    }

    [Fact]
    public void IncompatibleCodeLengthFails()
    {
        var schema = CreateSchema();
        schema = schema with
        {
            Columns = schema.Columns.Select(item => item.ColumnName == "Code"
                ? item with { MaxLength = 40 }
                : item).ToArray()
        };

        Assert.False(new ProductImportSchemaValidator().Validate(schema).IsCompatible);
    }

    private static DatabaseSchema CreateSchema()
    {
        static SqlColumnDefinition Column(
            string table,
            string name,
            string type,
            int length = 0,
            byte precision = 0,
            byte scale = 0,
            bool nullable = false,
            bool identity = false)
            => new(table, name, type, length, precision, scale, nullable, identity);

        var columns = new[]
        {
            Column("Products", "Id", "bigint", identity: true),
            Column("Products", "Code", "nvarchar", 50),
            Column("Products", "Barcode", "nvarchar", 100, nullable: true),
            Column("Products", "Name", "nvarchar", 300),
            Column("Products", "UnitId", "int"),
            Column("Products", "SupplierName", "nvarchar", 300, nullable: true),
            Column("Products", "SupplierCode", "nvarchar", 100, nullable: true),
            Column("Products", "CostPrice", "decimal", precision: 18, scale: 5, nullable: true),
            Column("Products", "Price", "decimal", precision: 18, scale: 5),
            Column("Products", "IsActive", "bit"),
            Column("Products", "DateCreated", "datetime2"),
            Column("Products", "DateUpdated", "datetime2", nullable: true),
            Column("Units", "Id", "int", identity: true),
            Column("Units", "Name", "nvarchar", 100),
            Column("Units", "ShortName", "nvarchar", 20, nullable: true)
        };
        var indexes = new[]
        {
            new SqlIndexDefinition("Products", "IX_Code", true, null, ["Code"]),
            new SqlIndexDefinition("Products", "IX_Barcode", true, "[Barcode] IS NOT NULL", ["Barcode"])
        };
        var keys = new[]
        {
            new SqlForeignKeyDefinition("Products", "UnitId", "Units", "Id")
        };
        return new DatabaseSchema(
            columns,
            indexes,
            keys,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CK_Products_Price_NonNegative",
                "CK_Products_CostPrice_NonNegative"
            });
    }
}
