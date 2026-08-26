namespace StorePos.Desktop.History.Models;

public sealed record AddDebtPaymentRequest(
    Guid OperationId,
    int PaymentType,
    decimal Amount);

public sealed record AddDebtPaymentResponse(
    long SaleId,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    bool HasDebt,
    SaleDetailsPaymentDto Payment);

public sealed record PaymentTypeOption(int Value, string Name);
