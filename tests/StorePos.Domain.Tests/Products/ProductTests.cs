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
    [InlineData("12 34")]
    [InlineData("１２３")]
    public void Create_NonnumericCode_Throws(string code)
        => Assert.Throws<ArgumentException>(() =>
            Product.Create(code, null, "Product", 1, 1m));

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
    public void UpdateDetails_RequiresBarcode()
    {
        var product = Product.Create("100", null, "Legacy", 1, 1m);

        Assert.Throws<ArgumentException>(() =>
            product.UpdateDetails("100", " ", "Legacy", 1, 1m));
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
    public void Create_PriceThatRoundsToZeroIsRejected()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            Product.Create("100", "111", "Product", 1, 0.000001m));
}
