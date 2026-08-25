using MediatR;

namespace StorePos.Application.Sales.Commands.AssignCustomer;

public sealed record AssignCustomerToSaleCommand(
    long SaleId,
    long CustomerId) : IRequest<AssignCustomerToSaleResult?>;
