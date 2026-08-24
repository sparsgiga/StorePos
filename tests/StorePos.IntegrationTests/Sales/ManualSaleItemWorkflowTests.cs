using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using StorePos.Application.Sales.Commands.AddManualItem;
using StorePos.Application.Sales.Queries.GetDraftDetails;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Persistence;
using StorePos.Persistence.Context;
using StorePos.Persistence.Repositories;

namespace StorePos.IntegrationTests.Sales;

public sealed class ManualSaleItemWorkflowTests
{
    [Fact]
    public async Task AddManualItem_PersistsItemAndTotal_AndDetailsReturnsItem()
    {
        await using var context = CreateContext();
        var repository = new SaleRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var sale = Sale.Create("20260824-0001");

        await repository.AddAsync(sale);
        await unitOfWork.SaveChangesAsync();

        var addHandler = new AddManualSaleItemCommandHandler(repository, unitOfWork);
        var result = await addHandler.Handle(
            new AddManualSaleItemCommand(
                sale.Id,
                "მუხლი 20",
                2m,
                0.50000m,
                "Manual item"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEqual(0, result.SaleItemId);
        Assert.Equal(1.00000m, result.LineTotal);
        Assert.Equal(1.00000m, result.SaleTotalAmount);

        context.ChangeTracker.Clear();

        var persistedItem = await context.SaleItems.SingleAsync();
        var persistedSale = await context.Sales.SingleAsync();

        Assert.Null(persistedItem.ProductId);
        Assert.True(persistedItem.IsManual);
        Assert.Equal(1.00000m, persistedItem.LineTotal);
        Assert.Equal(1.00000m, persistedSale.TotalAmount);

        var detailsHandler = new GetDraftSaleDetailsQueryHandler(repository);
        var details = await detailsHandler.Handle(
            new GetDraftSaleDetailsQuery(sale.Id),
            CancellationToken.None);

        Assert.NotNull(details);
        var detailsItem = Assert.Single(details.Items);
        Assert.Equal(persistedItem.Id, detailsItem.Id);
        Assert.Equal("მუხლი 20", detailsItem.ProductName);
        Assert.Equal(1.00000m, details.TotalAmount);
    }

    [Fact]
    public async Task NewContext_ReloadsDraftWithPersistedItemsAndTotal()
    {
        var databaseName = Guid.NewGuid().ToString();
        var databaseRoot = new InMemoryDatabaseRoot();
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .Options;

        long saleId;

        await using (var firstContext = new StorePosDbContext(options))
        {
            var repository = new SaleRepository(firstContext);
            var unitOfWork = new UnitOfWork(firstContext);
            var sale = Sale.Create("20260824-0001");

            await repository.AddAsync(sale);
            await unitOfWork.SaveChangesAsync();

            var addHandler = new AddManualSaleItemCommandHandler(repository, unitOfWork);
            await addHandler.Handle(
                new AddManualSaleItemCommand(sale.Id, "მუხლი 20", 2m, 0.50000m),
                CancellationToken.None);
            await addHandler.Handle(
                new AddManualSaleItemCommand(sale.Id, "შურუფი", 10m, 0.20000m),
                CancellationToken.None);

            saleId = sale.Id;
        }

        await using var reloadedContext = new StorePosDbContext(options);
        var reloadedRepository = new SaleRepository(reloadedContext);
        var detailsHandler = new GetDraftSaleDetailsQueryHandler(reloadedRepository);

        var details = await detailsHandler.Handle(
            new GetDraftSaleDetailsQuery(saleId),
            CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(2, details.Items.Count);
        Assert.Equal(3.00000m, details.TotalAmount);
        Assert.Equal(
            ["მუხლი 20", "შურუფი"],
            details.Items.Select(item => item.ProductName));
    }

    private static StorePosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new StorePosDbContext(options);
    }
}
