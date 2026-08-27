using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorePos.Api.Controllers;
using StorePos.Application.Products.Commands.UpdateRetailPrice;
using StorePos.Application.Sales.Commands.AddProductItem;
using StorePos.Domain.Aggregates.MeasurementUnit;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Persistence;
using StorePos.Persistence.Context;
using StorePos.Persistence.Repositories;

namespace StorePos.IntegrationTests.Products;

public sealed class ProductRetailPriceWorkflowTests
{
    [Fact]
    public async Task UpdateRetailPrice_PersistsOnlyPriceThenNormalSaleAddUsesConfirmedPriceAndMerge()
    {
        await using var context = CreateContext();
        var unit = MeasurementUnit.Create("Piece", "pc");
        await context.MeasurementUnits.AddAsync(unit);
        await context.SaveChangesAsync();
        var product = Product.Create(
            "GMTEK-40012",
            "00077",
            "Cement",
            unit.Id,
            0m,
            "Supplier",
            "00117",
            18m);
        var sale = Sale.Create("20260827-QUICK-1");
        await context.Products.AddAsync(product);
        await context.Sales.AddAsync(sale);
        await context.SaveChangesAsync();
        var dateCreated = product.DateCreated;
        var productRepository = new ProductRepository(context);
        var unitOfWork = new UnitOfWork(context);

        var updated = await new UpdateProductRetailPriceCommandHandler(
                productRepository,
                unitOfWork)
            .Handle(
                new UpdateProductRetailPriceCommand(product.Id, 25.123456m),
                CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(25.12346m, updated.Price);
        Assert.Equal("GMTEK-40012", product.Code);
        Assert.Equal("00077", product.Barcode);
        Assert.Equal("Cement", product.Name);
        Assert.Equal(unit.Id, product.MeasurementUnitId);
        Assert.Equal("Supplier", product.SupplierName);
        Assert.Equal("00117", product.SupplierCode);
        Assert.Equal(18m, product.CostPrice);
        Assert.Equal(dateCreated, product.DateCreated);
        Assert.NotNull(product.DateUpdated);

        var addHandler = new AddProductSaleItemCommandHandler(
            new SaleRepository(context),
            productRepository,
            new MeasurementUnitRepository(context),
            unitOfWork);
        var first = await addHandler.Handle(
            new AddProductSaleItemCommand(sale.Id, product.Id),
            CancellationToken.None);
        var second = await addHandler.Handle(
            new AddProductSaleItemCommand(sale.Id, product.Id),
            CancellationToken.None);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(25.12346m, first.UnitPrice);
        Assert.Equal(2m, second.Quantity);
        Assert.Equal(first.SaleItemId, second.SaleItemId);
        Assert.Single(await context.SaleItems.ToArrayAsync());
    }

    [Fact]
    public async Task UpdateRetailPrice_MissingProductReturnsNull()
    {
        await using var context = CreateContext();

        var result = await new UpdateProductRetailPriceCommandHandler(
                new ProductRepository(context),
                new UnitOfWork(context))
            .Handle(new UpdateProductRetailPriceCommand(999, 25m), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public void ProductsController_ExposesDedicatedPatchRetailPriceRoute()
    {
        var method = typeof(ProductsController).GetMethod(
            nameof(ProductsController.UpdateRetailPrice),
            BindingFlags.Public | BindingFlags.Instance);

        var route = Assert.Single(method!.GetCustomAttributes<HttpPatchAttribute>());
        Assert.Equal("{productId:long}/retail-price", route.Template);
    }

    private static StorePosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StorePosDbContext(options);
    }
}
