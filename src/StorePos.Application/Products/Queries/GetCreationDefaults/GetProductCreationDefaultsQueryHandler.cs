using MediatR;
using StorePos.Application.Common.Interfaces;

namespace StorePos.Application.Products.Queries.GetCreationDefaults;

public sealed class GetProductCreationDefaultsQueryHandler(
    IProductCreationDefaultsReadService readService)
    : IRequestHandler<GetProductCreationDefaultsQuery, ProductCreationDefaultsResult>
{
    public Task<ProductCreationDefaultsResult> Handle(
        GetProductCreationDefaultsQuery request,
        CancellationToken cancellationToken)
        => readService.GetAsync(cancellationToken);
}
