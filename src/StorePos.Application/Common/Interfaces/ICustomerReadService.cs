using StorePos.Application.Customers.Queries.Search;

namespace StorePos.Application.Common.Interfaces;

public interface ICustomerReadService
{
    Task<IReadOnlyList<CustomerSearchResult>> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default);

    Task<CustomerSearchResult?> GetByIdAsync(
        long customerId,
        CancellationToken cancellationToken = default);
}
