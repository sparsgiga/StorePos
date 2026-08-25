using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StorePos.Api.Contracts.Sales;
using StorePos.Application.Sales.Commands.AssignCustomer;
using StorePos.Application.Sales.Commands.Cancel;
using StorePos.Application.Sales.Commands.Complete;
using StorePos.Application.Sales.Commands.CreateDraft;
using StorePos.Application.Sales.Commands.RemoveCustomer;
using StorePos.Application.Sales.Commands.Reopen;
using StorePos.Application.Sales.Commands.UpdateComment;
using StorePos.Application.Sales.Queries.GetDetails;
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
        => Ok(await sender.Send(new GetDraftSalesQuery(), cancellationToken));

    [HttpGet("drafts/{saleId:long}")]
    public async Task<ActionResult<DraftSaleDetailsModel>> GetDraftSaleDetails(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetDraftSaleDetailsQuery(saleId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("drafts")]
    public async Task<ActionResult<CreateDraftSaleResult>> CreateDraftSale(
        CreateDraftSaleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateDraftSaleCommand(
                request.CashierId,
                request.CustomerName,
                request.CustomerIdentificationNumber,
                request.Comment),
            cancellationToken);
        return Created("/api/sales/drafts", result);
    }

    [HttpGet("{saleId:long}")]
    public async Task<ActionResult<SaleDetailsModel>> GetSaleDetails(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSaleDetailsQuery(saleId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{saleId:long}/complete")]
    public async Task<ActionResult<CompleteSaleResult>> CompleteSale(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        CompleteSaleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CompleteSaleCommand(
                saleId,
                request.Payments.Select(payment => new CompleteSalePayment(
                    payment.PaymentType,
                    payment.Amount)).ToArray()),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{saleId:long}/cancel")]
    public async Task<ActionResult<CancelSaleResult>> CancelSale(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelSaleCommand(saleId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{saleId:long}/reopen")]
    public async Task<ActionResult<ReopenSaleResult>> ReopenSale(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReopenSaleCommand(saleId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{saleId:long}/customer")]
    public async Task<ActionResult<AssignCustomerToSaleResult>> AssignCustomer(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        AssignCustomerToSaleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AssignCustomerToSaleCommand(saleId, request.CustomerId),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{saleId:long}/customer")]
    public async Task<ActionResult<RemoveCustomerFromSaleResult>> RemoveCustomer(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RemoveCustomerFromSaleCommand(saleId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{saleId:long}/comment")]
    public async Task<ActionResult<UpdateSaleCommentResult>> UpdateComment(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        UpdateSaleCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateSaleCommentCommand(saleId, request.Comment), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
