using MediatR;

namespace StorePos.Application.Products.Queries.Search;

public sealed record SearchProductsQuery(
    string? Query,
    int Limit = SearchProductsQueryHandler.DefaultLimit,
    bool ExactOnly = false) : IRequest<IReadOnlyList<ProductSearchResult>>;
