using Microsoft.EntityFrameworkCore;
using StorePos.Domain.Aggregates.Customer;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Repositories;

public sealed class CustomerRepository(StorePosDbContext context)
    : Repository<Customer, long>(context), ICustomerRepository
{
    public Task<bool> IdentificationNumberExistsAsync(
        string identificationNumber,
        long? excludedCustomerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = Entities.Where(customer =>
            customer.IdentificationNumber == identificationNumber);

        if (excludedCustomerId.HasValue)
        {
            query = query.Where(customer => customer.Id != excludedCustomerId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }
}
