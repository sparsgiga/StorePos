using MediatR;
using StorePos.Application.Common.Interfaces;
using StorePos.Application.Common.Models;

namespace StorePos.Application.Products.Queries.GetList;

public sealed class GetProductsQueryHandler(IProductManagementReadService readService)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductListItem>>
{
    public Task<PagedResult<ProductListItem>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
        => readService.GetListAsync(request, cancellationToken);
}
