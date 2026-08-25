using StorePos.Application.Common.Interfaces;
using StorePos.Application.Customers.Queries.Search;

namespace StorePos.Application.Tests.Customers;

public sealed class SearchCustomersQueryHandlerTests
{
    [Fact]
    public async Task ShortQuery_ReturnsEmptyWithoutCallingReadService()
    {
        var readService = new StubCustomerReadService();
        var handler = new SearchCustomersQueryHandler(readService);

        var result = await handler.Handle(
            new SearchCustomersQuery("a"),
            CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal(0, readService.SearchCallCount);
    }

    [Fact]
    public async Task LimitAboveMaximum_IsClamped()
    {
        var readService = new StubCustomerReadService();
        var handler = new SearchCustomersQueryHandler(readService);

        await handler.Handle(
            new SearchCustomersQuery("Customer", 100),
            CancellationToken.None);

        Assert.Equal(SearchCustomersQueryHandler.MaximumLimit, readService.LastLimit);
    }

    private sealed class StubCustomerReadService : ICustomerReadService
    {
        public int SearchCallCount { get; private set; }
        public int LastLimit { get; private set; }

        public Task<IReadOnlyList<CustomerSearchResult>> SearchAsync(
            string query,
            int limit,
            CancellationToken cancellationToken = default)
        {
            SearchCallCount++;
            LastLimit = limit;
            return Task.FromResult<IReadOnlyList<CustomerSearchResult>>([]);
        }

        public Task<CustomerSearchResult?> GetByIdAsync(
            long customerId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<CustomerSearchResult?>(null);
    }
}
