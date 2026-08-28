using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StorePos.Api.Contracts.Customers;
using StorePos.Application.Customers.Commands.Create;
using StorePos.Application.Customers.Commands.Update;
using StorePos.Application.Customers.Queries.GetById;
using StorePos.Application.Customers.Queries.GetAll;
using StorePos.Application.Customers.Queries.Search;

namespace StorePos.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerSearchResult>>> GetAll(
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetAllCustomersQuery(), cancellationToken));

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<CustomerSearchResult>>> Search(
        [FromQuery] string? query,
        [FromQuery, Range(1, SearchCustomersQueryHandler.MaximumLimit)] int limit = 20,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(
            new SearchCustomersQuery(query, limit), cancellationToken));

    [HttpGet("{customerId:long}")]
    public async Task<ActionResult<CustomerSearchResult>> GetById(
        [FromRoute, Range(1, long.MaxValue)] long customerId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetCustomerByIdQuery(customerId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CreateCustomerResult>> Create(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateCustomerCommand(
                request.Name,
                request.IdentificationNumber,
                request.Information), cancellationToken);
        return Created($"/api/customers/{result.Id}", result);
    }

    [HttpPut("{customerId:long}")]
    public async Task<ActionResult<UpdateCustomerResult>> Update(
        [FromRoute, Range(1, long.MaxValue)] long customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateCustomerCommand(
                customerId,
                request.Name,
                request.IdentificationNumber,
                request.Information), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
