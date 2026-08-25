using MediatR;
using StorePos.Application.Common.Models;
using StorePos.Domain.Enums;

namespace StorePos.Application.Sales.Queries.GetHistory;

public sealed record GetSalesHistoryQuery(
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    string? SaleNumber = null,
    string? CustomerName = null,
    SaleStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<PagedResult<SalesHistoryItemModel>>;
