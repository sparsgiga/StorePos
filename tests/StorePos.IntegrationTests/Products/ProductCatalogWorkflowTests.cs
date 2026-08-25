using Microsoft.EntityFrameworkCore;
using StorePos.Application.Common.Exceptions;
using StorePos.Application.Common.Interfaces;
using StorePos.Application.Products.Commands.CreateAndAddToSale;
using StorePos.Application.Products.Queries.Search;
using StorePos.Application.Sales.Commands.AddProductItem;
using StorePos.Domain.Aggregates.MeasurementUnit;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Persistence;
using StorePos.Persistence.Context;
using StorePos.Persistence.Queries;
using StorePos.Persistence.Repositories;

namespace StorePos.IntegrationTests.Products;

public sealed class ProductCatalogWorkflowTests
{
    [Fact]
    public async Task Search_RanksExactBarcodeBeforeExactCode()
    {
        await using var context = CreateContext();
        var unit = MeasurementUnit.Create("Piece", "pc");
        await context.MeasurementUnits.AddAsync(unit);
        await context.SaveChangesAsync();

        var barcodeMatch = Product.Create("PRD-1", "12345678", "Alpha", unit.Id, 2m);
        var codeMatch = Product.Create("12345678", null, "Beta", unit.Id, 3m);
        await context.Products.AddRangeAsync(barcodeMatch, codeMatch);
        await context.SaveChangesAsync();

        var service = new ProductReadService(context);
        var results = await service.SearchAsync(
            "12345678",
            15,
            exactOnly: false);

        Assert.Equal(2, results.Count);
        Assert.Equal(barcodeMatch.Id, results[0].Id);
        Assert.Equal(codeMatch.Id, results[1].Id);
    }

    [Fact]
    public async Task AddExistingProductTwice_PersistsOneRowAndPreservesOriginalPrice()
    {
        await using var context = CreateContext();
        var unit = MeasurementUnit.Create("Piece", "pc");
        var sale = Sale.Create("20260825-0001");
        await context.MeasurementUnits.AddAsync(unit);
        await context.SaveChangesAsync();

        var product = Product.Create("PRD-1", "12345678", "Cement", unit.Id, 3.50m);
        await context.Products.AddAsync(product);
        await context.Sales.AddAsync(sale);
        await context.SaveChangesAsync();

        var handler = new AddProductSaleItemCommandHandler(
            new SaleRepository(context),
            new ProductRepository(context),
            new MeasurementUnitRepository(context),
            new UnitOfWork(context));

        var first = await handler.Handle(
            new AddProductSaleItemCommand(sale.Id, product.Id),
            CancellationToken.None);
        var second = await handler.Handle(
            new AddProductSaleItemCommand(sale.Id, product.Id),
            CancellationToken.None);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.True(first.WasNewItem);
        Assert.False(second.WasNewItem);
        Assert.Equal(first.SaleItemId, second.SaleItemId);
        Assert.Equal(2m, second.Quantity);
        Assert.Equal(3.50m, second.UnitPrice);
        Assert.Single(await context.SaleItems.ToArrayAsync());
    }

    [Fact]
    public async Task CreateProductAndAddToSale_PersistsBothAndRejectsDuplicateBarcode()
    {
        await using var context = CreateContext();
        var unit = MeasurementUnit.Create("Piece", "pc");
        var sale = Sale.Create("20260825-0001");
        await context.MeasurementUnits.AddAsync(unit);
        await context.Sales.AddAsync(sale);
        await context.SaveChangesAsync();

        var handler = new CreateProductAndAddToSaleCommandHandler(
            new SaleRepository(context),
            new ProductRepository(context),
            new MeasurementUnitRepository(context),
            new StubProductCodeGenerator(),
            new UnitOfWork(context));

        var result = await handler.Handle(
            new CreateProductAndAddToSaleCommand(
                sale.Id,
                "Cement",
                "12345678",
                unit.Id,
                2m,
                0.444m,
                "Created by cashier"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("PRD-TEST-1", result.ProductCode);
        Assert.Equal(0.888m, result.LineTotal);
        Assert.Single(await context.Products.ToArrayAsync());
        Assert.Single(await context.SaleItems.ToArrayAsync());

        await Assert.ThrowsAsync<ProductBarcodeConflictException>(() => handler.Handle(
            new CreateProductAndAddToSaleCommand(
                sale.Id,
                "Other product",
                "12345678",
                unit.Id,
                1m,
                1m),
            CancellationToken.None));
    }

    private static StorePosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StorePosDbContext(options);
    }

    private sealed class StubProductCodeGenerator : IProductCodeGenerator
    {
        private int _sequence;

        public string Generate() => $"PRD-TEST-{++_sequence}";
    }
}
