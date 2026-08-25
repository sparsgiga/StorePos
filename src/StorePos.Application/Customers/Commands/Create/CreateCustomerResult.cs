namespace StorePos.Application.Customers.Commands.Create;

public sealed record CreateCustomerResult(
    long Id,
    string Name,
    string? IdentificationNumber,
    string? Information);
