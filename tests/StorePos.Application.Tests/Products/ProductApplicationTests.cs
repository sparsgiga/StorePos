using StorePos.Application.Common.Behaviors;
using StorePos.Application.Common.Interfaces;
using StorePos.Application.Products.Commands.CreateAndAddToSale;
using StorePos.Application.Products.Queries.GetCreationDefaults;
using StorePos.Application.Products.Queries.Search;
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
    public async Task CreationDefaults_ReturnsPersistenceSuggestionAndSemanticUnit()
    {
        var expected = new ProductCreationDefaultsResult(
            "10526",
            24,
            "ცალი",
            "ც",
            null);
        var handler = new GetProductCreationDefaultsQueryHandler(
            new StubProductCreationDefaultsReadService(expected));

        var result = await handler.Handle(
            new GetProductCreationDefaultsQuery(),
            CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CreateProductValidator_RequiresAsciiNumericProductCode()
    {
        var validator = new CreateProductAndAddToSaleCommandValidator();

        var valid = validator.Validate(new CreateProductAndAddToSaleCommand(
            1,
            "20000",
            "Cement",
            "0000000200004",
            24,
            1m,
            2m));
        var alphanumeric = validator.Validate(new CreateProductAndAddToSaleCommand(
            1,
            "PRD-1",
            "Cement",
            "0000000200004",
            24,
            1m,
            2m));
        var missingBarcode = validator.Validate(new CreateProductAndAddToSaleCommand(
            1,
            "20000",
            "Cement",
            null!,
            24,
            1m,
            2m));

        Assert.True(valid.IsValid);
        Assert.False(alphanumeric.IsValid);
        Assert.Contains(
            alphanumeric.Errors,
            error => error.PropertyName == "ProductCode");
        Assert.False(missingBarcode.IsValid);
        Assert.Contains(
            missingBarcode.Errors,
            error => error.PropertyName == "Barcode");
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

    private sealed class StubProductCreationDefaultsReadService(
        ProductCreationDefaultsResult result)
        : IProductCreationDefaultsReadService
    {
        public Task<ProductCreationDefaultsResult> GetAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult(result);
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
