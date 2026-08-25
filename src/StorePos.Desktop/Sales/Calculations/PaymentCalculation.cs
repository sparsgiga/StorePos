namespace StorePos.Desktop.Sales.Calculations;

public sealed record PaymentCalculation(
    decimal PaidAmount,
    decimal RemainingAmount,
    bool IsValid,
    bool CanComplete);
