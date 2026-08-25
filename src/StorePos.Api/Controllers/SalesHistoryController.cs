using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StorePos.Application.Common.Models;
using StorePos.Application.Sales.Queries.GetHistory;
using StorePos.Domain.Enums;

namespace StorePos.Api.Controllers;

[ApiController]
[Route("api/sales/history")]
public sealed class SalesHistoryController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SalesHistoryItemModel>>> GetSalesHistory(
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] string? saleNumber = null,
        [FromQuery] string? customerName = null,
        [FromQuery] SaleStatus? status = null,
        [FromQuery, Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery, Range(1, 200)] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(
            new GetSalesHistoryQuery(
                dateFrom, dateTo, saleNumber, customerName, status, pageNumber, pageSize),
            cancellationToken));
}
