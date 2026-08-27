using StorePos.Application.Products.Commands.Create;
using StorePos.Application.Products.Commands.Update;
using StorePos.Application.Products.Commands.UpdateRetailPrice;
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

    [Theory]
    [InlineData("A-1")]
    [InlineData("12A")]
    [InlineData("１２３")]
    public void Create_RejectsNonAsciiNumericCode(string code)
    {
        var result = new CreateProductCommandValidator().Validate(
            new CreateProductCommand(code, null, "Product", 1, 1m));

        Assert.Contains(result.Errors, error => error.PropertyName == "Code");
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

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("0.000001")]
    public void QuickRetailPrice_RejectsNonPositiveNormalizedPrice(string value)
    {
        var validator = new UpdateProductRetailPriceCommandValidator();

        var result = validator.Validate(new UpdateProductRetailPriceCommand(
            1,
            decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Contains(result.Errors, error => error.PropertyName == "Price");
    }

    [Fact]
    public void QuickRetailPrice_AcceptsPositivePrice()
    {
        var result = new UpdateProductRetailPriceCommandValidator().Validate(
            new UpdateProductRetailPriceCommand(1, 25.123456m));

        Assert.True(result.IsValid);
    }
}
