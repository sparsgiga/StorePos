using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Initialization;

public static class DatabaseMigrationExtensions
{
    private const int MaximumAttempts = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    public static async Task ApplyDatabaseMigrationsAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var serviceProvider = scope.ServiceProvider;
        var dbContext = serviceProvider.GetRequiredService<StorePosDbContext>();
        var logger = serviceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("StorePos.DatabaseMigration");
        var connection = dbContext.Database.GetDbConnection();

        logger.LogInformation(
            "Starting database migration check for server {DatabaseServer} and database {DatabaseName}.",
            connection.DataSource,
            connection.Database);

        try
        {
            for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
            {
                try
                {
                    await dbContext.Database.MigrateAsync(cancellationToken);
                    break;
                }
                catch (DbException exception)
                    when (exception.IsTransient && attempt < MaximumAttempts)
                {
                    logger.LogWarning(
                        exception,
                        "Transient database migration connection failure on attempt {Attempt} of {MaximumAttempts}. Retrying in {RetryDelaySeconds} seconds.",
                        attempt,
                        MaximumAttempts,
                        RetryDelay.TotalSeconds);

                    await Task.Delay(RetryDelay, cancellationToken);
                }
            }

            logger.LogInformation(
                "Database migration completed successfully for server {DatabaseServer} and database {DatabaseName}.",
                connection.DataSource,
                connection.Database);
        }
        catch (Exception exception)
        {
            logger.LogCritical(
                exception,
                "Database migration failed for server {DatabaseServer} and database {DatabaseName}. API startup will stop.",
                connection.DataSource,
                connection.Database);

            throw;
        }
    }
}
