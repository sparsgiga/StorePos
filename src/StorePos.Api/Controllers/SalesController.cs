using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StorePos.Api.Contracts.Sales;
using StorePos.Application.Sales.Commands.AddManualItem;
using StorePos.Application.Sales.Commands.Cancel;
using StorePos.Application.Sales.Commands.Complete;
using StorePos.Application.Sales.Commands.CreateDraft;
using StorePos.Application.Sales.Commands.RemoveItem;
using StorePos.Application.Sales.Commands.Reopen;
using StorePos.Application.Sales.Commands.UpdateDraftInfo;
using StorePos.Application.Sales.Commands.UpdateItem;
using StorePos.Application.Sales.Queries.GetDetails;
using StorePos.Application.Sales.Queries.GetDraftDetails;
using StorePos.Application.Sales.Queries.GetDrafts;
using StorePos.Application.Sales.Queries.GetHistory;
using StorePos.Application.Sales.Queries.GetSoldProducts;
using StorePos.Application.Common.Models;
using StorePos.Domain.Enums;

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

    [HttpPut("drafts/{saleId:long}/info")]
    public async Task<ActionResult<UpdateDraftSaleInfoResult>> UpdateDraftSaleInfo(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        UpdateDraftSaleInfoRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDraftSaleInfoCommand(
            saleId,
            request.CustomerName,
            request.CustomerIdentificationNumber,
            request.Comment);

        var result = await sender.Send(command, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{saleId:long}/items/{saleItemId:long}")]
    public async Task<ActionResult<UpdateSaleItemResult>> UpdateSaleItem(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        [FromRoute, Range(1, long.MaxValue)] long saleItemId,
        UpdateSaleItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSaleItemCommand(
            saleId,
            saleItemId,
            request.ProductName,
            request.Quantity,
            request.UnitPrice,
            request.Comment);

        var result = await sender.Send(command, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{saleId:long}/items/{saleItemId:long}")]
    public async Task<ActionResult<RemoveSaleItemResult>> RemoveSaleItem(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        [FromRoute, Range(1, long.MaxValue)] long saleItemId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RemoveSaleItemCommand(saleId, saleItemId),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{saleId:long}/complete")]
    public async Task<ActionResult<CompleteSaleResult>> CompleteSale(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        CompleteSaleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CompleteSaleCommand(
            saleId,
            request.Payments
                .Select(payment => new CompleteSalePayment(
                    payment.PaymentType,
                    payment.Amount))
                .ToArray());

        var result = await sender.Send(command, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{saleId:long}/cancel")]
    public async Task<ActionResult<CancelSaleResult>> CancelSale(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CancelSaleCommand(saleId),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("history")]
    public async Task<ActionResult<PagedResult<SalesHistoryItemModel>>> GetSalesHistory(
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] string? saleNumber = null,
        [FromQuery] string? customerName = null,
        [FromQuery] SaleStatus? status = null,
        [FromQuery, Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery, Range(1, 200)] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetSalesHistoryQuery(
                dateFrom,
                dateTo,
                saleNumber,
                customerName,
                status,
                pageNumber,
                pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("sold-items")]
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
    {
        var result = await sender.Send(
            new GetSoldProductsQuery(
                dateFrom,
                dateTo,
                productSearch,
                saleNumber,
                customerName,
                isManual,
                pageNumber,
                pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{saleId:long}")]
    public async Task<ActionResult<SaleDetailsModel>> GetSaleDetails(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetSaleDetailsQuery(saleId),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{saleId:long}/reopen")]
    public async Task<ActionResult<ReopenSaleResult>> ReopenSale(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ReopenSaleCommand(saleId),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }
}
