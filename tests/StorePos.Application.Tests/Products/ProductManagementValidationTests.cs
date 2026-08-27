using StorePos.Application.Products.Commands.Create;
using StorePos.Application.Products.Commands.Update;
using StorePos.Application.Products.Queries.GetList;

namespace StorePos.Application.Tests.Products;

public sealed class ProductManagementValidationTests
{
    [Fact]
    public void Create_AllowsMissingBarcodeAndZeroPrice()
    {
        var validator = new CreateProductCommandValidator();

        var result = validator.Validate(new CreateProductCommand(
            "100", "", "Product", 1, 0m));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Update_AllowsFlexibleCodeAndMissingBarcode()
    {
        var validator = new UpdateProductCommandValidator();

        var result = validator.Validate(new UpdateProductCommand(
            1, "A-1", "", "Product", 1, 1m));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void List_RejectsInvalidPriceRangeAndOversizedPage()
    {
        var validator = new GetProductsQueryValidator();

        var result = validator.Validate(new GetProductsQuery(
            PriceFrom: 20m,
            PriceTo: 10m,
            PageSize: 201));

        Assert.Contains(result.Errors, error => error.PropertyName == "PriceTo");
        Assert.Contains(result.Errors, error => error.PropertyName == "PageSize");
    }
}
