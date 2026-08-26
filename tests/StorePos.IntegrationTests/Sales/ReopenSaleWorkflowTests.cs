using Microsoft.EntityFrameworkCore;
using StorePos.Application.Sales.Commands.Cancel;
using StorePos.Application.Sales.Commands.Complete;
using StorePos.Application.Sales.Commands.Reopen;
using StorePos.Application.Sales.Commands.UpdateItem;
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
    public async Task CompleteReopenEditAndCompleteAgain_PreservesOldPaymentRows()
    {
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        long saleId;
        long itemId;
        Dictionary<long, (decimal Amount, PaymentType Type, DateTime DateCreated)> oldPayments;

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
            saleId = sale.Id;

            var firstCompletion = await new CompleteSaleCommandHandler(
                    repository,
                    unitOfWork,
                    TimeProvider.System)
                .Handle(
                    new CompleteSaleCommand(
                        saleId,
                        [
                            new CompleteSalePayment(PaymentType.Cash, 4m),
                            new CompleteSalePayment(PaymentType.Card, 6m)
                        ]),
                    CancellationToken.None);
            Assert.NotNull(firstCompletion);
            itemId = item.Id;
            oldPayments = sale.Payments.ToDictionary(
                payment => payment.Id,
                payment => (payment.Amount, payment.PaymentType, payment.DateCreated));

            context.ChangeTracker.Clear();
            var result = await new ReopenSaleCommandHandler(repository, unitOfWork)
                .Handle(new ReopenSaleCommand(saleId), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(SaleStatus.Draft, result.Status);

            context.ChangeTracker.Clear();
            var updated = await new UpdateSaleItemCommandHandler(repository, unitOfWork)
                .Handle(
                    new UpdateSaleItemCommand(
                        saleId,
                        itemId,
                        "პროდუქტი",
                        2m,
                        10m),
                    CancellationToken.None);
            Assert.NotNull(updated);

            context.ChangeTracker.Clear();
            var secondCompletion = await new CompleteSaleCommandHandler(
                    repository,
                    unitOfWork,
                    TimeProvider.System)
                .Handle(
                    new CompleteSaleCommand(
                        saleId,
                        [
                            new CompleteSalePayment(PaymentType.Cash, 5m),
                            new CompleteSalePayment(PaymentType.Card, 15m)
                        ]),
                    CancellationToken.None);
            Assert.NotNull(secondCompletion);
            Assert.Equal(20m, secondCompletion.PaidAmount);
        }

        await using (var restartedContext = new StorePosDbContext(options))
        {
            var persistedSale = await restartedContext.Sales
                .Include(sale => sale.Items)
                .Include(sale => sale.Payments)
                .SingleAsync(sale => sale.Id == saleId);

            Assert.Equal(SaleStatus.Completed, persistedSale.Status);
            Assert.Equal(2, persistedSale.CompletionVersion);
            Assert.NotNull(persistedSale.DateCompleted);
            Assert.Null(persistedSale.DateCancelled);
            Assert.Equal(20m, persistedSale.TotalAmount);
            Assert.Equal("გიორგი", persistedSale.CustomerName);
            Assert.Equal("01000000000", persistedSale.CustomerIdentificationNumber);
            Assert.Equal("უცვლელი", persistedSale.Comment);
            Assert.Equal(itemId, Assert.Single(persistedSale.Items).Id);
            Assert.Equal(4, persistedSale.Payments.Count);
            Assert.Equal(2, persistedSale.Payments.Count(payment =>
                payment.CompletionVersion == 1));
            Assert.Equal(2, persistedSale.Payments.Count(payment =>
                payment.CompletionVersion == 2));
            foreach (var oldPayment in oldPayments)
            {
                var persisted = persistedSale.Payments.Single(payment =>
                    payment.Id == oldPayment.Key);
                Assert.Equal(oldPayment.Value.Amount, persisted.Amount);
                Assert.Equal(oldPayment.Value.Type, persisted.PaymentType);
                Assert.Equal(oldPayment.Value.DateCreated, persisted.DateCreated);
                Assert.Equal(1, persisted.CompletionVersion);
            }
            Assert.Equal(20m, persistedSale.PaidAmount);
            Assert.Equal(0m, persistedSale.OutstandingAmount);

            restartedContext.ChangeTracker.Clear();
            var repository = new SaleRepository(restartedContext);
            var history = await new GetSalesHistoryQueryHandler(
                    new SalesReadService(restartedContext))
                .Handle(
                    new GetSalesHistoryQuery(Status: SaleStatus.Completed),
                    CancellationToken.None);
            var historySale = Assert.Single(history.Items);
            Assert.Equal(5m, historySale.CashAmount);
            Assert.Equal(15m, historySale.CardAmount);
            Assert.Equal(20m, historySale.PaidAmount);
        }
    }

    [Fact]
    public async Task CompleteReopenAndCancel_PreservesPaymentsAndFinancialSnapshot()
    {
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new StorePosDbContext(options);
        var repository = new SaleRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var sale = Sale.Create("20260825-0002");
        await repository.AddAsync(sale);
        await unitOfWork.SaveChangesAsync();
        sale.AddManualItem("Item", 1m, 10m);
        sale.Complete([new SalePaymentAllocation(PaymentType.Cash, 10m)], DateTime.Now);
        await unitOfWork.SaveChangesAsync();
        var paymentId = Assert.Single(sale.Payments).Id;

        context.ChangeTracker.Clear();
        await new ReopenSaleCommandHandler(repository, unitOfWork)
            .Handle(new ReopenSaleCommand(sale.Id), CancellationToken.None);
        context.ChangeTracker.Clear();
        var result = await new CancelSaleCommandHandler(
                repository,
                unitOfWork,
                TimeProvider.System)
            .Handle(new CancelSaleCommand(sale.Id), CancellationToken.None);

        Assert.NotNull(result);
        context.ChangeTracker.Clear();
        var persisted = await context.Sales
            .Include(current => current.Payments)
            .SingleAsync(current => current.Id == sale.Id);
        Assert.Equal(SaleStatus.Cancelled, persisted.Status);
        Assert.Equal(paymentId, Assert.Single(persisted.Payments).Id);
        Assert.Equal(10m, persisted.PaidAmount);
        Assert.Equal(0m, persisted.OutstandingAmount);
        var details = await new SalesReadService(context).GetDetailsAsync(sale.Id);
        Assert.NotNull(details);
        Assert.Equal(10m, details.PaidAmount);
        Assert.Equal(0m, details.OutstandingAmount);
        Assert.Single(details.Payments);
    }
}
