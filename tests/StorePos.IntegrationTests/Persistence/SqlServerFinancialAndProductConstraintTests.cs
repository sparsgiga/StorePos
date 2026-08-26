using Microsoft.EntityFrameworkCore;
using StorePos.Domain.Aggregates.Customer;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;
using StorePos.Persistence.Context;
using StorePos.Persistence.Repositories;

namespace StorePos.IntegrationTests.Persistence;

public sealed class SqlServerFinancialAndProductConstraintTests
{
    [SqlServerFact]
    public async Task SqlServer_PersistsCanonicalMoneyAndFiveScaleUnitPrice()
    {
        var databaseName = CreateDatabaseName();
        try
        {
            await using (var context = CreateContext(databaseName))
            {
                await context.Database.EnsureCreatedAsync();
                var sale = Sale.Create("SQL-DECIMAL-1");
                await context.Sales.AddAsync(sale);
                await context.SaveChangesAsync();
                var item = sale.AddManualItem("Product", 1m, 12.66565m);
                sale.Complete(
                    [new SalePaymentAllocation(PaymentType.Cash, 12.66565m)],
                    DateTime.Now);
                await context.SaveChangesAsync();
                Assert.Equal(12.66565m, item.UnitPrice);
            }

            await using var verification = CreateContext(databaseName);
            var persistedSale = await verification.Sales.AsNoTracking().SingleAsync();
            var persistedItem = await verification.SaleItems.AsNoTracking().SingleAsync();
            var persistedPayment = await verification.SalePayments.AsNoTracking().SingleAsync();

            Assert.Equal(12.67m, persistedItem.LineTotal);
            Assert.Equal(12.67m, persistedSale.TotalAmount);
            Assert.Equal(12.67m, persistedPayment.Amount);
            Assert.Equal(12.66565m, persistedItem.UnitPrice);
            Assert.Equal(persistedItem.LineTotal, persistedSale.TotalAmount);
        }
        finally
        {
            await DeleteDatabaseAsync(databaseName);
        }
    }

    [SqlServerFact]
    public async Task SqlServer_RejectsDuplicateNonNullBarcode()
    {
        var databaseName = CreateDatabaseName();
        try
        {
            await using var context = CreateContext(databaseName);
            await context.Database.EnsureCreatedAsync();
            context.Products.AddRange(
                Product.Create("100", "1234567890123", "First", 1, 1m),
                Product.Create("101", "1234567890123", "Second", 1, 1m));

            await Assert.ThrowsAsync<DbUpdateException>(async () =>
                await context.SaveChangesAsync());
        }
        finally
        {
            await DeleteDatabaseAsync(databaseName);
        }
    }

    [SqlServerFact]
    public async Task SqlServer_RowVersionPreventsConcurrentDebtOverpayment()
    {
        var databaseName = CreateDatabaseName();
        try
        {
            long saleId;
            await using (var setup = CreateContext(databaseName))
            {
                await setup.Database.EnsureCreatedAsync();
                var customer = Customer.Create("Customer", "SQL-CUSTOMER-1", null);
                await setup.Customers.AddAsync(customer);
                await setup.SaveChangesAsync();
                var sale = Sale.Create("SQL-CONCURRENCY-1");
                await setup.Sales.AddAsync(sale);
                await setup.SaveChangesAsync();
                sale.AssignCustomer(customer.Id, customer.Name, customer.IdentificationNumber);
                sale.AddManualItem("Product", 1m, 100m);
                sale.Complete([], DateTime.Now, allowDebt: true);
                await setup.SaveChangesAsync();
                saleId = sale.Id;
            }

            await using var firstContext = CreateContext(databaseName);
            await using var secondContext = CreateContext(databaseName);
            var first = await new SaleRepository(firstContext)
                .GetCompletedForUpdateAsync(saleId);
            var second = await new SaleRepository(secondContext)
                .GetCompletedForUpdateAsync(saleId);
            first!.AddDebtPayment(Guid.NewGuid(), PaymentType.Cash, 80m);
            second!.AddDebtPayment(Guid.NewGuid(), PaymentType.Card, 80m);

            await firstContext.SaveChangesAsync();
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
                await secondContext.SaveChangesAsync());

            await using var verification = CreateContext(databaseName);
            var persisted = await new SaleRepository(verification)
                .GetCompletedForUpdateAsync(saleId);
            Assert.NotNull(persisted);
            Assert.Equal(80m, persisted.PaidAmount);
            Assert.Equal(20m, persisted.OutstandingAmount);
            Assert.Single(persisted.Payments);
        }
        finally
        {
            await DeleteDatabaseAsync(databaseName);
        }
    }

    private static string CreateDatabaseName()
        => $"StorePosIntegration_{Guid.NewGuid():N}";

    private static StorePosDbContext CreateContext(string databaseName)
    {
        var connectionString =
            $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};" +
            "Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new StorePosDbContext(options);
    }

    private static async Task DeleteDatabaseAsync(string databaseName)
    {
        await using var context = CreateContext(databaseName);
        await context.Database.EnsureDeletedAsync();
    }
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
