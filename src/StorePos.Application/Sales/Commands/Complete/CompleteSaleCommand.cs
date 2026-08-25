using MediatR;
using StorePos.Domain.Enums;

namespace StorePos.Application.Sales.Commands.Complete;

public sealed record CompleteSalePayment(
    PaymentType PaymentType,
    decimal Amount);

public sealed record CompleteSaleCommand(
    long SaleId,
    IReadOnlyCollection<CompleteSalePayment> Payments,
    bool AllowDebt = false)
    : IRequest<CompleteSaleResult?>;
