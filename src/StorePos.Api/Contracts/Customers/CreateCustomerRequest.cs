namespace StorePos.Api.Contracts.Customers;

public sealed record CreateCustomerRequest(
    string Name,
    string? IdentificationNumber = null,
    string? Information = null);
