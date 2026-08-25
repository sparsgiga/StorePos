namespace StorePos.Application.Customers.Commands.Update;

public sealed record UpdateCustomerResult(
    long Id,
    string Name,
    string? IdentificationNumber,
    string? Information);
