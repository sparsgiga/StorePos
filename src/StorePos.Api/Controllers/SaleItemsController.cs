using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StorePos.Api.Contracts.Sales;
using StorePos.Application.Sales.Commands.AddManualItem;
using StorePos.Application.Sales.Commands.AddProductItem;
using StorePos.Application.Sales.Commands.RemoveItem;
using StorePos.Application.Sales.Commands.UpdateItem;
using StorePos.Application.Sales.Commands.UpdateFinancials;
using StorePos.Application.Products.Commands.CreateAndAddToSale;

namespace StorePos.Api.Controllers;

[ApiController]
[Route("api/sales/{saleId:long}/items")]
public sealed class SaleItemsController(ISender sender) : ControllerBase
{
    [HttpPost("product")]
    public async Task<ActionResult<AddProductSaleItemResult>> AddProductSaleItem(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        AddProductSaleItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddProductSaleItemCommand(saleId, request.ProductId),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("product/create")]
    public async Task<ActionResult<AddProductSaleItemResult>> CreateProductAndAddSaleItem(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        CreateProductAndAddSaleItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateProductAndAddToSaleCommand(
                saleId,
                request.ProductCode,
                request.Name,
                request.Barcode,
                request.MeasurementUnitId,
                request.Quantity,
                request.UnitPrice,
                request.Comment),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("manual")]
    public async Task<ActionResult<AddManualSaleItemResult>> AddManualSaleItem(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        AddManualSaleItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddManualSaleItemCommand(
                saleId,
                request.ProductName,
                request.Quantity,
                request.UnitPrice,
                request.Comment), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{saleItemId:long}")]
    public async Task<ActionResult<UpdateSaleItemResult>> UpdateSaleItem(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        [FromRoute, Range(1, long.MaxValue)] long saleItemId,
        UpdateSaleItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateSaleItemCommand(
                saleId,
                saleItemId,
                request.ProductName,
                request.Quantity,
                request.UnitPrice,
                request.Comment), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("{saleItemId:long}/financials")]
    public async Task<ActionResult<UpdateSaleItemFinancialsResult>> UpdateFinancials(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        [FromRoute, Range(1, long.MaxValue)] long saleItemId,
        UpdateSaleItemFinancialsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateSaleItemFinancialsCommand(
                saleId,
                saleItemId,
                request.Quantity,
                request.UnitPrice,
                request.LineTotal),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{saleItemId:long}")]
    public async Task<ActionResult<RemoveSaleItemResult>> RemoveSaleItem(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        [FromRoute, Range(1, long.MaxValue)] long saleItemId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RemoveSaleItemCommand(saleId, saleItemId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
