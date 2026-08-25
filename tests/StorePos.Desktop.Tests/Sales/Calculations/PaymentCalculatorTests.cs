using StorePos.Desktop.Sales.Calculations;

namespace StorePos.Desktop.Tests.Sales.Calculations;

public sealed class PaymentCalculatorTests
{
    [Fact]
    public void CashAndCard_CalculatesPaidAndRemainingAmounts()
    {
        var result = PaymentCalculator.Calculate(272m, [100m, 172m]);

        Assert.Equal(272m, result.PaidAmount);
        Assert.Equal(0m, result.RemainingAmount);
    }

    [Fact]
    public void ExactPayment_CanComplete()
    {
        var result = PaymentCalculator.Calculate(272m, [100m, 172m]);

        Assert.True(result.IsValid);
        Assert.True(result.CanComplete);
    }

    [Fact]
    public void Underpayment_CannotComplete()
    {
        var result = PaymentCalculator.Calculate(272m, [100m, 150m]);

        Assert.Equal(22m, result.RemainingAmount);
        Assert.False(result.CanComplete);
    }

    [Fact]
    public void Overpayment_CannotComplete()
    {
        var result = PaymentCalculator.Calculate(272m, [300m]);

        Assert.Equal(-28m, result.RemainingAmount);
        Assert.False(result.CanComplete);
    }

    [Fact]
    public void NegativePayment_IsInvalidAndCannotComplete()
    {
        var result = PaymentCalculator.Calculate(272m, [-1m, 273m]);

        Assert.False(result.IsValid);
        Assert.False(result.CanComplete);
    }

    [Fact]
    public void Comparison_UsesFiveDecimalPrecision()
    {
        var result = PaymentCalculator.Calculate(0.333333m, [0.33333m]);

        Assert.True(result.CanComplete);
        Assert.Equal(0m, result.RemainingAmount);
    }
}
