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
    [Fact]
    public async Task History_FiltersPaginatesAndSortsCompletedSales()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context);
        var handler = new GetSalesHistoryQueryHandler(
            new SalesReadService(context));

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
            new SalesReadService(context));

        var byNumber = await handler.Handle(
            new GetSalesHistoryQuery(SaleNumber: "0002"),
            CancellationToken.None);
        var byCustomer = await handler.Handle(
            new GetSalesHistoryQuery(CustomerName: "ნინო"),
            CancellationToken.None);
        var cancelled = await handler.Handle(
            new GetSalesHistoryQuery(Status: SaleStatus.Cancelled),
            CancellationToken.None);
        var drafts = await handler.Handle(
            new GetSalesHistoryQuery(Status: SaleStatus.Draft),
            CancellationToken.None);

        Assert.Equal("20260825-0002", Assert.Single(byNumber.Items).SaleNumber);
        Assert.All(byCustomer.Items, sale => Assert.Contains("ნინო", sale.CustomerName));
        var cancelledSale = Assert.Single(cancelled.Items);
        Assert.Equal(SaleStatus.Cancelled, cancelledSale.Status);
        Assert.Equal(0m, cancelledSale.CashAmount);
        Assert.Equal(0m, cancelledSale.PaidAmount);
        Assert.Equal(0m, cancelledSale.OutstandingAmount);
        Assert.False(cancelledSale.HasDebt);
        var draftSale = Assert.Single(drafts.Items);
        Assert.Equal(0m, draftSale.CashAmount);
        Assert.Equal(0m, draftSale.PaidAmount);
        Assert.Equal(0m, draftSale.OutstandingAmount);
        Assert.False(draftSale.HasDebt);
    }

    [Fact]
    public async Task Details_ReturnsItemsAndPaymentsForCompletedSale()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context);
        var handler = new GetSaleDetailsQueryHandler(
            new SalesReadService(context));

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

    [Fact]
    public async Task HistoryAndDetails_UseOnlyCurrentCompletionVersionAndKeepPreviousPayments()
    {
        await using var context = CreateContext();
        var sale = Sale.Create("20260825-0090");
        await context.Sales.AddAsync(sale);
        await context.SaveChangesAsync();
        var item = sale.AddManualItem("Item", 1m, 100m);
        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 100m)],
            new DateTime(2026, 8, 25, 10, 0, 0));
        await context.SaveChangesAsync();
        sale.Reopen();
        sale.UpdateItem(item.Id, item.ProductName, 1m, 120m);
        sale.Complete(
            [
                new SalePaymentAllocation(PaymentType.Cash, 50m),
                new SalePaymentAllocation(PaymentType.Card, 70m)
            ],
            new DateTime(2026, 8, 25, 11, 0, 0));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var readService = new SalesReadService(context);
        var history = await readService.GetHistoryAsync(
            new GetSalesHistoryQuery(SaleNumber: sale.SaleNumber),
            CancellationToken.None);
        var historySale = Assert.Single(history.Items);
        Assert.Equal(50m, historySale.CashAmount);
        Assert.Equal(70m, historySale.CardAmount);
        Assert.Equal(120m, historySale.PaidAmount);
        Assert.Equal(0m, historySale.OutstandingAmount);

        var details = await readService.GetDetailsAsync(sale.Id);
        Assert.NotNull(details);
        Assert.Equal(2, details.CompletionVersion);
        Assert.Equal(120m, details.PaidAmount);
        Assert.Equal(3, details.Payments.Count);
        Assert.Single(details.Payments, payment => payment.CompletionVersion == 1);
        Assert.Equal(2, details.Payments.Count(payment => payment.CompletionVersion == 2));
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
}
