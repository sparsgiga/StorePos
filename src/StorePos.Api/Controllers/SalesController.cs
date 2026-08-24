using MediatR;
using Microsoft.AspNetCore.Mvc;
using StorePos.Api.Contracts.Sales;
using StorePos.Application.Sales.Commands.CreateDraft;
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
}
