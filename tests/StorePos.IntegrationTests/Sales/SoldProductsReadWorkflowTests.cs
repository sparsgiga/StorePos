using System.Reflection;
using Microsoft.EntityFrameworkCore;
using StorePos.Application.Sales.Queries.GetSoldProducts;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;
using StorePos.Persistence;
using StorePos.Persistence.Context;
using StorePos.Persistence.Queries;
using StorePos.Persistence.Repositories;

namespace StorePos.IntegrationTests.Sales;

public sealed class SoldProductsReadWorkflowTests
{
    private static readonly TimeProvider TestTimeProvider =
        new FixedTimeProvider(new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task SoldProducts_ReturnsOnlyCompletedItemsAndPaginatesNewestFirst()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context);
        var handler = CreateHandler(context);

        var firstPage = await handler.Handle(
            new GetSoldProductsQuery(PageNumber: 1, PageSize: 1),
            CancellationToken.None);
        var secondPage = await handler.Handle(
            new GetSoldProductsQuery(PageNumber: 2, PageSize: 1),
            CancellationToken.None);

        Assert.Equal(2, firstPage.TotalCount);
        Assert.Equal(data.NewestCompletedItemId, Assert.Single(firstPage.Items).SaleItemId);
        Assert.Equal(data.OldCompletedItemId, Assert.Single(secondPage.Items).SaleItemId);
        Assert.DoesNotContain(firstPage.Items.Concat(secondPage.Items),
            item => item.ProductName is "Draft item" or "Cancelled item");
    }

    [Theory]
    [InlineData("სმესიკა")]
    [InlineData("CAT-100")]
    [InlineData("48600001")]
    public async Task SoldProducts_ProductSearchMatchesSnapshots(string search)
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new GetSoldProductsQuery(ProductSearch: search),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal("სმესიკა", item.ProductName);
        Assert.False(item.IsManual);
    }

    [Fact]
    public async Task SoldProducts_SaleCustomerManualAndDateFiltersWork()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var handler = CreateHandler(context);

        var catalog = await handler.Handle(
            new GetSoldProductsQuery(
                DateFrom: new DateOnly(2026, 8, 25),
                DateTo: new DateOnly(2026, 8, 25),
                SaleNumber: "0002",
                CustomerName: "ნინო",
                IsManual: false),
            CancellationToken.None);
        var manual = await handler.Handle(
            new GetSoldProductsQuery(IsManual: true),
            CancellationToken.None);

        Assert.Equal("სმესიკა", Assert.Single(catalog.Items).ProductName);
        Assert.True(Assert.Single(manual.Items).IsManual);
    }

    private static GetSoldProductsQueryHandler CreateHandler(StorePosDbContext context)
        => new(new SalesReadService(context, TestTimeProvider));

    private static async Task<SeededItems> SeedAsync(StorePosDbContext context)
    {
        var repository = new SaleRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var oldCompleted = Sale.Create("20260824-0001", customerName: "გიორგი");
        var newestCompleted = Sale.Create("20260825-0002", customerName: "ნინო");
        var draft = Sale.Create("20260825-0003");
        var cancelled = Sale.Create("20260825-0004");

        foreach (var sale in new[] { oldCompleted, newestCompleted, draft, cancelled })
        {
            await repository.AddAsync(sale);
        }
        await unitOfWork.SaveChangesAsync();

        var oldItem = oldCompleted.AddManualItem("მუხლი", 1m, 5m);
        oldCompleted.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 5m)],
            new DateTime(2026, 8, 24, 10, 0, 0, DateTimeKind.Utc));

        var newestItem = newestCompleted.AddManualItem("სმესიკა", 2m, 7m);
        SetPrivateProperty(newestItem, nameof(SaleItem.ProductCode), "CAT-100");
        SetPrivateProperty(newestItem, nameof(SaleItem.Barcode), "48600001");
        SetPrivateProperty(newestItem, nameof(SaleItem.IsManual), false);
        newestCompleted.Complete(
            [new SalePaymentAllocation(PaymentType.Card, 14m)],
            new DateTime(2026, 8, 25, 11, 0, 0, DateTimeKind.Utc));

        draft.AddManualItem("Draft item", 1m, 2m);
        cancelled.AddManualItem("Cancelled item", 1m, 3m);
        cancelled.Cancel(new DateTime(2026, 8, 25, 9, 0, 0, DateTimeKind.Utc));
        await unitOfWork.SaveChangesAsync();

        context.ChangeTracker.Clear();
        return new SeededItems(oldItem.Id, newestItem.Id);
    }

    private static void SetPrivateProperty<T>(SaleItem item, string name, T value)
        => typeof(SaleItem)
            .GetProperty(name, BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(item, value);

    private static StorePosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StorePosDbContext(options);
    }

    private sealed record SeededItems(long OldCompletedItemId, long NewestCompletedItemId);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => now;
    }
}
