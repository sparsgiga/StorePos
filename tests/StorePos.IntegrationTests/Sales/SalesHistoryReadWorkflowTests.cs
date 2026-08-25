using Microsoft.EntityFrameworkCore;
using StorePos.Application.Sales.Queries.GetDetails;
using StorePos.Application.Sales.Queries.GetHistory;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;
using StorePos.Persistence;
using StorePos.Persistence.Context;
using StorePos.Persistence.Queries;
using StorePos.Persistence.Repositories;

namespace StorePos.IntegrationTests.Sales;

public sealed class SalesHistoryReadWorkflowTests
{
    private static readonly TimeProvider TestTimeProvider =
        new FixedTimeProvider(new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task History_FiltersPaginatesAndSortsCompletedSales()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context);
        var handler = new GetSalesHistoryQueryHandler(
            new SalesReadService(context, TestTimeProvider));

        var firstPage = await handler.Handle(
            new GetSalesHistoryQuery(
                DateFrom: new DateOnly(2026, 8, 25),
                DateTo: new DateOnly(2026, 8, 25),
                Status: SaleStatus.Completed,
                PageNumber: 1,
                PageSize: 1),
            CancellationToken.None);
        var secondPage = await handler.Handle(
            new GetSalesHistoryQuery(
                DateFrom: new DateOnly(2026, 8, 25),
                DateTo: new DateOnly(2026, 8, 25),
                Status: SaleStatus.Completed,
                PageNumber: 2,
                PageSize: 1),
            CancellationToken.None);

        Assert.Equal(2, firstPage.TotalCount);
        Assert.Equal(1, firstPage.PageNumber);
        Assert.Equal(1, firstPage.PageSize);
        Assert.Equal(data.LatestCompletedId, Assert.Single(firstPage.Items).Id);
        Assert.Equal(data.EarlierCompletedId, Assert.Single(secondPage.Items).Id);
        Assert.All(firstPage.Items.Concat(secondPage.Items),
            sale => Assert.Equal(SaleStatus.Completed, sale.Status));
    }

    [Fact]
    public async Task History_SaleNumberCustomerAndStatusFiltersWork()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var handler = new GetSalesHistoryQueryHandler(
            new SalesReadService(context, TestTimeProvider));

        var byNumber = await handler.Handle(
            new GetSalesHistoryQuery(SaleNumber: "0002"),
            CancellationToken.None);
        var byCustomer = await handler.Handle(
            new GetSalesHistoryQuery(CustomerName: "ნინო"),
            CancellationToken.None);
        var cancelled = await handler.Handle(
            new GetSalesHistoryQuery(Status: SaleStatus.Cancelled),
            CancellationToken.None);

        Assert.Equal("20260825-0002", Assert.Single(byNumber.Items).SaleNumber);
        Assert.All(byCustomer.Items, sale => Assert.Contains("ნინო", sale.CustomerName));
        Assert.Equal(SaleStatus.Cancelled, Assert.Single(cancelled.Items).Status);
    }

    [Fact]
    public async Task Details_ReturnsItemsAndPaymentsForCompletedSale()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context);
        var handler = new GetSaleDetailsQueryHandler(
            new SalesReadService(context, TestTimeProvider));

        var details = await handler.Handle(
            new GetSaleDetailsQuery(data.LatestCompletedId),
            CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(SaleStatus.Completed, details.Status);
        Assert.Single(details.Items);
        Assert.Collection(
            details.Payments,
            payment =>
            {
                Assert.Equal(PaymentType.Cash, payment.PaymentType);
                Assert.Equal(4m, payment.Amount);
            },
            payment =>
            {
                Assert.Equal(PaymentType.Card, payment.PaymentType);
                Assert.Equal(6m, payment.Amount);
            });
    }

    private static async Task<SeededHistory> SeedAsync(StorePosDbContext context)
    {
        var repository = new SaleRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var oldCompleted = Sale.Create("20260824-0001", customerName: "გიორგი");
        var earlierCompleted = Sale.Create("20260825-0001", customerName: "ნინო პირველი");
        var latestCompleted = Sale.Create("20260825-0002", customerName: "ნინო მეორე");
        var cancelled = Sale.Create("20260825-0003", customerName: "ანა");
        var draft = Sale.Create("20260825-0004", customerName: "დავით");

        foreach (var sale in new[] { oldCompleted, earlierCompleted, latestCompleted, cancelled, draft })
        {
            await repository.AddAsync(sale);
        }
        await unitOfWork.SaveChangesAsync();

        oldCompleted.AddManualItem("ძველი", 1m, 5m);
        oldCompleted.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 5m)],
            new DateTime(2026, 8, 24, 10, 0, 0, DateTimeKind.Utc));

        earlierCompleted.AddManualItem("პირველი", 1m, 8m);
        earlierCompleted.Complete(
            [new SalePaymentAllocation(PaymentType.Card, 8m)],
            new DateTime(2026, 8, 25, 11, 0, 0, DateTimeKind.Utc));

        latestCompleted.AddManualItem("მეორე", 1m, 10m);
        latestCompleted.Complete(
            [
                new SalePaymentAllocation(PaymentType.Cash, 4m),
                new SalePaymentAllocation(PaymentType.Card, 6m)
            ],
            new DateTime(2026, 8, 25, 11, 0, 0, DateTimeKind.Utc));

        cancelled.AddManualItem("გაუქმებული", 1m, 3m);
        cancelled.Cancel(new DateTime(2026, 8, 25, 9, 0, 0, DateTimeKind.Utc));
        draft.AddManualItem("Draft", 1m, 2m);
        await unitOfWork.SaveChangesAsync();

        context.ChangeTracker.Clear();
        return new SeededHistory(earlierCompleted.Id, latestCompleted.Id);
    }

    private static StorePosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StorePosDbContext(options);
    }

    private sealed record SeededHistory(long EarlierCompletedId, long LatestCompletedId);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => now;
    }
}
