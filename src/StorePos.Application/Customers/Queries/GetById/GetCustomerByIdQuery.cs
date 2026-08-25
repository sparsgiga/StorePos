using MediatR;
using StorePos.Application.Customers.Queries.Search;

namespace StorePos.Application.Customers.Queries.GetById;

public sealed record GetCustomerByIdQuery(long CustomerId)
    : IRequest<CustomerSearchResult?>;
