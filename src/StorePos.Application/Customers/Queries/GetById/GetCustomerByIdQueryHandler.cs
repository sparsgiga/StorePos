using MediatR;
using StorePos.Application.Common.Interfaces;
using StorePos.Application.Customers.Queries.Search;

namespace StorePos.Application.Customers.Queries.GetById;

public sealed class GetCustomerByIdQueryHandler(ICustomerReadService customerReadService)
    : IRequestHandler<GetCustomerByIdQuery, CustomerSearchResult?>
{
    public Task<CustomerSearchResult?> Handle(
        GetCustomerByIdQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.CustomerId);
        return customerReadService.GetByIdAsync(request.CustomerId, cancellationToken);
    }
}
