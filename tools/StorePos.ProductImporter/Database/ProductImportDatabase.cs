using System.Data;
using Microsoft.Data.SqlClient;
using StorePos.ProductImporter.Models;

namespace StorePos.ProductImporter.Database;

public sealed class ProductImportDatabase(string connectionString)
{
    private const string StagingTableName = "#ProductImportStaging";

    public string ConnectionString { get; } = connectionString;

    public async Task<DatabaseSchema> LoadSchemaAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT t.name, c.name, ty.name,
                   CASE WHEN ty.name IN (N'nvarchar', N'nchar') AND c.max_length > 0
                        THEN c.max_length / 2 ELSE c.max_length END,
                   c.precision, c.scale, c.is_nullable, c.is_identity
            FROM sys.tables t
            JOIN sys.schemas s ON s.schema_id = t.schema_id
            JOIN sys.columns c ON c.object_id = t.object_id
            JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            WHERE s.name = N'dbo' AND t.name IN (N'Products', N'Units');

            SELECT t.name, i.name, i.is_unique, i.filter_definition, c.name, ic.key_ordinal
            FROM sys.tables t
            JOIN sys.schemas s ON s.schema_id = t.schema_id
            JOIN sys.indexes i ON i.object_id = t.object_id
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE s.name = N'dbo' AND t.name = N'Products' AND ic.key_ordinal > 0
            ORDER BY t.name, i.name, ic.key_ordinal;

            SELECT pt.name, pc.name, rt.name, rc.name
            FROM sys.foreign_key_columns fkc
            JOIN sys.tables pt ON pt.object_id = fkc.parent_object_id
            JOIN sys.schemas ps ON ps.schema_id = pt.schema_id
            JOIN sys.columns pc ON pc.object_id = pt.object_id AND pc.column_id = fkc.parent_column_id
            JOIN sys.tables rt ON rt.object_id = fkc.referenced_object_id
            JOIN sys.columns rc ON rc.object_id = rt.object_id AND rc.column_id = fkc.referenced_column_id
            WHERE ps.name = N'dbo' AND pt.name = N'Products';

            SELECT cc.name
            FROM sys.check_constraints cc
            JOIN sys.tables t ON t.object_id = cc.parent_object_id
            JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE s.name = N'dbo' AND t.name = N'Products';
            """;

        var columns = new List<SqlColumnDefinition>();
        var indexes = new List<SqlIndexDefinition>();
        var foreignKeys = new List<SqlForeignKeyDefinition>();
        var checks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new SqlColumnDefinition(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                Convert.ToInt32(reader.GetValue(3)),
                reader.GetByte(4),
                reader.GetByte(5),
                reader.GetBoolean(6),
                reader.GetBoolean(7)));
        }

        await reader.NextResultAsync(cancellationToken);
        var indexRows = new List<(string Table, string Name, bool Unique, string? Filter, string Column, int Order)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            indexRows.Add((
                reader.GetString(0),
                reader.GetString(1),
                reader.GetBoolean(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetString(4),
                Convert.ToInt32(reader.GetValue(5))));
        }
        indexes.AddRange(indexRows
            .GroupBy(row => (row.Table, row.Name, row.Unique, row.Filter))
            .Select(group => new SqlIndexDefinition(
                group.Key.Table,
                group.Key.Name,
                group.Key.Unique,
                group.Key.Filter,
                group.OrderBy(row => row.Order).Select(row => row.Column).ToArray())));

        await reader.NextResultAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            foreignKeys.Add(new SqlForeignKeyDefinition(
                reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
        }

        await reader.NextResultAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            checks.Add(reader.GetString(0));
        }

        return new DatabaseSchema(columns, indexes, foreignKeys, checks);
    }

    public async Task<DatabaseReferenceData> LoadReferenceDataAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT [Id], [Name], [ShortName] FROM [dbo].[Units];
            SELECT [Code], [Barcode] FROM [dbo].[Products];
            """;
        var units = new List<MeasurementUnitRecord>();
        var products = new List<ExistingProductRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            units.Add(new MeasurementUnitRecord(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2)));
        }

        await reader.NextResultAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(new ExistingProductRecord(
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1)));
        }

        return new DatabaseReferenceData(units, products);
    }

    public async Task<ImportExecutionResult> ImportAsync(
        IReadOnlyList<ImportProductRow> products,
        CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await CreateStagingTableAsync(connection, transaction, cancellationToken);
            await BulkCopyAsync(connection, transaction, products, cancellationToken);
            var inserted = await ValidateAndInsertAsync(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ImportExecutionResult(inserted, DateTime.UtcNow - started);
        }
        catch (Exception exception)
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                throw new ProductImportTransactionException(
                    "Import failed and transaction rollback also failed.",
                    rollbackSucceeded: false,
                    new AggregateException(exception, rollbackException));
            }

            throw new ProductImportTransactionException(
                "Import failed. Transaction rolled back.",
                rollbackSucceeded: true,
                exception);
        }
    }

    private static async Task CreateStagingTableAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE #ProductImportStaging
            (
                [SourceRowNumber] int NOT NULL,
                [Code] nvarchar(50) NOT NULL,
                [Barcode] nvarchar(100) NULL,
                [Name] nvarchar(300) NOT NULL,
                [UnitId] int NOT NULL,
                [SupplierName] nvarchar(300) NULL,
                [SupplierCode] nvarchar(100) NULL,
                [CostPrice] decimal(18,5) NULL,
                [Price] decimal(18,5) NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task BulkCopyAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        IReadOnlyList<ImportProductRow> products,
        CancellationToken cancellationToken)
    {
        using var table = CreateDataTable(products);
        using var bulkCopy = new SqlBulkCopy(
            connection,
            SqlBulkCopyOptions.CheckConstraints,
            transaction)
        {
            DestinationTableName = StagingTableName,
            BatchSize = 2000,
            BulkCopyTimeout = 120
        };
        foreach (DataColumn column in table.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }
        await bulkCopy.WriteToServerAsync(table, cancellationToken);
    }

    private static DataTable CreateDataTable(IEnumerable<ImportProductRow> products)
    {
        var table = new DataTable();
        table.Columns.Add("SourceRowNumber", typeof(int));
        table.Columns.Add("Code", typeof(string));
        table.Columns.Add("Barcode", typeof(string));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("UnitId", typeof(int));
        table.Columns.Add("SupplierName", typeof(string));
        table.Columns.Add("SupplierCode", typeof(string));
        table.Columns.Add("CostPrice", typeof(decimal));
        table.Columns.Add("Price", typeof(decimal));

        foreach (var product in products)
        {
            table.Rows.Add(
                product.SourceRowNumber,
                product.Code,
                (object?)product.Barcode ?? DBNull.Value,
                product.Name,
                product.UnitId,
                (object?)product.SupplierName ?? DBNull.Value,
                (object?)product.SupplierCode ?? DBNull.Value,
                (object?)product.CostPrice ?? DBNull.Value,
                product.Price);
        }

        return table;
    }

    private static async Task<int> ValidateAndInsertAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            IF EXISTS
            (
                SELECT 1
                FROM #ProductImportStaging s
                JOIN dbo.Products p WITH (UPDLOCK, HOLDLOCK) ON p.Code = s.Code
            )
                THROW 51000, 'A Product Code became unavailable after Dry Run.', 1;

            IF EXISTS
            (
                SELECT 1
                FROM #ProductImportStaging s
                JOIN dbo.Products p WITH (UPDLOCK, HOLDLOCK) ON p.Barcode = s.Barcode
                WHERE s.Barcode IS NOT NULL
            )
                THROW 51001, 'A Product Barcode became unavailable after Dry Run.', 1;

            INSERT dbo.Products
            (
                [Code], [Barcode], [Name], [UnitId], [SupplierName], [SupplierCode],
                [CostPrice], [Price], [IsActive], [DateCreated], [DateUpdated]
            )
            SELECT
                [Code], [Barcode], [Name], [UnitId], [SupplierName], [SupplierCode],
                [CostPrice], [Price], CAST(1 AS bit), SYSUTCDATETIME(), NULL
            FROM #ProductImportStaging;

            SELECT @@ROWCOUNT;
            """;
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }
}

public sealed class ProductImportTransactionException(
    string message,
    bool rollbackSucceeded,
    Exception innerException)
    : Exception(message, innerException)
{
    public bool RollbackSucceeded { get; } = rollbackSucceeded;
}
