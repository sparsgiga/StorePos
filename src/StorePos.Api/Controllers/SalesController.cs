using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StorePos.Api.Contracts.Sales;
using StorePos.Application.Sales.Commands.AddManualItem;
using StorePos.Application.Sales.Commands.CreateDraft;
using StorePos.Application.Sales.Queries.GetDraftDetails;
using StorePos.Application.Sales.Queries.GetDrafts;

namespace StorePos.Api.Controllers;

[ApiController]
[Route("api/sales")]
public sealed class SalesController(ISender sender) : ControllerBase
{
    [HttpGet("drafts")]
    public async Task<ActionResult<IReadOnlyList<DraftSaleModel>>> GetDraftSales(
        CancellationToken cancellationToken)
    {
        var drafts = await sender.Send(new GetDraftSalesQuery(), cancellationToken);
        return Ok(drafts);
    }

    [HttpGet("drafts/{saleId:long}")]
    public async Task<ActionResult<DraftSaleDetailsModel>> GetDraftSaleDetails(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        CancellationToken cancellationToken)
    {
        var details = await sender.Send(
            new GetDraftSaleDetailsQuery(saleId),
            cancellationToken);

        return details is null ? NotFound() : Ok(details);
    }

    [HttpPost("drafts")]
    public async Task<ActionResult<CreateDraftSaleResult>> CreateDraftSale(
        CreateDraftSaleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateDraftSaleCommand(
            request.CashierId,
            request.CustomerName,
            request.CustomerIdentificationNumber,
            request.Comment);

        var result = await sender.Send(command, cancellationToken);
        return Created("/api/sales/drafts", result);
    }

    [HttpPost("{saleId:long}/items/manual")]
    public async Task<ActionResult<AddManualSaleItemResult>> AddManualSaleItem(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        AddManualSaleItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddManualSaleItemCommand(
            saleId,
            request.ProductName,
            request.Quantity,
            request.UnitPrice,
            request.Comment);

        var result = await sender.Send(command, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
