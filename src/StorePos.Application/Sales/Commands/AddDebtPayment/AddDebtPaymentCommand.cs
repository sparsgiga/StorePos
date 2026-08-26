using MediatR;
using StorePos.Application.Common.Behaviors;
using StorePos.Domain.Enums;

namespace StorePos.Application.Sales.Commands.AddDebtPayment;

public sealed record AddDebtPaymentCommand(
    long SaleId,
    Guid OperationId,
    PaymentType PaymentType,
    decimal Amount)
    : IRequest<AddDebtPaymentResult?>, ITransactionalRequest;
