namespace StorePos.Desktop.Sales.Calculations;

public static class ManualItemCalculator
{
    public const int DecimalPlaces = FinancialInputPrecision.UnitPriceScale;

    public static bool TryCalculate(
        decimal? quantity,
        decimal? unitPrice,
        decimal? lineTotal,
        out ManualItemCalculation? calculation)
    {
        calculation = null;

        if (quantity is <= 0 || unitPrice is < 0 || lineTotal is < 0)
        {
            return false;
        }

        var suppliedValueCount =
            (quantity.HasValue ? 1 : 0) +
            (unitPrice.HasValue ? 1 : 0) +
            (lineTotal.HasValue ? 1 : 0);

        if (suppliedValueCount != 2)
        {
            return false;
        }

        try
        {
            if (quantity.HasValue && unitPrice.HasValue)
            {
                var normalizedQuantity = FinancialInputPrecision.RoundQuantity(quantity.Value);
                var normalizedUnitPrice = FinancialInputPrecision.RoundUnitPrice(unitPrice.Value);
                var canonicalLineTotal = FinancialInputPrecision.CalculateLineTotal(
                    normalizedQuantity,
                    normalizedUnitPrice);
                if (!AreCanonicalValuesValid(
                        normalizedQuantity,
                        normalizedUnitPrice,
                        canonicalLineTotal))
                {
                    return false;
                }

                calculation = new ManualItemCalculation(
                    ManualItemField.LineTotal,
                    canonicalLineTotal,
                    normalizedQuantity,
                    normalizedUnitPrice,
                    canonicalLineTotal);
                return true;
            }

            if (unitPrice.HasValue && lineTotal.HasValue)
            {
                var normalizedUnitPrice = FinancialInputPrecision.RoundUnitPrice(unitPrice.Value);
                var desiredLineTotal = FinancialInputPrecision.RoundMoney(lineTotal.Value);
                if (normalizedUnitPrice <= 0 || desiredLineTotal <= 0)
                {
                    return false;
                }

                var calculatedQuantity = FinancialInputPrecision.RoundQuantity(
                    desiredLineTotal / normalizedUnitPrice);
                var canonicalLineTotal = FinancialInputPrecision.CalculateLineTotal(
                    calculatedQuantity,
                    normalizedUnitPrice);
                if (!AreCanonicalValuesValid(
                        calculatedQuantity,
                        normalizedUnitPrice,
                        canonicalLineTotal))
                {
                    return false;
                }

                calculation = new ManualItemCalculation(
                    ManualItemField.Quantity,
                    calculatedQuantity,
                    calculatedQuantity,
                    normalizedUnitPrice,
                    canonicalLineTotal);
                return true;
            }

            if (quantity.HasValue && lineTotal.HasValue)
            {
                var normalizedQuantity = FinancialInputPrecision.RoundQuantity(quantity.Value);
                var desiredLineTotal = FinancialInputPrecision.RoundMoney(lineTotal.Value);
                if (normalizedQuantity <= 0 || desiredLineTotal <= 0)
                {
                    return false;
                }

                var calculatedUnitPrice = FinancialInputPrecision.RoundUnitPrice(
                    desiredLineTotal / normalizedQuantity);
                var canonicalLineTotal = FinancialInputPrecision.CalculateLineTotal(
                    normalizedQuantity,
                    calculatedUnitPrice);
                if (!AreCanonicalValuesValid(
                        normalizedQuantity,
                        calculatedUnitPrice,
                        canonicalLineTotal))
                {
                    return false;
                }

                calculation = new ManualItemCalculation(
                    ManualItemField.UnitPrice,
                    calculatedUnitPrice,
                    normalizedQuantity,
                    calculatedUnitPrice,
                    canonicalLineTotal);
                return true;
            }
        }
        catch (OverflowException)
        {
            return false;
        }

        return false;
    }

    public static decimal Round(decimal value)
        => FinancialInputPrecision.RoundUnitPrice(value);

    private static bool AreCanonicalValuesValid(
        decimal quantity,
        decimal unitPrice,
        decimal lineTotal)
        => quantity > 0 &&
           quantity <= FinancialInputPrecision.MaximumFiveScaleValue &&
           unitPrice >= 0.00001m &&
           unitPrice <= FinancialInputPrecision.MaximumFiveScaleValue &&
           lineTotal >= 0.01m &&
           lineTotal <= FinancialInputPrecision.MaximumMoneyValue;
}
