namespace StorePos.Desktop.Sales.Models;

public sealed record CompleteSaleRequest(
    IReadOnlyList<CompleteSalePaymentRequest> Payments);

public sealed record CompleteSalePaymentRequest(
    int PaymentType,
    decimal Amount);
