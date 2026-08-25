using MediatR;

namespace StorePos.Application.Customers.Commands.Create;

public sealed record CreateCustomerCommand(
    string Name,
    string? IdentificationNumber = null,
    string? Information = null) : IRequest<CreateCustomerResult>;
