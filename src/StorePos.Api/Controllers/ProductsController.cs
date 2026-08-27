using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StorePos.Application.Products.Queries.Search;
using StorePos.Application.Products.Queries.GetCreationDefaults;
using StorePos.Api.Contracts.Products;
using StorePos.Application.Common.Models;
using StorePos.Application.Products.Commands;
using StorePos.Application.Products.Commands.Activate;
using StorePos.Application.Products.Commands.Create;
using StorePos.Application.Products.Commands.Deactivate;
using StorePos.Application.Products.Commands.Update;
using StorePos.Application.Products.Commands.UpdateRetailPrice;
using StorePos.Application.Products.Queries.GetById;
using StorePos.Application.Products.Queries.GetList;

namespace StorePos.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductListItem>>> GetList(
        [FromQuery] string? search = null,
        [FromQuery] ProductStatusFilter status = ProductStatusFilter.Active,
        [FromQuery, Range(1, int.MaxValue)] int? measurementUnitId = null,
        [FromQuery, Range(typeof(decimal), "0", "9999999999999.99999")]
        decimal? priceFrom = null,
        [FromQuery, Range(typeof(decimal), "0", "9999999999999.99999")]
        decimal? priceTo = null,
        [FromQuery, Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery, Range(1, 200)] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(
            new GetProductsQuery(
                search,
                status,
                measurementUnitId,
                priceFrom,
                priceTo,
                pageNumber,
                pageSize),
            cancellationToken));

    [HttpGet("{productId:long}")]
    public async Task<ActionResult<ProductDetailsResult>> GetById(
        [FromRoute, Range(1, long.MaxValue)] long productId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductByIdQuery(productId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ProductCommandResult>> Create(
        SaveProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateProductCommand(
                request.Code,
                request.Barcode,
                request.Name,
                request.MeasurementUnitId,
                request.Price,
                request.SupplierName,
                request.SupplierCode,
                request.CostPrice),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { productId = result.Id }, result);
    }

    [HttpPut("{productId:long}")]
    public async Task<ActionResult<ProductCommandResult>> Update(
        [FromRoute, Range(1, long.MaxValue)] long productId,
        SaveProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateProductCommand(
                productId,
                request.Code,
                request.Barcode,
                request.Name,
                request.MeasurementUnitId,
                request.Price,
                request.SupplierName,
                request.SupplierCode,
                request.CostPrice),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("{productId:long}/retail-price")]
    public async Task<ActionResult<UpdateProductRetailPriceResult>> UpdateRetailPrice(
        [FromRoute, Range(1, long.MaxValue)] long productId,
        UpdateProductRetailPriceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateProductRetailPriceCommand(productId, request.Price),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{productId:long}")]
    public async Task<ActionResult<ProductCommandResult>> Deactivate(
        [FromRoute, Range(1, long.MaxValue)] long productId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new DeactivateProductCommand(productId),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{productId:long}/activate")]
    public async Task<ActionResult<ProductCommandResult>> Activate(
        [FromRoute, Range(1, long.MaxValue)] long productId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ActivateProductCommand(productId),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("creation-defaults")]
    public async Task<ActionResult<ProductCreationDefaultsResult>> GetCreationDefaults(
        CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new GetProductCreationDefaultsQuery(),
            cancellationToken));

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<ProductSearchResult>>> Search(
        [FromQuery] string? query,
        [FromQuery, Range(1, SearchProductsQueryHandler.MaximumLimit)]
        int limit = SearchProductsQueryHandler.DefaultLimit,
        [FromQuery] bool exactOnly = false,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(
            new SearchProductsQuery(query, limit, exactOnly),
            cancellationToken));
}
