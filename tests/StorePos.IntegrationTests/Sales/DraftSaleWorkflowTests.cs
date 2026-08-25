using Microsoft.EntityFrameworkCore;
using StorePos.Application.Sales.Commands.CreateDraft;
using StorePos.Application.Sales.Queries.GetDrafts;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;
using StorePos.Persistence;
using StorePos.Persistence.Context;
using StorePos.Persistence.Repositories;
using StorePos.Persistence.Services;

namespace StorePos.IntegrationTests.Sales;

public sealed class DraftSaleWorkflowTests
{
    [Fact]
    public async Task GetDraftSales_ReturnsOnlyDrafts_InCreationOrder()
    {
        await using var context = CreateContext();
        var repository = new SaleRepository(context);
        var unitOfWork = new UnitOfWork(context);

        var firstDraft = Sale.Create("20260824-0001");
        var completedSale = Sale.Create("20260824-0002");
        var cancelledSale = Sale.Create("20260824-0003");
        var secondDraft = Sale.Create("20260824-0004");

        await repository.AddAsync(firstDraft);
        await repository.AddAsync(completedSale);
        await repository.AddAsync(cancelledSale);
        await repository.AddAsync(secondDraft);
        await unitOfWork.SaveChangesAsync();

        completedSale.AddManualItem("პროდუქტი", 1m, 1m);
        completedSale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 1m)],
            DateTime.UtcNow);
        cancelledSale.Cancel(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var handler = new GetDraftSalesQueryHandler(repository);
        var result = await handler.Handle(new GetDraftSalesQuery(), CancellationToken.None);

        Assert.Collection(
            result,
            sale => Assert.Equal(firstDraft.SaleNumber, sale.SaleNumber),
            sale => Assert.Equal(secondDraft.SaleNumber, sale.SaleNumber));

        Assert.All(
            await repository.GetDraftsAsync(),
            sale => Assert.Equal(EntityState.Detached, context.Entry(sale).State));
    }

    [Fact]
    public async Task CreateDraftSale_ThenGetDraftSales_ReturnsPersistedDraft()
    {
        await using var context = CreateContext();
        var repository = new SaleRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var numberGenerator = new SaleNumberGenerator(
            context,
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 24, 10, 0, 0, TimeSpan.Zero)));

        var createHandler = new CreateDraftSaleCommandHandler(
            numberGenerator,
            repository,
            unitOfWork);

        var created = await createHandler.Handle(
            new CreateDraftSaleCommand(),
            CancellationToken.None);

        context.ChangeTracker.Clear();

        var getDraftsHandler = new GetDraftSalesQueryHandler(repository);
        var drafts = await getDraftsHandler.Handle(
            new GetDraftSalesQuery(),
            CancellationToken.None);

        var persistedDraft = Assert.Single(drafts);
        Assert.NotEqual(0, created.SaleId);
        Assert.Equal("20260824-0001", created.SaleNumber);
        Assert.Equal(created.SaleId, persistedDraft.Id);
        Assert.Equal(created.SaleNumber, persistedDraft.SaleNumber);
    }

    [Fact]
    public async Task SaleNumber_UsesLocalBusinessDateInsteadOfUtcDate()
    {
        await using var context = CreateContext();
        var localTimeZone = TimeZoneInfo.CreateCustomTimeZone(
            "Test +04",
            TimeSpan.FromHours(4),
            "Test +04",
            "Test +04");
        var generator = new SaleNumberGenerator(
            context,
            new FixedTimeProvider(
                new DateTimeOffset(2026, 8, 24, 22, 30, 0, TimeSpan.Zero),
                localTimeZone));

        var saleNumber = await generator.GenerateAsync();

        Assert.Equal("20260825-0001", saleNumber);
    }

    private static StorePosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new StorePosDbContext(options);
    }

    private sealed class FixedTimeProvider(
        DateTimeOffset utcNow,
        TimeZoneInfo? localTimeZone = null) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => localTimeZone ?? TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
