namespace StorePos.Desktop.Sales.Calculations;

public static class PaymentCalculator
{
    private const int AmountScale = 5;

    public static PaymentCalculation Calculate(
        decimal totalAmount,
        IEnumerable<decimal> paymentAmounts,
        bool allowDebt = false,
        bool hasCustomer = false)
    {
        ArgumentNullException.ThrowIfNull(paymentAmounts);

        var amounts = paymentAmounts.ToArray();
        var isValid = amounts.All(amount => amount >= 0);
        var paidAmount = Round(amounts.Sum());
        var roundedTotal = Round(totalAmount);
        var remainingAmount = Round(roundedTotal - paidAmount);

        var hasActualDebt = remainingAmount > 0;
        var canComplete = isValid &&
                          remainingAmount >= 0 &&
                          (allowDebt
                              ? !hasActualDebt || hasCustomer
                              : remainingAmount == 0);

        return new PaymentCalculation(
            paidAmount,
            remainingAmount,
            isValid,
            canComplete);
    }

    private static decimal Round(decimal amount)
        => decimal.Round(amount, AmountScale, MidpointRounding.AwayFromZero);
}
