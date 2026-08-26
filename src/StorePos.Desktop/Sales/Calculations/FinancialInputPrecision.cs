namespace StorePos.Desktop.Sales.Calculations;

public static class FinancialInputPrecision
{
    public const int MoneyScale = 2;
    public const int QuantityScale = 5;
    public const int UnitPriceScale = 5;

    public const decimal MaximumMoneyValue = 9999999999999999.99m;
    public const decimal MaximumFiveScaleValue = 9999999999999.99999m;

    public static decimal RoundMoney(decimal value)
        => decimal.Round(value, MoneyScale, MidpointRounding.AwayFromZero);

    public static decimal RoundQuantity(decimal value)
        => decimal.Round(value, QuantityScale, MidpointRounding.AwayFromZero);

    public static decimal RoundUnitPrice(decimal value)
        => decimal.Round(value, UnitPriceScale, MidpointRounding.AwayFromZero);

    public static decimal CalculateLineTotal(decimal quantity, decimal unitPrice)
    {
        var result = checked(RoundQuantity(quantity) * RoundUnitPrice(unitPrice));
        return RoundMoney(result);
    }
}
