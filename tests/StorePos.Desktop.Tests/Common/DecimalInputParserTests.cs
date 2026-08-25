using StorePos.Desktop.Common;

namespace StorePos.Desktop.Tests.Common;

public sealed class DecimalInputParserTests
{
    [Theory]
    [InlineData("0.444", 0.444)]
    [InlineData("0,444", 0.444)]
    public void TryParse_AcceptsDotAndComma(string input, decimal expected)
    {
        var success = DecimalInputParser.TryParse(input, out var result);

        Assert.True(success);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(500, "500")]
    [InlineData(0.44, "0.44")]
    [InlineData(220, "220")]
    public void Format_RemovesUnnecessaryTrailingZeros(decimal value, string expected)
    {
        Assert.Equal(expected, DecimalInputParser.Format(value));
    }
}
