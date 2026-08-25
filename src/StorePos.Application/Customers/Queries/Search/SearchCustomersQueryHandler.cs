using MediatR;
using StorePos.Application.Common.Interfaces;

namespace StorePos.Application.Customers.Queries.Search;

public sealed class SearchCustomersQueryHandler(ICustomerReadService customerReadService)
    : IRequestHandler<SearchCustomersQuery, IReadOnlyList<CustomerSearchResult>>
{
    public const int MinimumQueryLength = 2;
    public const int MaximumLimit = 20;

    public Task<IReadOnlyList<CustomerSearchResult>> Handle(
        SearchCustomersQuery request,
        CancellationToken cancellationToken)
    {
        var query = request.Query?.Trim();
        if (string.IsNullOrEmpty(query) || query.Length < MinimumQueryLength)
        {
            return Task.FromResult<IReadOnlyList<CustomerSearchResult>>([]);
        }

        var limit = Math.Clamp(request.Limit, 1, MaximumLimit);
        return customerReadService.SearchAsync(query, limit, cancellationToken);
    }
}
