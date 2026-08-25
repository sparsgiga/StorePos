namespace StorePos.Desktop.Sales.Calculations;

public static class PaymentCalculator
{
    private const int AmountScale = 5;

    public static PaymentCalculation Calculate(
        decimal totalAmount,
        IEnumerable<decimal> paymentAmounts)
    {
        ArgumentNullException.ThrowIfNull(paymentAmounts);

        var amounts = paymentAmounts.ToArray();
        var isValid = amounts.All(amount => amount >= 0);
        var paidAmount = Round(amounts.Sum());
        var roundedTotal = Round(totalAmount);
        var remainingAmount = Round(roundedTotal - paidAmount);

        return new PaymentCalculation(
            paidAmount,
            remainingAmount,
            isValid,
            isValid && paidAmount == roundedTotal);
    }

    private static decimal Round(decimal amount)
        => decimal.Round(amount, AmountScale, MidpointRounding.AwayFromZero);
}
