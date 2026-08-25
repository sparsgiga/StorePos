namespace StorePos.Desktop.Sales.Calculations;

public static class ManualItemCalculator
{
    public const int DecimalPlaces = 5;

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
                calculation = new ManualItemCalculation(
                    ManualItemField.LineTotal,
                    Round(quantity.Value * unitPrice.Value));
                return true;
            }

            if (unitPrice.HasValue && lineTotal.HasValue)
            {
                if (unitPrice.Value == 0)
                {
                    return false;
                }

                var calculatedQuantity = Round(lineTotal.Value / unitPrice.Value);
                if (calculatedQuantity <= 0)
                {
                    return false;
                }

                calculation = new ManualItemCalculation(
                    ManualItemField.Quantity,
                    calculatedQuantity);
                return true;
            }

            if (quantity.HasValue && lineTotal.HasValue)
            {
                calculation = new ManualItemCalculation(
                    ManualItemField.UnitPrice,
                    Round(lineTotal.Value / quantity.Value));
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
        => decimal.Round(value, DecimalPlaces, MidpointRounding.AwayFromZero);
}
