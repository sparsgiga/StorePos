using MediatR;
using StorePos.Application.Common.Interfaces;
using StorePos.Application.Customers.Queries.Search;

namespace StorePos.Application.Customers.Queries.GetAll;

public sealed class GetAllCustomersQueryHandler(ICustomerReadService customerReadService)
    : IRequestHandler<GetAllCustomersQuery, IReadOnlyList<CustomerSearchResult>>
{
    public Task<IReadOnlyList<CustomerSearchResult>> Handle(
        GetAllCustomersQuery request,
        CancellationToken cancellationToken)
        => customerReadService.GetAllAsync(cancellationToken);
}
