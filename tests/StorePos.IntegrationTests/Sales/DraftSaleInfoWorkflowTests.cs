using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using StorePos.Application.Sales.Commands.UpdateComment;
using StorePos.Application.Sales.Queries.GetDraftDetails;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Persistence;
using StorePos.Persistence.Context;
using StorePos.Persistence.Repositories;

namespace StorePos.IntegrationTests.Sales;

public sealed class DraftSaleInfoWorkflowTests
{
    [Fact]
    public async Task UpdateComment_PersistsWithoutChangingLegacyCustomerSnapshot()
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
            var sale = Sale.Create(
                "20260825-0001",
                customerName: "Legacy Customer",
                customerIdentificationNumber: "01001");
            await repository.AddAsync(sale);
            await unitOfWork.SaveChangesAsync();

            var handler = new UpdateSaleCommentCommandHandler(repository, unitOfWork);
            var result = await handler.Handle(
                new UpdateSaleCommentCommand(sale.Id, "  Sale comment  "),
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Sale comment", result.Comment);
            saleId = sale.Id;
        }

        await using var reloadedContext = new StorePosDbContext(options);
        var details = await new GetDraftSaleDetailsQueryHandler(
                new SaleRepository(reloadedContext))
            .Handle(new GetDraftSaleDetailsQuery(saleId), CancellationToken.None);

        Assert.NotNull(details);
        Assert.Null(details.CustomerId);
        Assert.Equal("Legacy Customer", details.CustomerName);
        Assert.Equal("01001", details.CustomerIdentificationNumber);
        Assert.Equal("Sale comment", details.Comment);
        Assert.NotNull((await reloadedContext.Sales.SingleAsync()).DateUpdated);
    }
}
