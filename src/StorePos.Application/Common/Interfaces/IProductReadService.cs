using StorePos.Application.Products.Queries.Search;

namespace StorePos.Application.Common.Interfaces;

public interface IProductReadService
{
    Task<IReadOnlyList<ProductSearchResult>> SearchAsync(
        string query,
        int limit,
        bool exactOnly,
        CancellationToken cancellationToken = default);
}
