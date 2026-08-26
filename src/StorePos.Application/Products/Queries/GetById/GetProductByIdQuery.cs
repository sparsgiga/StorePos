using MediatR;

namespace StorePos.Application.Products.Queries.GetById;

public sealed record GetProductByIdQuery(long ProductId)
    : IRequest<ProductDetailsResult?>;
