using Microsoft.EntityFrameworkCore;
using StorePos.Application.Sales.Commands.Cancel;
using StorePos.Application.Sales.Commands.Complete;
using StorePos.Application.Sales.Queries.GetDrafts;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;
using StorePos.Persistence;
using StorePos.Persistence.Context;
using StorePos.Persistence.Repositories;

namespace StorePos.IntegrationTests.Sales;

public sealed class SaleLifecycleWorkflowTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 8, 25, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CompleteSale_PersistsPaymentsAndDoesNotReturnAfterRestart()
    {
        var options = CreateOptions();
        long saleId;

        await using (var context = new StorePosDbContext(options))
        {
            var repository = new SaleRepository(context);
            var unitOfWork = new UnitOfWork(context);
            var sale = Sale.Create("20260825-0001");
            await repository.AddAsync(sale);
            await unitOfWork.SaveChangesAsync();

            sale.AddManualItem("A", 1m, 200m);
            sale.AddManualItem("B", 1m, 72m);
            await unitOfWork.SaveChangesAsync();
            saleId = sale.Id;

            var handler = new CompleteSaleCommandHandler(
                repository,
                unitOfWork,
                new FixedTimeProvider(FixedNow));
            var result = await handler.Handle(
                new CompleteSaleCommand(
                    saleId,
                    [
                        new CompleteSalePayment(PaymentType.Cash, 100m),
                        new CompleteSalePayment(PaymentType.Card, 172m)
                    ]),
                CancellationToken.None);

            Assert.NotNull(result);
        }

        await using (var restartedContext = new StorePosDbContext(options))
        {
            var persistedSale = await restartedContext.Sales
                .Include(sale => sale.Items)
                .Include(sale => sale.Payments)
                .SingleAsync(sale => sale.Id == saleId);

            Assert.Equal(SaleStatus.Completed, persistedSale.Status);
            Assert.Equal(FixedNow.UtcDateTime, persistedSale.DateCompleted);
            Assert.Equal(2, persistedSale.Items.Count);
            Assert.Collection(
                persistedSale.Payments.OrderBy(payment => payment.PaymentType),
                payment =>
                {
                    Assert.Equal(PaymentType.Cash, payment.PaymentType);
                    Assert.Equal(100m, payment.Amount);
                },
                payment =>
                {
                    Assert.Equal(PaymentType.Card, payment.PaymentType);
                    Assert.Equal(172m, payment.Amount);
                });

            var drafts = await new GetDraftSalesQueryHandler(
                    new SaleRepository(restartedContext))
                .Handle(new GetDraftSalesQuery(), CancellationToken.None);

            Assert.Empty(drafts);
        }
    }

    [Fact]
    public async Task CancelSale_PreservesSaleAndItemsAndDoesNotReturnAfterRestart()
    {
        var options = CreateOptions();
        long saleId;
        long itemId;

        await using (var context = new StorePosDbContext(options))
        {
            var repository = new SaleRepository(context);
            var unitOfWork = new UnitOfWork(context);
            var sale = Sale.Create("20260825-0002");
            await repository.AddAsync(sale);
            await unitOfWork.SaveChangesAsync();

            var item = sale.AddManualItem("პროდუქტი", 2m, 5m);
            await unitOfWork.SaveChangesAsync();
            saleId = sale.Id;
            itemId = item.Id;

            var handler = new CancelSaleCommandHandler(
                repository,
                unitOfWork,
                new FixedTimeProvider(FixedNow));
            var result = await handler.Handle(
                new CancelSaleCommand(saleId),
                CancellationToken.None);

            Assert.NotNull(result);
        }

        await using (var restartedContext = new StorePosDbContext(options))
        {
            var persistedSale = await restartedContext.Sales
                .Include(sale => sale.Items)
                .SingleAsync(sale => sale.Id == saleId);

            Assert.Equal(SaleStatus.Cancelled, persistedSale.Status);
            Assert.Equal(FixedNow.UtcDateTime, persistedSale.DateCancelled);
            Assert.Null(persistedSale.DateCompleted);
            Assert.Equal(itemId, Assert.Single(persistedSale.Items).Id);
            Assert.Equal(1, await restartedContext.Sales.CountAsync());
            Assert.Equal(1, await restartedContext.SaleItems.CountAsync());

            var drafts = await new GetDraftSalesQueryHandler(
                    new SaleRepository(restartedContext))
                .Handle(new GetDraftSalesQuery(), CancellationToken.None);

            Assert.Empty(drafts);
        }
    }

    private static DbContextOptions<StorePosDbContext> CreateOptions()
        => new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
