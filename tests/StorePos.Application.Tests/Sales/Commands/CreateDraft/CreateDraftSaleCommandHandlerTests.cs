using StorePos.Application.Common.Interfaces;
using StorePos.Application.Sales.Commands.CreateDraft;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Tests.Sales.Commands.CreateDraft;

public sealed class CreateDraftSaleCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesAndPersistsDraftSale()
    {
        const string expectedSaleNumber = "20260824-0001";
        var repository = new FakeSaleRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateDraftSaleCommandHandler(
            new StubSaleNumberGenerator(expectedSaleNumber),
            repository,
            unitOfWork);

        var command = new CreateDraftSaleCommand(
            CashierId: 12,
            CustomerName: "Customer",
            CustomerIdentificationNumber: "12345678901",
            Comment: "Draft sale");

        var result = await handler.Handle(command, CancellationToken.None);

        var sale = Assert.IsType<Sale>(repository.AddedSale);
        Assert.Equal(expectedSaleNumber, result.SaleNumber);
        Assert.Equal(expectedSaleNumber, sale.SaleNumber);
        Assert.Equal(SaleStatus.Draft, sale.Status);
        Assert.Equal(command.CashierId, sale.CashierId);
        Assert.Equal(command.CustomerName, sale.CustomerName);
        Assert.Equal(command.CustomerIdentificationNumber, sale.CustomerIdentificationNumber);
        Assert.Equal(command.Comment, sale.Comment);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private sealed class StubSaleNumberGenerator(string saleNumber) : ISaleNumberGenerator
    {
        public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(saleNumber);
    }

    private sealed class FakeSaleRepository : ISaleRepository
    {
        public Sale? AddedSale { get; private set; }

        public IQueryable<Sale> Query() => Array.Empty<Sale>().AsQueryable();

        public Task<IReadOnlyList<Sale>> GetDraftsAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Sale>>(Array.Empty<Sale>());

        public Task<Sale?> GetDraftForUpdateAsync(
            long saleId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Sale?>(null);

        public Task<Sale?> GetDraftForMetadataUpdateAsync(
            long saleId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Sale?>(null);

        public Task<Sale?> GetDraftDetailsAsync(
            long saleId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Sale?>(null);

        public Task<Sale?> GetCompletedForUpdateAsync(
            long saleId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Sale?>(null);

        public Task<Sale?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Sale?>(null);

        public Task AddAsync(
            Sale entity,
            CancellationToken cancellationToken = default)
        {
            AddedSale = entity;
            return Task.CompletedTask;
        }

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
}
