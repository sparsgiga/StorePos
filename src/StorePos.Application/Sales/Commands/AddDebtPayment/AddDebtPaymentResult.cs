using StorePos.Domain.Enums;

namespace StorePos.Application.Sales.Commands.AddDebtPayment;

public sealed record AddDebtPaymentResult(
    long SaleId,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    bool HasDebt,
    SaleDebtPaymentResult Payment);

public sealed record SaleDebtPaymentResult(
    PaymentType PaymentType,
    SalePaymentKind PaymentKind,
    decimal Amount,
    DateTime DateCreated);
