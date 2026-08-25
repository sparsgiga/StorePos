using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StorePos.Application.Products.Queries.Search;

namespace StorePos.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<ProductSearchResult>>> Search(
        [FromQuery] string? query,
        [FromQuery, Range(1, SearchProductsQueryHandler.MaximumLimit)]
        int limit = SearchProductsQueryHandler.DefaultLimit,
        [FromQuery] bool exactOnly = false,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(
            new SearchProductsQuery(query, limit, exactOnly),
            cancellationToken));
}
