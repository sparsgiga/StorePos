using StorePos.Application.Products.Queries.GetCreationDefaults;

namespace StorePos.Application.Common.Interfaces;

public interface IProductCreationDefaultsReadService
{
    Task<ProductCreationDefaultsResult> GetAsync(
        CancellationToken cancellationToken = default);
}
