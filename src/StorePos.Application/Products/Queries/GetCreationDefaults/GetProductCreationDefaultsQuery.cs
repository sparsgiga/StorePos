using MediatR;

namespace StorePos.Application.Products.Queries.GetCreationDefaults;

public sealed record GetProductCreationDefaultsQuery
    : IRequest<ProductCreationDefaultsResult>;
