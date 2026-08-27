using Microsoft.EntityFrameworkCore;
using StorePos.Application.Common.Exceptions;
using StorePos.Application.Products.Commands.Activate;
using StorePos.Application.Products.Commands.Create;
using StorePos.Application.Products.Commands.Deactivate;
using StorePos.Application.Products.Commands.Update;
using StorePos.Application.Products.Queries.GetList;
using StorePos.Domain.Aggregates.MeasurementUnit;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Persistence;
using StorePos.Persistence.Context;
using StorePos.Persistence.Queries;
using StorePos.Persistence.Repositories;

namespace StorePos.IntegrationTests.Products;

public sealed class ProductManagementWorkflowTests
{
    [Fact]
    public async Task List_SearchesFiltersAndPaginatesDeterministically()
    {
        await using var context = CreateContext();
        var piece = MeasurementUnit.Create("Piece", "pc");
        var kilogram = MeasurementUnit.Create("Kilogram", "kg");
        await context.MeasurementUnits.AddRangeAsync(piece, kilogram);
        await context.SaveChangesAsync();
        var inactive = Product.Create("103", "300", "Beta inactive", kilogram.Id, 30m);
        inactive.Deactivate();
        await context.Products.AddRangeAsync(
            Product.Create("101", "100", "Alpha one", piece.Id, 10m),
            Product.Create("102", "200", "Alpha two", piece.Id, 20m),
            Product.Create(
                "104", "400", "Zero price", piece.Id, 0m,
                "Supplier Alpha", "00077", 0.06m),
            inactive);
        await context.SaveChangesAsync();
        var service = new ProductManagementReadService(context);

        var firstPage = await service.GetListAsync(new GetProductsQuery(
            Search: "Alpha",
            Status: ProductStatusFilter.Active,
            MeasurementUnitId: piece.Id,
            PriceFrom: 5m,
            PriceTo: 25m,
            PageNumber: 1,
            PageSize: 1));
        var secondPage = await service.GetListAsync(new GetProductsQuery(
            Search: "Alpha",
            Status: ProductStatusFilter.Active,
            MeasurementUnitId: piece.Id,
            PageNumber: 2,
            PageSize: 1));
        var byCode = await service.GetListAsync(new GetProductsQuery(
            Search: "103", Status: ProductStatusFilter.Inactive));
        var byBarcode = await service.GetListAsync(new GetProductsQuery(
            Search: "300", Status: ProductStatusFilter.All));
        var bySupplier = await service.GetListAsync(new GetProductsQuery(
            Search: "00077", Status: ProductStatusFilter.Active));

        Assert.Equal(2, firstPage.TotalCount);
        Assert.Equal("101", Assert.Single(firstPage.Items).Code);
        Assert.Equal("102", Assert.Single(secondPage.Items).Code);
        Assert.Equal(inactive.Id, Assert.Single(byCode.Items).Id);
        Assert.Equal(inactive.Id, Assert.Single(byBarcode.Items).Id);
        var supplierProduct = Assert.Single(bySupplier.Items);
        Assert.Equal("Supplier Alpha", supplierProduct.SupplierName);
        Assert.Equal("00077", supplierProduct.SupplierCode);
        Assert.Equal(0.06m, supplierProduct.CostPrice);
        Assert.Equal(0m, supplierProduct.Price);
    }

    [Fact]
    public async Task CreateAndUpdate_ValidateUniquenessAndAllowOwnValues()
    {
        await using var context = CreateContext();
        var unit = MeasurementUnit.Create("Piece", "pc");
        await context.MeasurementUnits.AddAsync(unit);
        await context.SaveChangesAsync();
        var repository = new ProductRepository(context);
        var unitRepository = new MeasurementUnitRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var create = new CreateProductCommandHandler(repository, unitRepository, unitOfWork);

        var first = await create.Handle(
            new CreateProductCommand("100", "111", "First", unit.Id, 1.234567m),
            CancellationToken.None);
        await Assert.ThrowsAsync<ProductCodeConflictException>(() => create.Handle(
            new CreateProductCommand("100", "222", "Duplicate", unit.Id, 1m),
            CancellationToken.None));
        await Assert.ThrowsAsync<ProductBarcodeConflictException>(() => create.Handle(
            new CreateProductCommand("101", "111", "Duplicate", unit.Id, 1m),
            CancellationToken.None));

        var update = new UpdateProductCommandHandler(repository, unitRepository, unitOfWork);
        var unchanged = await update.Handle(
            new UpdateProductCommand(first.Id, "100", "111", "Renamed", unit.Id, 2m),
            CancellationToken.None);

        Assert.NotNull(unchanged);
        Assert.Equal("Renamed", unchanged.Name);
        Assert.Equal(1.23457m, first.Price);
        Assert.Equal(2m, unchanged.Price);
    }

    [Fact]
    public async Task DeactivateAndReactivate_ControlCashierSearchWithoutChangingSnapshot()
    {
        await using var context = CreateContext();
        var unit = MeasurementUnit.Create("Piece", "pc");
        await context.MeasurementUnits.AddAsync(unit);
        await context.SaveChangesAsync();
        var product = Product.Create("100", "111", "Original", unit.Id, 10m);
        var sale = Sale.Create("20260826-0001");
        await context.Products.AddAsync(product);
        await context.Sales.AddAsync(sale);
        await context.SaveChangesAsync();
        var snapshot = sale.AddProductItem(
            product.Id,
            product.Code,
            product.Barcode,
            product.Name,
            unit.Id,
            unit.Name,
            2m,
            product.Price).Item;
        await context.SaveChangesAsync();
        var repository = new ProductRepository(context);
        var unitOfWork = new UnitOfWork(context);

        await new UpdateProductCommandHandler(
                repository,
                new MeasurementUnitRepository(context),
                unitOfWork)
            .Handle(
                new UpdateProductCommand(product.Id, "100", "111", "Changed", unit.Id, 12m),
                CancellationToken.None);
        await new DeactivateProductCommandHandler(repository, unitOfWork)
            .Handle(new DeactivateProductCommand(product.Id), CancellationToken.None);

        Assert.Empty(await new ProductReadService(context)
            .SearchAsync("111", 15, exactOnly: true));
        Assert.Equal("Original", snapshot.ProductName);
        Assert.Equal(10m, snapshot.UnitPrice);
        Assert.Equal(20m, snapshot.LineTotal);

        await new ActivateProductCommandHandler(repository, unitOfWork)
            .Handle(new ActivateProductCommand(product.Id), CancellationToken.None);
        Assert.Single(await new ProductReadService(context)
            .SearchAsync("111", 15, exactOnly: true));
        Assert.Equal("Original", snapshot.ProductName);
    }

    [Fact]
    public void Model_HasFilteredUniqueBarcodeIndex()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Product));
        var index = Assert.Single(entityType!.GetIndexes(), current =>
            current.Properties.Single().Name == nameof(Product.Barcode));

        Assert.True(index.IsUnique);
        Assert.Equal("[Barcode] IS NOT NULL", index.GetFilter());
    }

    private static StorePosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StorePosDbContext(options);
    }
}
