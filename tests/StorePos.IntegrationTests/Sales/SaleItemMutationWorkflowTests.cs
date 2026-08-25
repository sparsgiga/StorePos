using Microsoft.EntityFrameworkCore;
using StorePos.Application.Sales.Commands.AddManualItem;
using StorePos.Application.Sales.Commands.RemoveItem;
using StorePos.Application.Sales.Commands.UpdateItem;
using StorePos.Application.Sales.Queries.GetDraftDetails;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Persistence;
using StorePos.Persistence.Context;
using StorePos.Persistence.Repositories;

namespace StorePos.IntegrationTests.Sales;

public sealed class SaleItemMutationWorkflowTests
{
    [Fact]
    public async Task UpdateThenRemove_PersistsItemsAndCorrectTotal()
    {
        await using var context = CreateContext();
        var repository = new SaleRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var sale = Sale.Create("20260825-0001");

        await repository.AddAsync(sale);
        await unitOfWork.SaveChangesAsync();

        var addHandler = new AddManualSaleItemCommandHandler(repository, unitOfWork);
        var first = await addHandler.Handle(
            new AddManualSaleItemCommand(sale.Id, "მუხლი 20", 2m, 0.50m),
            CancellationToken.None);
        var second = await addHandler.Handle(
            new AddManualSaleItemCommand(sale.Id, "შურუფი", 10m, 0.20m),
            CancellationToken.None);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(3m, second.SaleTotalAmount);

        var updateHandler = new UpdateSaleItemCommandHandler(repository, unitOfWork);
        var updated = await updateHandler.Handle(
            new UpdateSaleItemCommand(
                sale.Id,
                first.SaleItemId,
                "მუხლი 20",
                4m,
                0.50m,
                "განახლებული"),
            CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(2m, updated.LineTotal);
        Assert.Equal(4m, updated.SaleTotalAmount);

        var removeHandler = new RemoveSaleItemCommandHandler(repository, unitOfWork);
        var removed = await removeHandler.Handle(
            new RemoveSaleItemCommand(sale.Id, second.SaleItemId),
            CancellationToken.None);

        Assert.NotNull(removed);
        Assert.Equal(2m, removed.SaleTotalAmount);

        context.ChangeTracker.Clear();

        var detailsHandler = new GetDraftSaleDetailsQueryHandler(repository);
        var details = await detailsHandler.Handle(
            new GetDraftSaleDetailsQuery(sale.Id),
            CancellationToken.None);

        Assert.NotNull(details);
        var remainingItem = Assert.Single(details.Items);
        Assert.Equal(first.SaleItemId, remainingItem.Id);
        Assert.Equal(4m, remainingItem.Quantity);
        Assert.Equal(0.50m, remainingItem.UnitPrice);
        Assert.Equal(2m, remainingItem.LineTotal);
        Assert.Equal("განახლებული", remainingItem.Comment);
        Assert.Equal(2m, details.TotalAmount);
        Assert.Single(await context.SaleItems.ToListAsync());
    }

    [Fact]
    public async Task UpdateAndRemove_InvalidItemId_ReturnNullWithoutChangingSale()
    {
        await using var context = CreateContext();
        var repository = new SaleRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var sale = Sale.Create("20260825-0001");

        await repository.AddAsync(sale);
        await unitOfWork.SaveChangesAsync();

        var addHandler = new AddManualSaleItemCommandHandler(repository, unitOfWork);
        await addHandler.Handle(
            new AddManualSaleItemCommand(sale.Id, "მუხლი", 1m, 2m),
            CancellationToken.None);

        var updateHandler = new UpdateSaleItemCommandHandler(repository, unitOfWork);
        var removeHandler = new RemoveSaleItemCommandHandler(repository, unitOfWork);

        var updateResult = await updateHandler.Handle(
            new UpdateSaleItemCommand(sale.Id, 999, "სხვა", 2m, 3m),
            CancellationToken.None);
        var removeResult = await removeHandler.Handle(
            new RemoveSaleItemCommand(sale.Id, 999),
            CancellationToken.None);

        Assert.Null(updateResult);
        Assert.Null(removeResult);

        context.ChangeTracker.Clear();
        var persistedItem = await context.SaleItems.SingleAsync();
        var persistedSale = await context.Sales.SingleAsync();
        Assert.Equal("მუხლი", persistedItem.ProductName);
        Assert.Equal(2m, persistedSale.TotalAmount);
    }

    private static StorePosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new StorePosDbContext(options);
    }
}
