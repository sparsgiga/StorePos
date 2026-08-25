namespace StorePos.Api.Contracts.Customers;

public sealed record UpdateCustomerRequest(
    string Name,
    string? IdentificationNumber = null,
    string? Information = null);
