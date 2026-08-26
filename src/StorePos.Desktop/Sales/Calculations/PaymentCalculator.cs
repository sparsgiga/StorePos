namespace StorePos.Desktop.Sales.Calculations;

public static class PaymentCalculator
{
    public static PaymentCalculation Calculate(
        decimal totalAmount,
        IEnumerable<decimal> paymentAmounts,
        bool allowDebt = false,
        bool hasCustomer = false)
    {
        ArgumentNullException.ThrowIfNull(paymentAmounts);

        var amounts = paymentAmounts.ToArray();
        var isValid = amounts.All(amount => amount >= 0);
        var normalizedAmounts = amounts
            .Select(FinancialInputPrecision.RoundMoney)
            .ToArray();
        decimal paidAmount;
        try
        {
            paidAmount = FinancialInputPrecision.RoundMoney(normalizedAmounts.Sum());
        }
        catch (OverflowException)
        {
            return new PaymentCalculation(0m, 0m, false, false);
        }

        var roundedTotal = FinancialInputPrecision.RoundMoney(totalAmount);
        var remainingAmount = FinancialInputPrecision.RoundMoney(
            roundedTotal - paidAmount);

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
}
