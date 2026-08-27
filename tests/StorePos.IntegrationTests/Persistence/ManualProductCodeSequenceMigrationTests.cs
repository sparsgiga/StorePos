using Microsoft.EntityFrameworkCore;
using StorePos.Domain.Aggregates.Product;
using StorePos.Persistence.Services;

namespace StorePos.IntegrationTests.Persistence;

public sealed class ManualProductCodeSequenceMigrationTests
{
    private const string PreviousMigration =
        "20260826201142_AddProductSupplierMetadataAndAllowZeroPrice";

    [SqlServerFact]
    public async Task Migration_FourDigitProgressionInitializesToNextCode()
    {
        await using var database = new SqlServerTestDatabase();
        await database.MigrateToAsync(PreviousMigration);
        await using (var setup = database.CreateContext())
        {
            await setup.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO [dbo].[Products]
                    ([Code], [Barcode], [Name], [UnitId], [Price], [SupplierName],
                     [SupplierCode], [CostPrice], [IsActive], [DateCreated], [DateUpdated])
                VALUES
                    (N'1000', NULL, N'First', 1, 1, NULL, NULL, NULL, 1, SYSDATETIME(), NULL),
                    (N'9755', NULL, N'Current', 1, 1, NULL, NULL, NULL, 1, SYSDATETIME(), NULL);
                """);
        }

        await database.MigrateToLatestAsync();

        await using var verification = database.CreateContext();
        Assert.Equal(
            9756,
            await verification.ManualProductCodeSequences
                .Select(sequence => sequence.NextCode)
                .SingleAsync());
    }

    [SqlServerFact]
    public async Task Migration_InitializesFromFourDigitRangeAndIgnoresOutliersAndProductId()
    {
        await using var database = new SqlServerTestDatabase();
        await database.MigrateToAsync(PreviousMigration);
        await using (var setup = database.CreateContext())
        {
            await setup.Database.ExecuteSqlRawAsync(
                """
                SET IDENTITY_INSERT [dbo].[Products] ON;
                INSERT INTO [dbo].[Products]
                    ([Id], [Code], [Barcode], [Name], [UnitId], [Price], [SupplierName],
                     [SupplierCode], [CostPrice], [IsActive], [DateCreated], [DateUpdated])
                VALUES
                    (1, N'1000', NULL, N'First', 1, 1, NULL, NULL, NULL, 1, SYSDATETIME(), NULL),
                    (2, N'9755', NULL, N'Current', 1, 1, NULL, NULL, NULL, 1, SYSDATETIME(), NULL),
                    (900000, N'75505', NULL, N'Outlier', 1, 1, NULL, NULL, NULL, 1, SYSDATETIME(), NULL),
                    (900001, N'GMTEK-40012', NULL, N'Mixed', 1, 1, NULL, NULL, NULL, 1, SYSDATETIME(), NULL);
                SET IDENTITY_INSERT [dbo].[Products] OFF;
                """);
        }

        await database.MigrateToLatestAsync();

        await using var verification = database.CreateContext();
        Assert.Equal(
            9756,
            await verification.ManualProductCodeSequences
                .Select(sequence => sequence.NextCode)
                .SingleAsync());
    }

    [SqlServerFact]
    public async Task Migration_NoFourDigitNumericCodeStartsAt1000()
    {
        await using var database = new SqlServerTestDatabase();
        await database.MigrateToAsync(PreviousMigration);
        await using (var setup = database.CreateContext())
        {
            await setup.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO [dbo].[Products]
                    ([Code], [Barcode], [Name], [UnitId], [Price], [SupplierName],
                     [SupplierCode], [CostPrice], [IsActive], [DateCreated], [DateUpdated])
                VALUES
                    (N'GMTEK-40012', NULL, N'Mixed', 1, 1, NULL, NULL, NULL, 1, SYSDATETIME(), NULL),
                    (N'75505', NULL, N'Outlier', 1, 1, NULL, NULL, NULL, 1, SYSDATETIME(), NULL);
                """);
        }

        await database.MigrateToLatestAsync();

        await using var verification = database.CreateContext();
        Assert.Equal(
            1000,
            await verification.ManualProductCodeSequences
                .Select(sequence => sequence.NextCode)
                .SingleAsync());
    }

    [SqlServerFact]
    public async Task Migration_FourDigitMaximum9999StartsAt10000()
    {
        await using var database = new SqlServerTestDatabase();
        await database.MigrateToAsync(PreviousMigration);
        await using (var setup = database.CreateContext())
        {
            await setup.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO [dbo].[Products]
                    ([Code], [Barcode], [Name], [UnitId], [Price], [SupplierName],
                     [SupplierCode], [CostPrice], [IsActive], [DateCreated], [DateUpdated])
                VALUES
                    (N'9999', NULL, N'Last four digit', 1, 1, NULL, NULL, NULL, 1, SYSDATETIME(), NULL),
                    (N'75505', NULL, N'Outlier', 1, 1, NULL, NULL, NULL, 1, SYSDATETIME(), NULL);
                """);
        }

        await database.MigrateToLatestAsync();

        await using var verification = database.CreateContext();
        Assert.Equal(
            10000,
            await verification.ManualProductCodeSequences
                .Select(sequence => sequence.NextCode)
                .SingleAsync());
    }

    [SqlServerFact]
    public async Task SqlServerSuggestion_SkipsOccupiedCandidatesButNotLargeOutlier()
    {
        await using var database = new SqlServerTestDatabase();
        await database.MigrateToLatestAsync();
        await using var context = database.CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "UPDATE [dbo].[ManualProductCodeSequence] SET [NextCode] = 9756 WHERE [Id] = 1;");
        await context.Products.AddAsync(
            Product.Create("75505", null, "Outlier", 1, 1m));
        await context.SaveChangesAsync();
        var service = new ManualProductCodeSequenceService(context);

        Assert.Equal("9756", await service.GetSuggestedCodeAsync());

        await context.Products.AddRangeAsync(
            Product.Create("9756", null, "One", 1, 1m),
            Product.Create("9757", null, "Two", 1, 1m),
            Product.Create("9758", null, "Three", 1, 1m));
        await context.SaveChangesAsync();

        Assert.Equal("9759", await service.GetSuggestedCodeAsync());

        await using var transaction = await context.Database.BeginTransactionAsync();
        await service.AdvanceIfConsumedAsync("9759");
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        context.ChangeTracker.Clear();
        Assert.Equal(
            9760,
            await context.ManualProductCodeSequences
                .Select(sequence => sequence.NextCode)
                .SingleAsync());
    }
}
