using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace StorePos.IntegrationTests.Persistence;

public sealed class SaleDiscountMigrationTests
{
    private const string PreviousMigration =
        "20260827100650_AddManualProductCodeSequence";

    [SqlServerFact]
    public async Task Migration_ExistingSaleReceivesZeroDiscount_AndCanRollback()
    {
        await using var database = new SqlServerTestDatabase();
        await database.MigrateToAsync(PreviousMigration);
        await using (var oldSchema = database.CreateContext())
        {
            await oldSchema.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO [dbo].[Sales]
                    ([SaleNumber], [Status], [CashierId], [CustomerId], [CustomerName],
                     [CustomerIdentificationNumber], [TotalAmount], [PaidAmount],
                     [OutstandingAmount], [Comment], [DateCreated], [DateUpdated],
                     [DateCompleted], [DateCancelled], [FinancialRevision],
                     [CompletionVersion])
                VALUES
                    (N'MIG-DISCOUNT-1', 1, NULL, NULL, NULL, NULL, 25.00, 0.00,
                     0.00, NULL, SYSDATETIME(), NULL, NULL, NULL, 0, 0);
                """);
        }

        await database.MigrateToLatestAsync();

        await using (var verification = database.CreateContext())
        {
            Assert.Equal(
                0m,
                await verification.Sales
                    .Where(sale => sale.SaleNumber == "MIG-DISCOUNT-1")
                    .Select(sale => sale.DiscountAmount)
                    .SingleAsync());
        }

        await database.MigrateToAsync(PreviousMigration);
        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COL_LENGTH('dbo.Sales', 'DiscountAmount');";
        Assert.Equal(DBNull.Value, await command.ExecuteScalarAsync());
    }
}
