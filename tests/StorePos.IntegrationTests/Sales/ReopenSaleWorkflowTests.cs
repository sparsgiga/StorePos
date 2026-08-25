using Microsoft.EntityFrameworkCore;
using StorePos.Application.Sales.Commands.Reopen;
using StorePos.Application.Sales.Queries.GetDrafts;
using StorePos.Application.Sales.Queries.GetHistory;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;
using StorePos.Persistence;
using StorePos.Persistence.Context;
using StorePos.Persistence.Queries;
using StorePos.Persistence.Repositories;

namespace StorePos.IntegrationTests.Sales;

public sealed class ReopenSaleWorkflowTests
{
    [Fact]
    public async Task Reopen_PersistsDraftPreservesDataRemovesPaymentsAndUpdatesQueries()
    {
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        long saleId;
        long itemId;

        await using (var context = new StorePosDbContext(options))
        {
            var repository = new SaleRepository(context);
            var unitOfWork = new UnitOfWork(context);
            var sale = Sale.Create(
                "20260825-0001",
                customerName: "გიორგი",
                customerIdentificationNumber: "01000000000",
                comment: "უცვლელი");
            await repository.AddAsync(sale);
            await unitOfWork.SaveChangesAsync();

            var item = sale.AddManualItem("პროდუქტი", 2m, 5m);
            sale.Complete(
                [
                    new SalePaymentAllocation(PaymentType.Cash, 4m),
                    new SalePaymentAllocation(PaymentType.Card, 6m)
                ],
                new DateTime(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc));
            await unitOfWork.SaveChangesAsync();
            saleId = sale.Id;
            itemId = item.Id;

            context.ChangeTracker.Clear();
            var result = await new ReopenSaleCommandHandler(repository, unitOfWork)
                .Handle(new ReopenSaleCommand(saleId), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(SaleStatus.Draft, result.Status);
        }

        await using (var restartedContext = new StorePosDbContext(options))
        {
            var persistedSale = await restartedContext.Sales
                .Include(sale => sale.Items)
                .Include(sale => sale.Payments)
                .SingleAsync(sale => sale.Id == saleId);

            Assert.Equal(SaleStatus.Draft, persistedSale.Status);
            Assert.Null(persistedSale.DateCompleted);
            Assert.Null(persistedSale.DateCancelled);
            Assert.Equal(10m, persistedSale.TotalAmount);
            Assert.Equal("გიორგი", persistedSale.CustomerName);
            Assert.Equal("01000000000", persistedSale.CustomerIdentificationNumber);
            Assert.Equal("უცვლელი", persistedSale.Comment);
            Assert.Equal(itemId, Assert.Single(persistedSale.Items).Id);
            Assert.Empty(persistedSale.Payments);
            Assert.Empty(await restartedContext.SalePayments.ToListAsync());

            restartedContext.ChangeTracker.Clear();
            var repository = new SaleRepository(restartedContext);
            var drafts = await new GetDraftSalesQueryHandler(repository)
                .Handle(new GetDraftSalesQuery(), CancellationToken.None);
            Assert.Equal(saleId, Assert.Single(drafts).Id);

            var history = await new GetSalesHistoryQueryHandler(
                    new SalesReadService(restartedContext))
                .Handle(
                    new GetSalesHistoryQuery(Status: SaleStatus.Completed),
                    CancellationToken.None);
            Assert.Empty(history.Items);
        }
    }
}
