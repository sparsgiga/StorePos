using MediatR;
using StorePos.Application.Common.Interfaces;

namespace StorePos.Application.Products.Queries.Search;

public sealed class SearchProductsQueryHandler(IProductReadService productReadService)
    : IRequestHandler<SearchProductsQuery, IReadOnlyList<ProductSearchResult>>
{
    public const int MinimumQueryLength = 2;
    public const int DefaultLimit = 15;
    public const int MaximumLimit = 20;

    public Task<IReadOnlyList<ProductSearchResult>> Handle(
        SearchProductsQuery request,
        CancellationToken cancellationToken)
    {
        var query = request.Query?.Trim();
        if (string.IsNullOrEmpty(query) ||
            !request.ExactOnly && query.Length < MinimumQueryLength)
        {
            return Task.FromResult<IReadOnlyList<ProductSearchResult>>([]);
        }

        var limit = Math.Clamp(request.Limit, 1, MaximumLimit);
        return productReadService.SearchAsync(
            query,
            limit,
            request.ExactOnly,
            cancellationToken);
    }
}
