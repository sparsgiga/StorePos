using StorePos.Domain.Aggregates.Product;

namespace StorePos.Domain.Tests.Products;

public sealed class ProductTests
{
    [Fact]
    public void Create_NormalizesValuesAndCreatesActiveProduct()
    {
        var product = Product.Create(" 10525 ", " 12345678 ", " Cement ", 2, 0.444m);

        Assert.Equal("10525", product.Code);
        Assert.Equal("12345678", product.Barcode);
        Assert.Equal("Cement", product.Name);
        Assert.Equal(2, product.MeasurementUnitId);
        Assert.Equal(0.444m, product.Price);
        Assert.True(product.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyName_Throws(string? name)
        => Assert.Throws<ArgumentException>(() =>
            Product.Create("10525", null, name!, 1, 1m));

    [Fact]
    public void Create_InvalidMeasurementUnit_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            Product.Create("10525", null, "Product", 0, 1m));

    [Fact]
    public void Create_NegativePrice_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            Product.Create("10525", null, "Product", 1, -0.001m));

    [Theory]
    [InlineData("PRD-1")]
    [InlineData("12.01კოდი")]
    [InlineData("GMTEK-40012")]
    [InlineData("A-100/5")]
    public void Create_FlexibleCode_PreservesIdentifier(string code)
    {
        var product = Product.Create(code, null, "Product", 1, 0m);

        Assert.Equal(code, product.Code);
        Assert.Equal(0m, product.Price);
    }

    [Fact]
    public void UpdateDetails_NormalizesAllFieldsAndPrice()
    {
        var product = Product.Create("100", "111", "Old", 1, 1m);

        product.UpdateDetails(" 200 ", " 222 ", " New ", 2, 12.665656m);

        Assert.Equal("200", product.Code);
        Assert.Equal("222", product.Barcode);
        Assert.Equal("New", product.Name);
        Assert.Equal(2, product.MeasurementUnitId);
        Assert.Equal(12.66566m, product.Price);
    }

    [Fact]
    public void UpdateDetails_AllowsMissingBarcode()
    {
        var product = Product.Create("100", null, "Legacy", 1, 1m);

        product.UpdateDetails("100", " ", "Legacy", 1, 1m);

        Assert.Null(product.Barcode);
    }

    [Fact]
    public void ActivateAndDeactivate_ControlStatus()
    {
        var product = Product.Create("100", "111", "Product", 1, 1m);

        product.Deactivate();
        Assert.False(product.IsActive);
        product.Activate();
        Assert.True(product.IsActive);
    }

    [Fact]
    public void Create_PriceThatRoundsToZeroIsAccepted()
    {
        var product = Product.Create("100", "111", "Product", 1, 0.000001m);

        Assert.Equal(0m, product.Price);
    }

    public static TheoryData<decimal?> ValidCostPrices => new()
    {
        null,
        0m,
        0.06m
    };

    [Theory]
    [MemberData(nameof(ValidCostPrices))]
    public void Create_AllowsOptionalNonNegativeCostPrice(decimal? costPrice)
    {
        var product = Product.Create(
            "100", null, "Product", 1, 0m, "Supplier", "00077", costPrice);

        Assert.Equal("00077", product.SupplierCode);
        Assert.Equal(costPrice, product.CostPrice);
    }

    [Fact]
    public void Create_NegativeCostPrice_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            Product.Create("100", null, "Product", 1, 0m, costPrice: -0.01m));

    [Fact]
    public void Create_CodeAboveMaximumLength_Throws()
        => Assert.Throws<ArgumentException>(() =>
            Product.Create(new string('A', Product.CodeMaxLength + 1), null, "Product", 1, 0m));
}
