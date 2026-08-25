using MediatR;

namespace StorePos.Application.Customers.Commands.Update;

public sealed record UpdateCustomerCommand(
    long CustomerId,
    string Name,
    string? IdentificationNumber = null,
    string? Information = null) : IRequest<UpdateCustomerResult?>;
