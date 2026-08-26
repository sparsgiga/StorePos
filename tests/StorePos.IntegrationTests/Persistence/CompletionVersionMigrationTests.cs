using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StorePos.Domain.Enums;

namespace StorePos.IntegrationTests.Persistence;

public sealed class CompletionVersionMigrationTests
{
    private const string PreviousMigration =
        "20260825210235_AddProductBarcodeUniqueness";

    [SqlServerFact]
    public async Task MigrationUpgrade_BackfillsVersionsWithoutChangingPaymentHistory()
    {
        await using var database = new SqlServerTestDatabase();
        await database.MigrateToAsync(PreviousMigration);
        var operationId = Guid.NewGuid();
        var paymentDate = new DateTime(2026, 8, 25, 13, 15, 0);

        await using (var oldSchema = database.CreateContext())
        {
            await oldSchema.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO [dbo].[Sales]
                    ([SaleNumber], [Status], [CashierId], [CustomerId], [CustomerName],
                     [CustomerIdentificationNumber], [TotalAmount], [Comment], [DateCreated],
                     [DateUpdated], [DateCompleted], [DateCancelled], [FinancialRevision])
                VALUES
                    (N'MIG-COMPLETED-PAID', 2, NULL, NULL, NULL, NULL, 100.00, NULL,
                     '2026-08-25T10:00:00', NULL, '2026-08-25T11:00:00', NULL, 1),
                    (N'MIG-COMPLETED-ZERO', 2, NULL, NULL, NULL, NULL, 50.00, NULL,
                     '2026-08-25T10:00:00', NULL, '2026-08-25T11:00:00', NULL, 0),
                    (N'MIG-DRAFT', 1, NULL, NULL, NULL, NULL, 25.00, NULL,
                     '2026-08-25T10:00:00', NULL, NULL, NULL, 0),
                    (N'MIG-CANCELLED', 3, NULL, NULL, NULL, NULL, 30.00, NULL,
                     '2026-08-25T10:00:00', NULL, NULL, '2026-08-25T12:00:00', 0);
                """);

            await oldSchema.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO [dbo].[SalePayments]
                    ([SaleId], [PaymentType], [PaymentKind], [Amount], [OperationId],
                     [DateCreated], [DateUpdated])
                SELECT [Id], 3, 2, 25.00, {operationId}, {paymentDate}, NULL
                FROM [dbo].[Sales]
                WHERE [SaleNumber] = N'MIG-COMPLETED-PAID';
                """);
        }

        await database.MigrateToLatestAsync();

        await using var verification = database.CreateContext();
        var sales = await verification.Sales
            .AsNoTracking()
            .ToDictionaryAsync(sale => sale.SaleNumber);
        var payment = await verification.SalePayments.AsNoTracking().SingleAsync();

        Assert.Equal(1, sales["MIG-COMPLETED-PAID"].CompletionVersion);
        Assert.Equal(1, sales["MIG-COMPLETED-ZERO"].CompletionVersion);
        Assert.Equal(0, sales["MIG-DRAFT"].CompletionVersion);
        Assert.Equal(0, sales["MIG-CANCELLED"].CompletionVersion);
        Assert.Equal(1, payment.CompletionVersion);
        Assert.Equal(25m, payment.Amount);
        Assert.Equal(PaymentType.BankTransfer, payment.PaymentType);
        Assert.Equal(SalePaymentKind.DebtRepayment, payment.PaymentKind);
        Assert.Equal(operationId, payment.OperationId);
        Assert.Equal(paymentDate, payment.DateCreated);
    }

    [SqlServerFact]
    public async Task MigrationUpgrade_PaymentOnNonCompletedSaleFailsWithoutDeletingPayment()
    {
        await using var database = new SqlServerTestDatabase();
        await database.MigrateToAsync(PreviousMigration);

        await using (var oldSchema = database.CreateContext())
        {
            await oldSchema.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO [dbo].[Sales]
                    ([SaleNumber], [Status], [CashierId], [CustomerId], [CustomerName],
                     [CustomerIdentificationNumber], [TotalAmount], [Comment], [DateCreated],
                     [DateUpdated], [DateCompleted], [DateCancelled], [FinancialRevision])
                VALUES
                    (N'MIG-INVALID-DRAFT', 1, NULL, NULL, NULL, NULL, 10.00, NULL,
                     '2026-08-25T10:00:00', NULL, NULL, NULL, 0);

                INSERT INTO [dbo].[SalePayments]
                    ([SaleId], [PaymentType], [PaymentKind], [Amount], [OperationId],
                     [DateCreated], [DateUpdated])
                SELECT [Id], 1, 1, 10.00, NULL, '2026-08-25T10:05:00', NULL
                FROM [dbo].[Sales]
                WHERE [SaleNumber] = N'MIG-INVALID-DRAFT';
                """);
        }

        var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await database.MigrateToLatestAsync());
        Assert.Contains(
            "a payment belongs to a sale that is not completed",
            GetExceptionMessages(exception),
            StringComparison.OrdinalIgnoreCase);

        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM [dbo].[SalePayments];";
        Assert.Equal(1, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }

    private static string GetExceptionMessages(Exception exception)
    {
        var messages = new List<string>();
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            messages.Add(current.Message);
        }

        return string.Join(Environment.NewLine, messages);
    }
}
