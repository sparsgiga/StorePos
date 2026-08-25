using MediatR;
using StorePos.Domain.Enums;

namespace StorePos.Application.Sales.Commands.AddDebtPayment;

public sealed record AddDebtPaymentCommand(
    long SaleId,
    PaymentType PaymentType,
    decimal Amount)
    : IRequest<AddDebtPaymentResult?>;
