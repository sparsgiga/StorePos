using Microsoft.EntityFrameworkCore;
using StorePos.Application.Common.Interfaces;
using StorePos.Application.Customers.Queries.Search;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Queries;

public sealed class CustomerReadService(StorePosDbContext context) : ICustomerReadService
{
    public async Task<IReadOnlyList<CustomerSearchResult>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await context.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.Name)
            .ThenBy(customer => customer.Id)
            .Select(customer => new CustomerSearchResult(
                customer.Id,
                customer.Name,
                customer.IdentificationNumber,
                customer.Information))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<CustomerSearchResult>> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
        => await context.Customers
            .AsNoTracking()
            .Where(customer =>
                customer.Name.Contains(query) ||
                customer.IdentificationNumber != null &&
                customer.IdentificationNumber.Contains(query))
            .OrderByDescending(customer => customer.IdentificationNumber == query)
            .ThenByDescending(customer => customer.Name == query)
            .ThenByDescending(customer => customer.Name.StartsWith(query))
            .ThenBy(customer => customer.Name)
            .ThenBy(customer => customer.Id)
            .Take(limit)
            .Select(customer => new CustomerSearchResult(
                customer.Id,
                customer.Name,
                customer.IdentificationNumber,
                customer.Information))
            .ToArrayAsync(cancellationToken);

    public Task<CustomerSearchResult?> GetByIdAsync(
        long customerId,
        CancellationToken cancellationToken = default)
        => context.Customers
            .AsNoTracking()
            .Where(customer => customer.Id == customerId)
            .Select(customer => new CustomerSearchResult(
                customer.Id,
                customer.Name,
                customer.IdentificationNumber,
                customer.Information))
            .SingleOrDefaultAsync(cancellationToken);
}
