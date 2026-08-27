using Microsoft.EntityFrameworkCore;
using StorePos.Application.Common.Exceptions;
using StorePos.Application.Products.Commands.Create;
using StorePos.Domain.Aggregates.MeasurementUnit;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Interfaces;
using StorePos.Persistence.Context;
using StorePos.Persistence.Repositories;
using StorePos.Persistence.Sequences;
using StorePos.Persistence.Services;

namespace StorePos.IntegrationTests.Products;

public sealed class ManualProductCodeSequenceTests
{
    [Fact]
    public async Task Suggestion_ReturnsStoredCandidateWhenFreeAndIgnoresLargeOutlier()
    {
        await using var context = CreateContext();
        await SeedAsync(context, 9756, "75505");

        var suggestion = await new ManualProductCodeSequenceService(context)
            .GetSuggestedCodeAsync();

        Assert.Equal("9756", suggestion);
    }

    [Fact]
    public async Task Suggestion_SkipsContiguousOccupiedCodesFromStoredPosition()
    {
        await using var context = CreateContext();
        await SeedAsync(context, 9756, "9756", "9757", "9758", "75505");

        var suggestion = await new ManualProductCodeSequenceService(context)
            .GetSuggestedCodeAsync();

        Assert.Equal("9759", suggestion);
    }

    [Theory]
    [InlineData(9999, "9999")]
    [InlineData(10000, "10000", "10001")]
    public async Task Suggestion_CrossesFourDigitBoundaryNaturally(
        long nextCode,
        params string[] occupiedCodes)
    {
        await using var context = CreateContext();
        await SeedAsync(context, nextCode, occupiedCodes);

        var suggestion = await new ManualProductCodeSequenceService(context)
            .GetSuggestedCodeAsync();

        var expected = nextCode == 9999 ? "10000" : "10002";
        Assert.Equal(expected, suggestion);
    }

    [Fact]
    public async Task Suggestion_Returns9999WhenItIsFree()
    {
        await using var context = CreateContext();
        await SeedAsync(context, 9999);

        var suggestion = await new ManualProductCodeSequenceService(context)
            .GetSuggestedCodeAsync();

        Assert.Equal("9999", suggestion);
    }

    [Fact]
    public async Task Advance_SequentialCandidateMovesSequenceForward()
    {
        await using var context = CreateContext();
        await SeedAsync(context, 9756);
        var service = new ManualProductCodeSequenceService(context);

        await service.AdvanceIfConsumedAsync("9756");
        await context.SaveChangesAsync();

        Assert.Equal(9757, await ReadNextCodeAsync(context));
    }

    [Fact]
    public async Task CreateHandler_ConsumingSuggestionPersistsProductAndAdvancesSequence()
    {
        await using var context = CreateContext();
        var unit = MeasurementUnit.Create("Piece", "pc");
        await context.MeasurementUnits.AddAsync(unit);
        await context.ManualProductCodeSequences.AddAsync(
            ManualProductCodeSequence.Initialize(9756));
        await context.SaveChangesAsync();
        var handler = new CreateProductCommandHandler(
            new ProductRepository(context),
            new MeasurementUnitRepository(context),
            new ManualProductCodeSequenceService(context),
            new StorePos.Persistence.UnitOfWork(context));

        var result = await handler.Handle(
            new CreateProductCommand("9756", null, "Product", unit.Id, 1m),
            CancellationToken.None);

        Assert.Equal("9756", result.Code);
        Assert.Equal(9757, await ReadNextCodeAsync(context));
        Assert.Equal("9756", Assert.Single(await context.Products.ToArrayAsync()).Code);
    }

    [Fact]
    public async Task Advance_ManualOverrideDoesNotMoveSequence()
    {
        await using var context = CreateContext();
        await SeedAsync(context, 9756, "75505");
        var service = new ManualProductCodeSequenceService(context);

        await service.AdvanceIfConsumedAsync("75505");
        await context.SaveChangesAsync();

        Assert.Equal(9756, await ReadNextCodeAsync(context));
    }

    [Fact]
    public async Task Advance_StaleSequenceMovesPastConsumedEffectiveCandidate()
    {
        await using var context = CreateContext();
        await SeedAsync(context, 9756, "9756", "9757", "9758");
        var service = new ManualProductCodeSequenceService(context);

        await service.AdvanceIfConsumedAsync("9759");
        await context.SaveChangesAsync();

        Assert.Equal(9760, await ReadNextCodeAsync(context));
    }

    [Fact]
    public async Task CreateFailure_DoesNotPersistProductOrSequenceAdvance()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(databaseName))
        {
            var unit = MeasurementUnit.Create("Piece", "pc");
            await context.MeasurementUnits.AddAsync(unit);
            await context.ManualProductCodeSequences.AddAsync(
                ManualProductCodeSequence.Initialize(9756));
            await context.SaveChangesAsync();
            var handler = new CreateProductCommandHandler(
                new ProductRepository(context),
                new MeasurementUnitRepository(context),
                new ManualProductCodeSequenceService(context),
                new ThrowingUnitOfWork());

            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
                new CreateProductCommand("9756", null, "Product", unit.Id, 1m),
                CancellationToken.None));
        }

        await using var verification = CreateContext(databaseName);
        Assert.Empty(await verification.Products.ToArrayAsync());
        Assert.Equal(9756, await ReadNextCodeAsync(verification));
    }

    private static async Task SeedAsync(
        StorePosDbContext context,
        long nextCode,
        params string[] productCodes)
    {
        await context.ManualProductCodeSequences.AddAsync(
            ManualProductCodeSequence.Initialize(nextCode));
        foreach (var code in productCodes)
        {
            await context.Products.AddAsync(Product.Create(code, null, code, 1, 1m));
        }

        await context.SaveChangesAsync();
    }

    private static async Task<long> ReadNextCodeAsync(StorePosDbContext context)
    {
        context.ChangeTracker.Clear();
        return await context.ManualProductCodeSequences
            .AsNoTracking()
            .Select(sequence => sequence.NextCode)
            .SingleAsync();
    }

    private static StorePosDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
        return new StorePosDbContext(options);
    }

    private sealed class ThrowingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Simulated persistence failure.");

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
