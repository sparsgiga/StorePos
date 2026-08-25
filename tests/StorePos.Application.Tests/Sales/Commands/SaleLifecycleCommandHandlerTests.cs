using StorePos.Application.Sales.Commands.Cancel;
using StorePos.Application.Sales.Commands.Complete;
using StorePos.Application.Sales.Commands.Reopen;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Tests.Sales.Commands;

public sealed class SaleLifecycleCommandHandlerTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 8, 25, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CompleteHandler_CompletesAggregateAndSavesOnce()
    {
        var sale = Sale.Create("20260825-0001");
        sale.AddManualItem("A", 1m, 200m);
        sale.AddManualItem("B", 1m, 72m);
        var repository = new FakeSaleRepository(sale);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CompleteSaleCommandHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(FixedNow));

        var result = await handler.Handle(
            new CompleteSaleCommand(
                1,
                [
                    new CompleteSalePayment(PaymentType.Cash, 100m),
                    new CompleteSalePayment(PaymentType.Card, 172m)
                ]),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SaleStatus.Completed, result.Status);
        Assert.Equal(FixedNow.UtcDateTime, result.DateCompleted);
        Assert.Equal(2, sale.Payments.Count);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task CancelHandler_CancelsAggregateAndSavesOnce()
    {
        var sale = Sale.Create("20260825-0001");
        var repository = new FakeSaleRepository(sale);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CancelSaleCommandHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(FixedNow));

        var result = await handler.Handle(
            new CancelSaleCommand(1),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SaleStatus.Cancelled, result.Status);
        Assert.Equal(FixedNow.UtcDateTime, result.DateCancelled);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ReopenHandler_ReopensCompletedAggregateAndSavesOnce()
    {
        var sale = Sale.Create("20260825-0001");
        sale.AddManualItem("A", 1m, 10m);
        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 10m)],
            FixedNow.UtcDateTime);
        var repository = new FakeSaleRepository(sale);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ReopenSaleCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new ReopenSaleCommand(1),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SaleStatus.Draft, result.Status);
        Assert.Empty(sale.Payments);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private sealed class FakeSaleRepository(Sale? sale) : ISaleRepository
    {
        public IQueryable<Sale> Query() => Array.Empty<Sale>().AsQueryable();

        public Task<IReadOnlyList<Sale>> GetDraftsAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Sale>>(Array.Empty<Sale>());

        public Task<Sale?> GetDraftForUpdateAsync(
            long saleId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(sale?.Status == SaleStatus.Draft ? sale : null);

        public Task<Sale?> GetDraftForInfoUpdateAsync(
            long saleId,
            CancellationToken cancellationToken = default)
            => GetDraftForUpdateAsync(saleId, cancellationToken);

        public Task<Sale?> GetDraftDetailsAsync(
            long saleId,
            CancellationToken cancellationToken = default)
            => GetDraftForUpdateAsync(saleId, cancellationToken);

        public Task<Sale?> GetCompletedForReopenAsync(
            long saleId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(sale?.Status == SaleStatus.Completed ? sale : null);

        public Task<Sale?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken = default)
            => Task.FromResult(sale);

        public Task AddAsync(
            Sale entity,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Update(Sale entity)
        {
        }

        public void Remove(Sale entity)
        {
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
