namespace StorePos.Desktop.Sales.Calculations;

public sealed record ManualItemCalculation(
    ManualItemField CalculatedField,
    decimal Value,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);
