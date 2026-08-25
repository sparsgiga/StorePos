using MediatR;
using StorePos.Application.Common.Models;

namespace StorePos.Application.Sales.Queries.GetSoldProducts;

public sealed record GetSoldProductsQuery(
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    string? ProductSearch = null,
    string? SaleNumber = null,
    string? CustomerName = null,
    bool? IsManual = null,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<PagedResult<SoldProductModel>>;
