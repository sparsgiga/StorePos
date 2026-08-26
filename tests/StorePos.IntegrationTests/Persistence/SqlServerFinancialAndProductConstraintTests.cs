using Microsoft.EntityFrameworkCore;
using StorePos.Domain.Aggregates.Customer;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;
using StorePos.Persistence.Repositories;

namespace StorePos.IntegrationTests.Persistence;

public sealed class SqlServerFinancialAndProductConstraintTests
{
    [SqlServerFact]
    public async Task SqlServer_PersistsCanonicalMoneyAndFiveScaleUnitPrice()
    {
        await using var database = new SqlServerTestDatabase();
        await database.MigrateToLatestAsync();

        await using (var context = database.CreateContext())
        {
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

        await using var verification = database.CreateContext();
        var persistedSale = await verification.Sales.AsNoTracking().SingleAsync();
        var persistedItem = await verification.SaleItems.AsNoTracking().SingleAsync();
        var persistedPayment = await verification.SalePayments.AsNoTracking().SingleAsync();

        Assert.Equal(12.67m, persistedItem.LineTotal);
        Assert.Equal(12.67m, persistedSale.TotalAmount);
        Assert.Equal(12.67m, persistedPayment.Amount);
        Assert.Equal(12.66565m, persistedItem.UnitPrice);
        Assert.Equal(1, persistedSale.CompletionVersion);
        Assert.Equal(1, persistedPayment.CompletionVersion);
    }

    [SqlServerFact]
    public async Task SqlServer_RejectsDuplicateNonNullBarcode()
    {
        await using var database = new SqlServerTestDatabase();
        await database.MigrateToLatestAsync();
        await using var context = database.CreateContext();
        context.Products.AddRange(
            Product.Create("100", "1234567890123", "First", 1, 1m),
            Product.Create("101", "1234567890123", "Second", 1, 1m));

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await context.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task SqlServer_RowVersionPreventsConcurrentDebtOverpayment()
    {
        await using var database = new SqlServerTestDatabase();
        await database.MigrateToLatestAsync();
        long saleId;

        await using (var setup = database.CreateContext())
        {
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

        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var first = await new SaleRepository(firstContext)
            .GetCompletedForUpdateAsync(saleId);
        var second = await new SaleRepository(secondContext)
            .GetCompletedForUpdateAsync(saleId);
        first!.AddDebtPayment(Guid.NewGuid(), PaymentType.Cash, 80m);
        second!.AddDebtPayment(Guid.NewGuid(), PaymentType.Card, 80m);

        await firstContext.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
            await secondContext.SaveChangesAsync());

        await using var verification = database.CreateContext();
        var persisted = await new SaleRepository(verification)
            .GetCompletedForUpdateAsync(saleId);
        Assert.NotNull(persisted);
        Assert.Equal(80m, persisted.PaidAmount);
        Assert.Equal(20m, persisted.OutstandingAmount);
        Assert.Single(persisted.Payments);
        Assert.Equal(persisted.CompletionVersion,
            Assert.Single(persisted.Payments).CompletionVersion);
    }

    [SqlServerFact]
    public async Task SqlServer_RowVersionPreventsDuplicateConcurrentCompletionSets()
    {
        await using var database = new SqlServerTestDatabase();
        await database.MigrateToLatestAsync();
        long saleId;

        await using (var setup = database.CreateContext())
        {
            var sale = Sale.Create("SQL-CONCURRENT-COMPLETE-1");
            await setup.Sales.AddAsync(sale);
            await setup.SaveChangesAsync();
            sale.AddManualItem("Item", 1m, 100m);
            await setup.SaveChangesAsync();
            saleId = sale.Id;
        }

        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var first = await new SaleRepository(firstContext).GetDraftForUpdateAsync(saleId);
        var second = await new SaleRepository(secondContext).GetDraftForUpdateAsync(saleId);
        first!.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 100m)],
            DateTime.Now);
        second!.Complete(
            [new SalePaymentAllocation(PaymentType.Card, 100m)],
            DateTime.Now);

        await firstContext.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
            await secondContext.SaveChangesAsync());

        await using var verification = database.CreateContext();
        var persisted = await new SaleRepository(verification)
            .GetCompletedForUpdateAsync(saleId);
        Assert.NotNull(persisted);
        Assert.Equal(1, persisted.CompletionVersion);
        var payment = Assert.Single(persisted.Payments);
        Assert.Equal(1, payment.CompletionVersion);
        Assert.Equal(PaymentType.Cash, payment.PaymentType);
    }

    [SqlServerFact]
    public async Task SqlServer_RecompletionInsertsNewPaymentsAndPreservesFirstVersion()
    {
        await using var database = new SqlServerTestDatabase();
        await database.MigrateToLatestAsync();
        long saleId;
        long oldPaymentId;
        DateTime oldDateCreated;

        await using (var context = database.CreateContext())
        {
            var sale = Sale.Create("SQL-RECOMPLETE-1");
            await context.Sales.AddAsync(sale);
            await context.SaveChangesAsync();
            var item = sale.AddManualItem("Item", 1m, 100m);
            sale.Complete(
                [new SalePaymentAllocation(PaymentType.Cash, 100m)],
                DateTime.Now);
            await context.SaveChangesAsync();
            var oldPayment = Assert.Single(sale.Payments);
            saleId = sale.Id;
            oldPaymentId = oldPayment.Id;
            oldDateCreated = oldPayment.DateCreated;

            sale.Reopen();
            sale.UpdateItem(item.Id, item.ProductName, 1m, 120m);
            sale.Complete(
                [
                    new SalePaymentAllocation(PaymentType.Cash, 50m),
                    new SalePaymentAllocation(PaymentType.Card, 70m)
                ],
                DateTime.Now.AddMinutes(1));
            await context.SaveChangesAsync();
        }

        await using var verification = database.CreateContext();
        var persisted = await new SaleRepository(verification)
            .GetCompletedForUpdateAsync(saleId);
        Assert.NotNull(persisted);
        Assert.Equal(2, persisted.CompletionVersion);
        Assert.Equal(3, persisted.Payments.Count);
        var oldPaymentReloaded = persisted.Payments.Single(payment =>
            payment.Id == oldPaymentId);
        Assert.Equal(1, oldPaymentReloaded.CompletionVersion);
        Assert.Equal(100m, oldPaymentReloaded.Amount);
        Assert.Equal(PaymentType.Cash, oldPaymentReloaded.PaymentType);
        Assert.Equal(SalePaymentKind.Completion, oldPaymentReloaded.PaymentKind);
        Assert.Equal(oldDateCreated, oldPaymentReloaded.DateCreated);
        Assert.Null(oldPaymentReloaded.DateUpdated);
        Assert.Equal(120m, persisted.PaidAmount);
        Assert.Equal(0m, persisted.OutstandingAmount);
    }

    [SqlServerFact]
    public async Task SqlServer_TenReopensPreserveAllPaymentVersionsAndCurrentTotals()
    {
        await using var database = new SqlServerTestDatabase();
        await database.MigrateToLatestAsync();
        long saleId;
        var snapshots = new Dictionary<long, (int Version, decimal Amount, PaymentType Type)>();

        await using (var context = database.CreateContext())
        {
            var sale = Sale.Create("SQL-REOPEN-10");
            await context.Sales.AddAsync(sale);
            await context.SaveChangesAsync();
            var item = sale.AddManualItem("Item", 1m, 100m);
            await context.SaveChangesAsync();
            saleId = sale.Id;

            for (var version = 1; version <= 10; version++)
            {
                var total = 100m + version;
                sale.UpdateItem(item.Id, item.ProductName, 1m, total);
                var type = version % 2 == 0 ? PaymentType.Card : PaymentType.Cash;
                sale.Complete(
                    [new SalePaymentAllocation(type, total)],
                    DateTime.Now.AddMinutes(version));
                await context.SaveChangesAsync();
                var payment = sale.Payments.Single(current =>
                    current.CompletionVersion == version);
                snapshots[payment.Id] = (version, payment.Amount, payment.PaymentType);

                if (version < 10)
                {
                    sale.Reopen();
                    await context.SaveChangesAsync();
                }
            }
        }

        await using var verification = database.CreateContext();
        var persisted = await new SaleRepository(verification)
            .GetCompletedForUpdateAsync(saleId);
        Assert.NotNull(persisted);
        Assert.Equal(10, persisted.CompletionVersion);
        Assert.Equal(10, persisted.Payments.Count);
        Assert.Equal(Enumerable.Range(1, 10),
            persisted.Payments
                .OrderBy(payment => payment.CompletionVersion)
                .Select(payment => payment.CompletionVersion));
        Assert.Equal(110m, persisted.PaidAmount);
        Assert.Equal(0m, persisted.OutstandingAmount);

        foreach (var snapshot in snapshots)
        {
            var payment = persisted.Payments.Single(current => current.Id == snapshot.Key);
            Assert.Equal(snapshot.Value.Version, payment.CompletionVersion);
            Assert.Equal(snapshot.Value.Amount, payment.Amount);
            Assert.Equal(snapshot.Value.Type, payment.PaymentType);
        }
    }
}
