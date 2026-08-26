using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StorePos.Persistence.Context;

namespace StorePos.IntegrationTests.Persistence;

internal sealed class SqlServerTestDatabase : IAsyncDisposable
{
    internal const string ExpectedDataSource = "(localdb)\\MSSQLLocalDB";
    internal const string DatabasePrefix = "StorePosIntegration_";

    private bool _disposed;

    public SqlServerTestDatabase()
    {
        DatabaseName = $"{DatabasePrefix}{Guid.NewGuid():N}";
        ConnectionString = BuildConnectionString(DatabaseName);
    }

    public string DatabaseName { get; }

    public string ConnectionString { get; }

    public StorePosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new StorePosDbContext(options);
    }

    public async Task MigrateToLatestAsync(CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync(cancellationToken);
    }

    public async Task MigrateToAsync(
        string targetMigration,
        CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(targetMigration, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ValidateDeleteTarget(ConnectionString);
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }

    internal static void ValidateDeleteTarget(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var suffix = builder.InitialCatalog.StartsWith(
            DatabasePrefix,
            StringComparison.Ordinal)
            ? builder.InitialCatalog[DatabasePrefix.Length..]
            : string.Empty;

        if (!string.Equals(
                builder.DataSource,
                ExpectedDataSource,
                StringComparison.OrdinalIgnoreCase) ||
            !builder.InitialCatalog.StartsWith(DatabasePrefix, StringComparison.Ordinal) ||
            !Guid.TryParseExact(suffix, "N", out _))
        {
            throw new InvalidOperationException(
                "Refusing to delete a non-test database.");
        }
    }

    private static string BuildConnectionString(string databaseName)
        => new SqlConnectionStringBuilder
        {
            DataSource = ExpectedDataSource,
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            ConnectTimeout = 5
        }.ConnectionString;
}

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("STOREPOS_RUN_SQLSERVER_TESTS"),
                "1",
                StringComparison.Ordinal))
        {
            Skip = "Set STOREPOS_RUN_SQLSERVER_TESTS=1 when a working SQL Server/LocalDB runtime is available.";
        }
    }
}
