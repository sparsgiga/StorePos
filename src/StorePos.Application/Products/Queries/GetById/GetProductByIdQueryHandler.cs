using MediatR;
using StorePos.Application.Common.Interfaces;

namespace StorePos.Application.Products.Queries.GetById;

public sealed class GetProductByIdQueryHandler(IProductManagementReadService readService)
    : IRequestHandler<GetProductByIdQuery, ProductDetailsResult?>
{
    public Task<ProductDetailsResult?> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
        => readService.GetByIdAsync(request.ProductId, cancellationToken);
}
