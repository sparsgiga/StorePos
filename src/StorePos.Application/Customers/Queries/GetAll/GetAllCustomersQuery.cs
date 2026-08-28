using MediatR;
using StorePos.Application.Customers.Queries.Search;

namespace StorePos.Application.Customers.Queries.GetAll;

public sealed record GetAllCustomersQuery
    : IRequest<IReadOnlyList<CustomerSearchResult>>;
