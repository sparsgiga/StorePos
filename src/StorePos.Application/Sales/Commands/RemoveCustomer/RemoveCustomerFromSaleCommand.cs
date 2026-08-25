using MediatR;

namespace StorePos.Application.Sales.Commands.RemoveCustomer;

public sealed record RemoveCustomerFromSaleCommand(long SaleId)
    : IRequest<RemoveCustomerFromSaleResult?>;
