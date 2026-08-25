using StorePos.Domain.Interfaces;

namespace StorePos.Domain.Aggregates.Customer;

public interface ICustomerRepository :
    IRepository<Customer, long>,
    IQueryRepository<Customer, long>
{
    Task<bool> IdentificationNumberExistsAsync(
        string identificationNumber,
        long? excludedCustomerId = null,
        CancellationToken cancellationToken = default);
}
