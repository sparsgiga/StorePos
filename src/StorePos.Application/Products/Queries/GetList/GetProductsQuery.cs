using MediatR;
using StorePos.Application.Common.Models;

namespace StorePos.Application.Products.Queries.GetList;

public sealed record GetProductsQuery(
    string? Search = null,
    ProductStatusFilter Status = ProductStatusFilter.Active,
    int? MeasurementUnitId = null,
    decimal? PriceFrom = null,
    decimal? PriceTo = null,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<PagedResult<ProductListItem>>;
