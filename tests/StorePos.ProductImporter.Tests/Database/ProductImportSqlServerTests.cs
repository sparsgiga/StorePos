using Microsoft.Data.SqlClient;
using StorePos.ProductImporter.Database;
using StorePos.ProductImporter.Models;

namespace StorePos.ProductImporter.Tests.Database;

public sealed class ProductImportSqlServerTests
{
    [ImporterSqlServerFact]
    public async Task Import_CommitsFlexibleCodeZeroPriceAndSupplierMetadataWithoutUpdatingExisting()
    {
        await using var database = await ImporterTestDatabase.CreateAsync();
        await database.ExecuteAsync("""
            INSERT dbo.Products
                (Code, Barcode, Name, UnitId, SupplierName, SupplierCode,
                 CostPrice, Price, IsActive, DateCreated, DateUpdated)
            VALUES
                (N'EXISTING', N'900', N'Original', 1, NULL, NULL,
                 NULL, 12.00000, 1, SYSUTCDATETIME(), NULL);
            """);

        var result = await new ProductImportDatabase(database.ConnectionString).ImportAsync(
        [
            Product("12.01კოდი", "000000000077", "Imported", 0m, "Supplier", "00077", 0.06m)
        ]);

        Assert.Equal(1, result.InsertedCount);
        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Code, Barcode, SupplierName, SupplierCode, CostPrice, Price
            FROM dbo.Products WHERE Code = N'12.01კოდი';
            SELECT Name, Price FROM dbo.Products WHERE Code = N'EXISTING';
            """;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("12.01კოდი", reader.GetString(0));
        Assert.Equal("000000000077", reader.GetString(1));
        Assert.Equal("Supplier", reader.GetString(2));
        Assert.Equal("00077", reader.GetString(3));
        Assert.Equal(0.06m, reader.GetDecimal(4));
        Assert.Equal(0m, reader.GetDecimal(5));
        Assert.True(await reader.NextResultAsync());
        Assert.True(await reader.ReadAsync());
        Assert.Equal("Original", reader.GetString(0));
        Assert.Equal(12m, reader.GetDecimal(1));
    }

    [ImporterSqlServerFact]
    public async Task Import_UnknownUnitRollsBackEveryCandidate()
    {
        await using var database = await ImporterTestDatabase.CreateAsync();
        var importer = new ProductImportDatabase(database.ConnectionString);

        var exception = await Assert.ThrowsAsync<ProductImportTransactionException>(() =>
            importer.ImportAsync(
            [
                Product("VALID", "101", "Valid", 1m),
                Product("BAD-UNIT", "102", "Invalid", 1m) with { UnitId = 999 }
            ]));

        Assert.True(exception.RollbackSucceeded);
        Assert.Equal(0, await database.ProductCountAsync());
    }

    [ImporterSqlServerFact]
    public async Task Import_RaceCodeConflictRollsBackOtherCandidates()
    {
        await using var database = await ImporterTestDatabase.CreateAsync();
        await database.ExecuteAsync("""
            INSERT dbo.Products
                (Code, Barcode, Name, UnitId, Price, IsActive, DateCreated)
            VALUES (N'RACE', N'800', N'Other transaction', 1, 1, 1, SYSUTCDATETIME());
            """);

        var exception = await Assert.ThrowsAsync<ProductImportTransactionException>(() =>
            new ProductImportDatabase(database.ConnectionString).ImportAsync(
            [
                Product("SAFE", "801", "Must roll back", 1m),
                Product("RACE", "802", "Conflicting", 1m)
            ]));

        Assert.True(exception.RollbackSucceeded);
        Assert.Equal(0, await database.CountByCodeAsync("SAFE"));
        Assert.Equal(1, await database.CountByCodeAsync("RACE"));
    }

    [ImporterSqlServerFact]
    public async Task Import_FailureAfterFinalInsertRollsBackAllRows()
    {
        await using var database = await ImporterTestDatabase.CreateAsync();
        await database.ExecuteAsync("""
            CREATE TRIGGER dbo.TR_Products_ImporterFailure
            ON dbo.Products AFTER INSERT AS
            BEGIN
                THROW 51010, 'Forced failure after final INSERT.', 1;
            END;
            """);

        var exception = await Assert.ThrowsAsync<ProductImportTransactionException>(() =>
            new ProductImportDatabase(database.ConnectionString).ImportAsync(
            [
                Product("ROLLBACK-1", "701", "One", 1m),
                Product("ROLLBACK-2", "702", "Two", 1m)
            ]));

        Assert.True(exception.RollbackSucceeded);
        Assert.Equal(0, await database.ProductCountAsync());
    }

    [ImporterSqlServerFact]
    public async Task Import_ExistingBarcodeConflictRollsBackAllCandidates()
    {
        await using var database = await ImporterTestDatabase.CreateAsync();
        await database.ExecuteAsync("""
            INSERT dbo.Products
                (Code, Barcode, Name, UnitId, Price, IsActive, DateCreated)
            VALUES (N'EXISTING', N'600', N'Existing', 1, 1, 1, SYSUTCDATETIME());
            """);

        await Assert.ThrowsAsync<ProductImportTransactionException>(() =>
            new ProductImportDatabase(database.ConnectionString).ImportAsync(
            [
                Product("SAFE", "601", "Safe", 1m),
                Product("BARCODE-CONFLICT", "600", "Conflict", 1m)
            ]));

        Assert.Equal(0, await database.CountByCodeAsync("SAFE"));
        Assert.Equal(1, await database.ProductCountAsync());
    }

    private static ImportProductRow Product(
        string code,
        string? barcode,
        string name,
        decimal price,
        string? supplierName = null,
        string? supplierCode = null,
        decimal? costPrice = null)
        => new(2, code, barcode, name, 1, supplierName, supplierCode, costPrice, price);
}

public sealed class ImporterSqlServerFactAttribute : FactAttribute
{
    public ImporterSqlServerFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("STOREPOS_RUN_SQLSERVER_TESTS"),
                "1",
                StringComparison.Ordinal))
        {
            Skip = "Set STOREPOS_RUN_SQLSERVER_TESTS=1 to run isolated LocalDB importer tests.";
        }
    }
}

internal sealed class ImporterTestDatabase : IAsyncDisposable
{
    private const string DefaultDataSource = "(localdb)\\MSSQLLocalDB";
    private const string Prefix = "StorePosImporterIntegration_";
    private bool _disposed;

    private ImporterTestDatabase(string databaseName)
    {
        DatabaseName = databaseName;
        ConnectionString = BuildConnectionString(databaseName);
    }

    public string DatabaseName { get; }

    public string ConnectionString { get; }

    private static string DataSource =>
        Environment.GetEnvironmentVariable("STOREPOS_IMPORTER_TEST_SERVER") ?? DefaultDataSource;

    public static async Task<ImporterTestDatabase> CreateAsync()
    {
        var database = new ImporterTestDatabase($"{Prefix}{Guid.NewGuid():N}");
        await using var master = new SqlConnection(BuildConnectionString("master"));
        await master.OpenAsync();
        await using var create = master.CreateCommand();
        create.CommandText = $"CREATE DATABASE [{database.DatabaseName}]";
        await create.ExecuteNonQueryAsync();
        await database.CreateSchemaAsync();
        return database;
    }

    public async Task ExecuteAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> ProductCountAsync()
        => await ScalarAsync("SELECT COUNT(*) FROM dbo.Products;");

    public async Task<int> CountByCodeAsync(string code)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM dbo.Products WHERE Code = @code;";
        command.Parameters.AddWithValue("@code", code);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ValidateDeleteTarget();
        await using var master = new SqlConnection(BuildConnectionString("master"));
        await master.OpenAsync();
        await using var drop = master.CreateCommand();
        drop.CommandText = $"""
            ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE [{DatabaseName}];
            """;
        await drop.ExecuteNonQueryAsync();
    }

    private async Task CreateSchemaAsync()
        => await ExecuteAsync("""
            CREATE TABLE dbo.Units
            (
                Id int NOT NULL PRIMARY KEY,
                Name nvarchar(100) NOT NULL,
                ShortName nvarchar(20) NULL
            );
            INSERT dbo.Units (Id, Name, ShortName) VALUES (1, N'ცალი', N'ც');

            CREATE TABLE dbo.Products
            (
                Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                Code nvarchar(50) NOT NULL,
                Barcode nvarchar(100) NULL,
                Name nvarchar(300) NOT NULL,
                UnitId int NOT NULL,
                SupplierName nvarchar(300) NULL,
                SupplierCode nvarchar(100) NULL,
                CostPrice decimal(18,5) NULL,
                Price decimal(18,5) NOT NULL,
                IsActive bit NOT NULL,
                DateCreated datetime2 NOT NULL,
                DateUpdated datetime2 NULL,
                CONSTRAINT FK_Products_Units_UnitId FOREIGN KEY (UnitId) REFERENCES dbo.Units(Id),
                CONSTRAINT CK_Products_Price_NonNegative CHECK (Price >= 0),
                CONSTRAINT CK_Products_CostPrice_NonNegative CHECK (CostPrice IS NULL OR CostPrice >= 0)
            );
            CREATE UNIQUE INDEX IX_Products_Code ON dbo.Products(Code);
            CREATE UNIQUE INDEX IX_Products_Barcode ON dbo.Products(Barcode) WHERE Barcode IS NOT NULL;
            """);

    private async Task<int> ScalarAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private void ValidateDeleteTarget()
    {
        var builder = new SqlConnectionStringBuilder(ConnectionString);
        var suffix = DatabaseName.StartsWith(Prefix, StringComparison.Ordinal)
            ? DatabaseName[Prefix.Length..]
            : string.Empty;
        if (!string.Equals(builder.DataSource, DataSource, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(builder.InitialCatalog, DatabaseName, StringComparison.Ordinal) ||
            !Guid.TryParseExact(suffix, "N", out _))
        {
            throw new InvalidOperationException("Refusing to delete a non-importer-test database.");
        }
    }

    private static string BuildConnectionString(string databaseName)
        => new SqlConnectionStringBuilder
        {
            DataSource = DataSource,
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            ConnectTimeout = 5
        }.ConnectionString;
}
