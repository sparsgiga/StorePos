using StorePos.Application.Common.Behaviors;
using StorePos.Application.Common.Interfaces;
using StorePos.Application.Products.Queries.Search;
using StorePos.Application.Products.Services;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Tests.Products;

public sealed class ProductApplicationTests
{
    [Fact]
    public async Task Search_NormalQueryShorterThanTwoCharacters_ReturnsEmpty()
    {
        var readService = new StubProductReadService();
        var handler = new SearchProductsQueryHandler(readService);

        var result = await handler.Handle(
            new SearchProductsQuery("1"),
            CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal(0, readService.CallCount);
    }

    [Fact]
    public async Task Search_ExactQueryAllowsSingleCharacterAndClampsLimit()
    {
        var readService = new StubProductReadService();
        var handler = new SearchProductsQueryHandler(readService);

        await handler.Handle(
            new SearchProductsQuery(" 1 ", 100, ExactOnly: true),
            CancellationToken.None);

        Assert.Equal(1, readService.CallCount);
        Assert.Equal("1", readService.Query);
        Assert.Equal(SearchProductsQueryHandler.MaximumLimit, readService.Limit);
        Assert.True(readService.ExactOnly);
    }

    [Fact]
    public void ProductCodeGenerator_ReturnsExpectedUniqueFormat()
    {
        var generator = new GuidProductCodeGenerator();

        var first = generator.Generate();
        var second = generator.Generate();

        Assert.StartsWith(GuidProductCodeGenerator.Prefix, first);
        Assert.Equal(36, first.Length);
        Assert.Equal(first.ToUpperInvariant(), first);
        Assert.NotEqual(first, second);
        Assert.True(Guid.TryParseExact(first[GuidProductCodeGenerator.Prefix.Length..], "N", out _));
    }

    [Fact]
    public async Task TransactionBehavior_CommitsSuccessfulTransactionalRequest()
    {
        var unitOfWork = new RecordingUnitOfWork();
        var behavior = new TransactionBehavior<TransactionalRequest, int>(unitOfWork);

        var result = await behavior.Handle(
            new TransactionalRequest(),
            _ => Task.FromResult(42),
            CancellationToken.None);

        Assert.Equal(42, result);
        Assert.Equal(["begin", "commit"], unitOfWork.Calls);
    }

    [Fact]
    public async Task TransactionBehavior_RollsBackFailedTransactionalRequest()
    {
        var unitOfWork = new RecordingUnitOfWork();
        var behavior = new TransactionBehavior<TransactionalRequest, int>(unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new TransactionalRequest(),
            _ => Task.FromException<int>(new InvalidOperationException("failure")),
            CancellationToken.None));

        Assert.Equal(["begin", "rollback"], unitOfWork.Calls);
    }

    private sealed class StubProductReadService : IProductReadService
    {
        public int CallCount { get; private set; }
        public string? Query { get; private set; }
        public int Limit { get; private set; }
        public bool ExactOnly { get; private set; }

        public Task<IReadOnlyList<ProductSearchResult>> SearchAsync(
            string query,
            int limit,
            bool exactOnly,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Query = query;
            Limit = limit;
            ExactOnly = exactOnly;
            return Task.FromResult<IReadOnlyList<ProductSearchResult>>([]);
        }
    }

    private sealed record TransactionalRequest : ITransactionalRequest;

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public List<string> Calls { get; } = [];

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            Calls.Add("begin");
            return Task.CompletedTask;
        }

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            Calls.Add("commit");
            return Task.CompletedTask;
        }

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            Calls.Add("rollback");
            return Task.CompletedTask;
        }
    }
}
