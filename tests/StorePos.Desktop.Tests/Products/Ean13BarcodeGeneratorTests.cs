using StorePos.Desktop.Products.Barcodes;

namespace StorePos.Desktop.Tests.Products;

public sealed class Ean13BarcodeGeneratorTests
{
    private readonly Ean13BarcodeGenerator _generator = new();

    [Fact]
    public void Generate_PadsCodeAndAppendsValidChecksum()
    {
        var barcode = _generator.Generate("10525");

        Assert.Equal("0000000105255", barcode);
        Assert.Equal(13, barcode.Length);
        Assert.All(barcode, character => Assert.InRange(character, '0', '9'));
        Assert.True(HasValidChecksum(barcode));
    }

    [Fact]
    public void Generate_IsDeterministicAndChangesWithCode()
    {
        Assert.Equal(_generator.Generate("10525"), _generator.Generate("10525"));
        Assert.Equal("0000000210003", _generator.Generate("21000"));
        Assert.NotEqual(_generator.Generate("10525"), _generator.Generate("21000"));
    }

    [Fact]
    public void Generate_CodeLongerThanTwelveDigitsIsRejectedWithoutTruncation()
        => Assert.Throws<ArgumentException>(() =>
            _generator.Generate("1234567890123"));

    private static bool HasValidChecksum(string barcode)
    {
        var sum = 0;
        for (var index = 0; index < 12; index++)
        {
            var digit = barcode[index] - '0';
            sum += index % 2 == 0 ? digit : digit * 3;
        }

        return (10 - sum % 10) % 10 == barcode[12] - '0';
    }
}
