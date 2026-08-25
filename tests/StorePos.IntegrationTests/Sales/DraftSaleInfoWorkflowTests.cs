using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using StorePos.Application.Sales.Commands.UpdateDraftInfo;
using StorePos.Application.Sales.Queries.GetDraftDetails;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Persistence;
using StorePos.Persistence.Context;
using StorePos.Persistence.Repositories;

namespace StorePos.IntegrationTests.Sales;

public sealed class DraftSaleInfoWorkflowTests
{
    [Fact]
    public async Task UpdateInfo_PersistsAllValues_AndDetailsRestoresThem()
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
            var sale = Sale.Create("20260825-0001");

            await repository.AddAsync(sale);
            await unitOfWork.SaveChangesAsync();

            var handler = new UpdateDraftSaleInfoCommandHandler(repository, unitOfWork);
            var result = await handler.Handle(
                new UpdateDraftSaleInfoCommand(
                    sale.Id,
                    "  გიორგი  ",
                    "  01000000000  ",
                    "  ხელოსანი  "),
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("გიორგი", result.CustomerName);
            Assert.Equal("01000000000", result.CustomerIdentificationNumber);
            Assert.Equal("ხელოსანი", result.Comment);
            saleId = sale.Id;
        }

        await using var reloadedContext = new StorePosDbContext(options);
        var reloadedRepository = new SaleRepository(reloadedContext);
        var detailsHandler = new GetDraftSaleDetailsQueryHandler(reloadedRepository);

        var details = await detailsHandler.Handle(
            new GetDraftSaleDetailsQuery(saleId),
            CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal("გიორგი", details.CustomerName);
        Assert.Equal("01000000000", details.CustomerIdentificationNumber);
        Assert.Equal("ხელოსანი", details.Comment);
        Assert.Equal(0m, details.TotalAmount);
        Assert.NotNull((await reloadedContext.Sales.SingleAsync()).DateUpdated);
    }
}
