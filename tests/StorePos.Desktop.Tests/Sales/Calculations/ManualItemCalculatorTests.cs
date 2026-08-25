using StorePos.Desktop.Sales.Calculations;

namespace StorePos.Desktop.Tests.Sales.Calculations;

public sealed class ManualItemCalculatorTests
{
    [Fact]
    public void QuantityAndUnitPrice_CalculatesLineTotal()
    {
        var success = ManualItemCalculator.TryCalculate(500m, 0.44m, null, out var result);

        Assert.True(success);
        Assert.Equal(ManualItemField.LineTotal, result!.CalculatedField);
        Assert.Equal(220m, result.Value);
    }

    [Fact]
    public void UnitPriceAndLineTotal_CalculatesQuantity()
    {
        var success = ManualItemCalculator.TryCalculate(null, 0.44m, 220m, out var result);

        Assert.True(success);
        Assert.Equal(ManualItemField.Quantity, result!.CalculatedField);
        Assert.Equal(500m, result.Value);
    }

    [Fact]
    public void QuantityAndLineTotal_CalculatesUnitPrice()
    {
        var success = ManualItemCalculator.TryCalculate(500m, null, 220m, out var result);

        Assert.True(success);
        Assert.Equal(ManualItemField.UnitPrice, result!.CalculatedField);
        Assert.Equal(0.44m, result.Value);
    }

    [Fact]
    public void Calculation_RoundsToFivePlacesAwayFromZero()
    {
        var success = ManualItemCalculator.TryCalculate(1m, 0.123456m, null, out var result);

        Assert.True(success);
        Assert.Equal(0.12346m, result!.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveQuantity_IsRejected(decimal quantity)
    {
        Assert.False(ManualItemCalculator.TryCalculate(quantity, 1m, null, out _));
    }

    [Fact]
    public void NegativeUnitPrice_IsRejected()
    {
        Assert.False(ManualItemCalculator.TryCalculate(1m, -0.01m, null, out _));
    }

    [Fact]
    public void NegativeLineTotal_IsRejected()
    {
        Assert.False(ManualItemCalculator.TryCalculate(1m, null, -0.01m, out _));
    }

    [Fact]
    public void ZeroUnitPriceAndPositiveTotal_CannotCalculateQuantity()
    {
        Assert.False(ManualItemCalculator.TryCalculate(null, 0m, 10m, out _));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ZeroPriceCalculation_IsValid(bool calculateLineTotal)
    {
        var success = calculateLineTotal
            ? ManualItemCalculator.TryCalculate(5m, 0m, null, out var result)
            : ManualItemCalculator.TryCalculate(5m, null, 0m, out result);

        Assert.True(success);
        Assert.Equal(0m, result!.Value);
    }
}
