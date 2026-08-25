using MediatR;

namespace StorePos.Application.Customers.Queries.Search;

public sealed record SearchCustomersQuery(
    string? Query,
    int Limit = 20) : IRequest<IReadOnlyList<CustomerSearchResult>>;
