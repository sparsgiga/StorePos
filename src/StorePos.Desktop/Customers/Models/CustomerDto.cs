namespace StorePos.Desktop.Customers.Models;

public sealed record CustomerDto(
    long Id,
    string Name,
    string? IdentificationNumber,
    string? Information);
