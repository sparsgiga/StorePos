namespace StorePos.Desktop.Customers.Models;

public sealed record UpdateCustomerRequest(
    string Name,
    string? IdentificationNumber,
    string? Information);
