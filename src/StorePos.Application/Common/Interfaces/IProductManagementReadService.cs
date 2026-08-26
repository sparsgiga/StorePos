using StorePos.Application.Common.Models;
using StorePos.Application.Products.Queries.GetById;
using StorePos.Application.Products.Queries.GetList;

namespace StorePos.Application.Common.Interfaces;

public interface IProductManagementReadService
{
    Task<PagedResult<ProductListItem>> GetListAsync(
        GetProductsQuery query,
        CancellationToken cancellationToken = default);

    Task<ProductDetailsResult?> GetByIdAsync(
        long productId,
        CancellationToken cancellationToken = default);
}
