using StorePos.Application.Sales.Commands.Cancel;
using StorePos.Application.Sales.Commands.Complete;
using StorePos.Application.Sales.Commands.Reopen;
using StorePos.Application.Sales.Commands.AddDebtPayment;
using StorePos.Application.Sales.Commands.UpdateDiscount;
using StorePos.Application.Sales.Commands.UpdateFinancials;
using StorePos.Application.Common.Exceptions;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Tests.Sales.Commands;

public sealed class SaleLifecycleCommandHandlerTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 8, 25, 14, 0, 0, TimeSpan.Zero);
    private static readonly TimeZoneInfo TestLocalTimeZone =
        TimeZoneInfo.CreateCustomTimeZone(
            "Test Local",
            TimeSpan.FromHours(4),
            "Test Local",
            "Test Local");

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
        Assert.Equal(FixedNow.ToOffset(TimeSpan.FromHours(4)).DateTime, result.DateCompleted);
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
        Assert.Equal(FixedNow.ToOffset(TimeSpan.FromHours(4)).DateTime, result.DateCancelled);
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
        Assert.Single(sale.Payments);
        Assert.Equal(1, sale.CompletionVersion);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task CompleteReopenEditAndCompleteAgain_PreservesOldPaymentAndUsesNewVersion()
    {
        var sale = Sale.Create("20260825-0020");
        var item = sale.AddManualItem("A", 1m, 100m);
        var repository = new FakeSaleRepository(sale);
        var unitOfWork = new FakeUnitOfWork();
        var timeProvider = new FixedTimeProvider(FixedNow);

        await new CompleteSaleCommandHandler(repository, unitOfWork, timeProvider)
            .Handle(
                new CompleteSaleCommand(
                    1,
                    [new CompleteSalePayment(PaymentType.Cash, 100m)]),
                CancellationToken.None);
        var oldPayment = Assert.Single(sale.Payments);
        var oldAmount = oldPayment.Amount;

        await new ReopenSaleCommandHandler(repository, unitOfWork)
            .Handle(new ReopenSaleCommand(1), CancellationToken.None);
        sale.UpdateItem(item.Id, item.ProductName, 1m, 120m);
        var result = await new CompleteSaleCommandHandler(repository, unitOfWork, timeProvider)
            .Handle(
                new CompleteSaleCommand(
                    1,
                    [
                        new CompleteSalePayment(PaymentType.Cash, 50m),
                        new CompleteSalePayment(PaymentType.Card, 70m)
                    ]),
                CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, sale.CompletionVersion);
        Assert.Equal(3, sale.Payments.Count);
        Assert.Same(oldPayment, sale.Payments.Single(payment =>
            payment.CompletionVersion == 1));
        Assert.Equal(oldAmount, oldPayment.Amount);
        Assert.Equal(120m, result.PaidAmount);
        Assert.Equal(0m, result.OutstandingAmount);
    }

    [Fact]
    public async Task AddDebtPaymentHandler_UpdatesFinancialStateAndSavesOnce()
    {
        var sale = Sale.Create("20260825-0001");
        sale.AssignCustomer(10, "Customer", null);
        sale.AddManualItem("A", 1m, 200m);
        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 100m)],
            FixedNow.DateTime,
            allowDebt: true);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AddDebtPaymentCommandHandler(
            new FakeSaleRepository(sale),
            unitOfWork);

        var result = await handler.Handle(
            new AddDebtPaymentCommand(1, Guid.NewGuid(), PaymentType.Cash, 60m),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(160m, result.PaidAmount);
        Assert.Equal(40m, result.OutstandingAmount);
        Assert.True(result.HasDebt);
        Assert.Equal(SalePaymentKind.DebtRepayment, result.Payment.PaymentKind);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task CompleteHandler_MapsLostRowVersionRaceToSaleConflict()
    {
        var sale = Sale.Create("20260825-0030");
        sale.AddManualItem("A", 1m, 10m);
        var handler = new CompleteSaleCommandHandler(
            new FakeSaleRepository(sale),
            new FakeUnitOfWork(throwConcurrency: true),
            new FixedTimeProvider(FixedNow));

        var exception = await Assert.ThrowsAsync<SaleOperationConflictException>(() =>
            handler.Handle(
                new CompleteSaleCommand(
                    1,
                    [new CompleteSalePayment(PaymentType.Cash, 10m)]),
                CancellationToken.None));

        Assert.Contains("ფინანსური მდგომარეობა შეიცვალა", exception.Message);
    }

    [Fact]
    public async Task UpdateDiscountHandler_ReturnsCanonicalFinancialState()
    {
        var sale = Sale.Create("20260828-0001");
        sale.AddManualItem("A", 1m, 601m);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateSaleDiscountCommandHandler(
            new FakeSaleRepository(sale),
            unitOfWork);

        var result = await handler.Handle(
            new UpdateSaleDiscountCommand(1, 1m),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(601m, result.Subtotal);
        Assert.Equal(1m, result.DiscountAmount);
        Assert.Equal(600m, result.TotalAmount);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task InlineLineTotalHandler_DerivesCanonicalUnitPrice()
    {
        var sale = Sale.Create("20260828-0002");
        var item = sale.AddManualItem("A", 3m, 10m);
        var handler = new UpdateSaleItemFinancialsCommandHandler(
            new FakeSaleRepository(sale),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new UpdateSaleItemFinancialsCommand(1, item.Id, null, null, 27m),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(9m, result.UnitPrice);
        Assert.Equal(27m, result.LineTotal);
        Assert.Equal(27m, result.SaleSubtotal);
        Assert.False(result.RequestedLineTotalAdjusted);
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

        public Task<Sale?> GetDraftForMetadataUpdateAsync(
            long saleId,
            CancellationToken cancellationToken = default)
            => GetDraftForUpdateAsync(saleId, cancellationToken);

        public Task<Sale?> GetDraftDetailsAsync(
            long saleId,
            CancellationToken cancellationToken = default)
            => GetDraftForUpdateAsync(saleId, cancellationToken);

        public Task<Sale?> GetCompletedForUpdateAsync(
            long saleId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(sale?.Status == SaleStatus.Completed ? sale : null);

        public Task<Sale?> GetByDebtPaymentOperationIdAsync(
            Guid operationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(sale?.Payments.Any(payment =>
                    payment.OperationId == operationId) == true
                ? sale
                : null);

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

    private sealed class FakeUnitOfWork(bool throwConcurrency = false) : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            if (throwConcurrency)
            {
                throw new PersistenceConcurrencyException("Lost update.");
            }

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
        public override TimeZoneInfo LocalTimeZone => TestLocalTimeZone;

        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
