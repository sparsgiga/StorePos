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
    public void Comparison_UsesMoneyPrecision()
    {
        var result = PaymentCalculator.Calculate(0.333333m, [0.33333m]);

        Assert.True(result.CanComplete);
        Assert.Equal(0m, result.RemainingAmount);
    }

    [Fact]
    public void SplitPaymentsAreRoundedIndividuallyBeforeSumming()
    {
        var result = PaymentCalculator.Calculate(0.02m, [0.005m, 0.005m]);

        Assert.Equal(0.02m, result.PaidAmount);
        Assert.Equal(0m, result.RemainingAmount);
        Assert.True(result.CanComplete);
    }

    [Fact]
    public void CreditWithCustomer_AllowsPartialAndZeroPayment()
    {
        var partial = PaymentCalculator.Calculate(
            200m, [100m], allowDebt: true, hasCustomer: true);
        var fullDebt = PaymentCalculator.Calculate(
            200m, [], allowDebt: true, hasCustomer: true);

        Assert.True(partial.CanComplete);
        Assert.Equal(100m, partial.RemainingAmount);
        Assert.True(fullDebt.CanComplete);
        Assert.Equal(200m, fullDebt.RemainingAmount);
    }

    [Fact]
    public void CreditWithoutCustomer_CannotLeaveOutstandingAmount()
    {
        var partial = PaymentCalculator.Calculate(
            200m, [100m], allowDebt: true, hasCustomer: false);
        var fullyPaid = PaymentCalculator.Calculate(
            200m, [200m], allowDebt: true, hasCustomer: false);

        Assert.False(partial.CanComplete);
        Assert.True(fullyPaid.CanComplete);
    }

    [Fact]
    public void CreditStillRejectsOverpayment()
    {
        var result = PaymentCalculator.Calculate(
            200m, [201m], allowDebt: true, hasCustomer: true);

        Assert.False(result.CanComplete);
    }
}
