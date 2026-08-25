namespace StorePos.Desktop.Customers.Models;

public sealed record CreateCustomerRequest(
    string Name,
    string? IdentificationNumber,
    string? Information);
