namespace StorePos.Desktop.Sales.Models;

public sealed record CompleteSaleRequest(
    IReadOnlyList<CompleteSalePaymentRequest> Payments,
    bool AllowDebt);

public sealed record CompleteSalePaymentRequest(
    int PaymentType,
    decimal Amount);
