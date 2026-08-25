using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StorePos.Application.Common.Models;
using StorePos.Application.Sales.Queries.GetSoldProducts;

namespace StorePos.Api.Controllers;

[ApiController]
[Route("api/sales/sold-items")]
public sealed class SoldProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SoldProductModel>>> GetSoldProducts(
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] string? productSearch = null,
        [FromQuery] string? saleNumber = null,
        [FromQuery] string? customerName = null,
        [FromQuery] bool? isManual = null,
        [FromQuery, Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery, Range(1, 200)] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(
            new GetSoldProductsQuery(
                dateFrom,
                dateTo,
                productSearch,
                saleNumber,
                customerName,
                isManual,
                pageNumber,
                pageSize), cancellationToken));
}
