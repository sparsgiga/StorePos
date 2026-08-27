using Microsoft.EntityFrameworkCore;
using StorePos.Application.Common.Exceptions;
using StorePos.Application.Products.Commands.CreateAndAddToSale;
using StorePos.Application.Sales.Commands.AddProductItem;
using StorePos.Domain.Aggregates.MeasurementUnit;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Persistence;
using StorePos.Persistence.Context;
using StorePos.Persistence.Queries;
using StorePos.Persistence.Repositories;
using StorePos.Persistence.Sequences;
using StorePos.Persistence.Services;

namespace StorePos.IntegrationTests.Products;

public sealed class ProductCatalogWorkflowTests
{
    [Fact]
    public async Task Search_RanksExactBarcodeBeforeExactCodeAndReturnsSnapshots()
    {
        await using var context = CreateContext();
        var unit = MeasurementUnit.Create("Piece", "pc");
        await context.MeasurementUnits.AddAsync(unit);
        await context.SaveChangesAsync();

        var barcodeMatch = Product.Create("100", "12345678", "Alpha", unit.Id, 2m);
        var codeMatch = Product.Create("12345678", null, "Beta", unit.Id, 3m);
        await context.Products.AddRangeAsync(barcodeMatch, codeMatch);
        await context.SaveChangesAsync();

        var results = await new ProductReadService(context).SearchAsync(
            "12345678",
            15,
            exactOnly: false);

        Assert.Equal(2, results.Count);
        Assert.Equal(barcodeMatch.Id, results[0].Id);
        Assert.Equal("100", results[0].Code);
        Assert.Equal("12345678", results[0].Barcode);
        Assert.Equal(unit.Id, results[0].MeasurementUnitId);
        Assert.Equal("Piece", results[0].MeasurementUnitName);
        Assert.Equal("pc", results[0].MeasurementUnitShortName);
        Assert.Equal(2m, results[0].Price);
        Assert.Equal(codeMatch.Id, results[1].Id);
    }

    [Fact]
    public async Task Search_SupportsPartialNameExcludesInactiveAndRespectsLimit()
    {
        await using var context = CreateContext();
        var unit = MeasurementUnit.Create("Piece", "pc");
        await context.MeasurementUnits.AddAsync(unit);
        await context.SaveChangesAsync();

        var activeOne = Product.Create("101", null, "Cement Alpha", unit.Id, 2m);
        var activeTwo = Product.Create("102", null, "Cement Beta", unit.Id, 3m);
        var inactive = Product.Create("103", null, "Cement Hidden", unit.Id, 4m);
        await context.Products.AddRangeAsync(activeOne, activeTwo, inactive);
        await context.SaveChangesAsync();
        context.Entry(inactive).Property(product => product.IsActive).CurrentValue = false;
        await context.SaveChangesAsync();

        var results = await new ProductReadService(context).SearchAsync(
            "Cement",
            1,
            exactOnly: false);

        Assert.Single(results);
        Assert.NotEqual(inactive.Id, results[0].Id);
        Assert.Contains(results[0].Id, new[] { activeOne.Id, activeTwo.Id });
    }

    [Fact]
    public async Task AddSameProductThreeTimes_PersistsOneRowAndPreservesOriginalPrice()
    {
        await using var context = CreateContext();
        var unit = MeasurementUnit.Create("Piece", "pc");
        var sale = Sale.Create("20260825-0001");
        await context.MeasurementUnits.AddAsync(unit);
        await context.SaveChangesAsync();

        var product = Product.Create("104", "12345678", "Cement", unit.Id, 4.50m);
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
        context.Entry(product).Property(item => item.Price).CurrentValue = 5m;
        await context.SaveChangesAsync();
        var second = await handler.Handle(
            new AddProductSaleItemCommand(sale.Id, product.Id),
            CancellationToken.None);
        var third = await handler.Handle(
            new AddProductSaleItemCommand(sale.Id, product.Id),
            CancellationToken.None);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotNull(third);
        Assert.True(first.WasNewItem);
        Assert.False(second.WasNewItem);
        Assert.False(third.WasNewItem);
        Assert.Equal(first.SaleItemId, third.SaleItemId);
        Assert.Equal(3m, third.Quantity);
        Assert.Equal(4.50m, third.UnitPrice);
        Assert.Equal(13.50m, third.LineTotal);
        Assert.Equal(13.50m, third.SaleTotalAmount);
        Assert.Single(await context.SaleItems.ToArrayAsync());
    }

    [Fact]
    public async Task AddZeroPriceCatalogProduct_IsBlockedBeforeSaleItemIsCreated()
    {
        await using var context = CreateContext();
        var unit = MeasurementUnit.Create("Piece", "pc");
        var sale = Sale.Create("20260827-0001");
        await context.MeasurementUnits.AddAsync(unit);
        await context.SaveChangesAsync();
        var product = Product.Create("ZERO", "00077", "Zero price product", unit.Id, 0m);
        await context.Products.AddAsync(product);
        await context.Sales.AddAsync(sale);
        await context.SaveChangesAsync();
        var handler = new AddProductSaleItemCommandHandler(
            new SaleRepository(context),
            new ProductRepository(context),
            new MeasurementUnitRepository(context),
            new UnitOfWork(context));

        var exception = await Assert.ThrowsAsync<ProductRetailPriceNotSetException>(() =>
            handler.Handle(
                new AddProductSaleItemCommand(sale.Id, product.Id),
                CancellationToken.None));

        Assert.Contains("საცალო ფასი", exception.Message);
        Assert.Empty(await context.SaleItems.ToArrayAsync());
        Assert.Empty(sale.Items);
    }

    [Fact]
    public async Task CreationDefaults_UseMaximumPositiveNumericCodeAndSemanticUnit()
    {
        await using var context = CreateContext();
        var otherUnit = MeasurementUnit.Create("Kilogram", "kg");
        var defaultUnit = MeasurementUnit.Create("ცალი", "ც");
        await context.MeasurementUnits.AddRangeAsync(otherUnit, defaultUnit);
        await context.SaveChangesAsync();
        Assert.NotEqual(1, defaultUnit.Id);

        await context.Products.AddAsync(
            Product.Create("0010525", null, "Old numeric maximum", otherUnit.Id, 1m));
        await context.SaveChangesAsync();
        await context.Products.AddRangeAsync(
            Product.Create("10520", null, "Newer lower numeric", otherUnit.Id, 1m),
            Product.Create("ABC-999999", null, "Alphanumeric", otherUnit.Id, 1m),
            Product.Create("9223372036854775808", null, "Outside bigint", otherUnit.Id, 1m));
        await context.SaveChangesAsync();
        await context.ManualProductCodeSequences.AddAsync(
            ManualProductCodeSequence.Initialize(9756));
        await context.SaveChangesAsync();

        var result = await new ProductCreationDefaultsReadService(
            context,
            new ManualProductCodeSequenceService(context)).GetAsync();

        Assert.Equal("9756", result.SuggestedCode);
        Assert.Equal(defaultUnit.Id, result.DefaultMeasurementUnitId);
        Assert.Equal("ცალი", result.DefaultMeasurementUnitName);
        Assert.Equal("ც", result.DefaultMeasurementUnitShortName);
        Assert.Null(result.ConfigurationMessage);
    }

    [Fact]
    public async Task CreationDefaults_NoNumericCodesReturnsEmptyAndReportsMissingDefaultUnit()
    {
        await using var context = CreateContext();
        await context.MeasurementUnits.AddAsync(MeasurementUnit.Create("Kilogram", "kg"));
        await context.SaveChangesAsync();
        await context.Products.AddRangeAsync(
            Product.Create("ABC", null, "Alphabetic", 1, 1m),
            Product.Create("9223372036854775808", null, "Outside bigint", 1, 1m));
        await context.SaveChangesAsync();
        await context.ManualProductCodeSequences.AddAsync(
            ManualProductCodeSequence.Initialize(1000));
        await context.SaveChangesAsync();

        var result = await new ProductCreationDefaultsReadService(
            context,
            new ManualProductCodeSequenceService(context)).GetAsync();

        Assert.Equal("1000", result.SuggestedCode);
        Assert.Null(result.DefaultMeasurementUnitId);
        Assert.False(string.IsNullOrWhiteSpace(result.ConfigurationMessage));
    }

    [Fact]
    public async Task CreateProductAndAddToSale_UsesEnteredNameCodeBarcodeUnitAndQuantity()
    {
        await using var context = CreateContext();
        var defaultUnit = MeasurementUnit.Create("ცალი", "ც");
        var selectedUnit = MeasurementUnit.Create("Kilogram", "kg");
        var sale = Sale.Create("20260825-0001");
        await context.MeasurementUnits.AddRangeAsync(defaultUnit, selectedUnit);
        await context.Sales.AddAsync(sale);
        await context.SaveChangesAsync();

        var handler = CreateProductHandler(context);
        var result = await handler.Handle(
            new CreateProductAndAddToSaleCommand(
                sale.Id,
                "20000",
                "Cement entered by cashier",
                "0000000200008",
                selectedUnit.Id,
                2m,
                0.444m,
                "Created by cashier"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("20000", result.ProductCode);
        Assert.Equal("Cement entered by cashier", result.ProductName);
        Assert.Equal("0000000200008", result.Barcode);
        Assert.Equal(selectedUnit.Id, result.MeasurementUnitId);
        Assert.Equal(2m, result.Quantity);
        Assert.Equal(0.444m, result.UnitPrice);
        Assert.Equal(0.89m, result.LineTotal);
        Assert.Equal(0.89m, result.SaleTotalAmount);
        Assert.False(result.IsManual);

        var product = Assert.Single(await context.Products.ToArrayAsync());
        Assert.Equal("Cement entered by cashier", product.Name);
        Assert.Equal("20000", product.Code);
        Assert.Equal("0000000200008", product.Barcode);
        Assert.Equal(selectedUnit.Id, product.MeasurementUnitId);
        Assert.Equal(0.444m, product.Price);
        Assert.True(product.IsActive);
        context.ChangeTracker.Clear();
        Assert.Equal(
            1000,
            await context.ManualProductCodeSequences
                .Select(sequence => sequence.NextCode)
                .SingleAsync());

        var searchResult = Assert.Single(await new ProductReadService(context)
            .SearchAsync("20000", 15, exactOnly: true));
        Assert.Equal(product.Id, searchResult.Id);
    }

    [Fact]
    public async Task CreateProductAndAddToSale_ConsumingSuggestionAdvancesSequence()
    {
        await using var context = CreateContext();
        var unit = MeasurementUnit.Create("Piece", "pc");
        var sale = Sale.Create("20260827-0002");
        await context.MeasurementUnits.AddAsync(unit);
        await context.Sales.AddAsync(sale);
        await context.SaveChangesAsync();
        var handler = CreateProductHandler(context);

        var result = await handler.Handle(
            new CreateProductAndAddToSaleCommand(
                sale.Id, "1000", "Product", "0000000010004", unit.Id, 1m, 1m),
            CancellationToken.None);

        Assert.NotNull(result);
        context.ChangeTracker.Clear();
        Assert.Equal(
            1001,
            await context.ManualProductCodeSequences
                .Select(sequence => sequence.NextCode)
                .SingleAsync());
    }

    [Fact]
    public async Task CreateProductAndAddToSale_RejectsDuplicateCodeOrBarcode()
    {
        await using var context = CreateContext();
        var unit = MeasurementUnit.Create("Piece", "pc");
        var sale = Sale.Create("20260825-0001");
        await context.MeasurementUnits.AddAsync(unit);
        await context.Sales.AddAsync(sale);
        await context.SaveChangesAsync();

        var handler = CreateProductHandler(context);
        await handler.Handle(
            new CreateProductAndAddToSaleCommand(
                sale.Id, "30000", "First product", "12345670", unit.Id, 1m, 1m),
            CancellationToken.None);

        var product = Assert.Single(await context.Products.ToArrayAsync());
        Assert.Equal("12345670", product.Barcode);

        await Assert.ThrowsAsync<ProductCodeConflictException>(() => handler.Handle(
            new CreateProductAndAddToSaleCommand(
                sale.Id, "30000", "Duplicate code", "12345678", unit.Id, 1m, 1m),
            CancellationToken.None));

        await handler.Handle(
            new CreateProductAndAddToSaleCommand(
                sale.Id, "30001", "With barcode", "12345678", unit.Id, 1m, 1m),
            CancellationToken.None);

        await Assert.ThrowsAsync<ProductBarcodeConflictException>(() => handler.Handle(
            new CreateProductAndAddToSaleCommand(
                sale.Id, "30002", "Duplicate barcode", "12345678", unit.Id, 1m, 1m),
            CancellationToken.None));
    }

    private static CreateProductAndAddToSaleCommandHandler CreateProductHandler(
        StorePosDbContext context)
    {
        if (!context.ManualProductCodeSequences.Local.Any() &&
            !context.ManualProductCodeSequences.Any())
        {
            context.ManualProductCodeSequences.Add(
                ManualProductCodeSequence.Initialize(1000));
            context.SaveChanges();
        }

        return new(
            new SaleRepository(context),
            new ProductRepository(context),
            new MeasurementUnitRepository(context),
            new ManualProductCodeSequenceService(context),
            new UnitOfWork(context));
    }

    private static StorePosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StorePosDbContext(options);
    }
}
