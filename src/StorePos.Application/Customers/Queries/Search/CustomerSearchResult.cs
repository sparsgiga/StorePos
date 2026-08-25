namespace StorePos.Application.Customers.Queries.Search;

public sealed record CustomerSearchResult(
    long Id,
    string Name,
    string? IdentificationNumber,
    string? Information);
