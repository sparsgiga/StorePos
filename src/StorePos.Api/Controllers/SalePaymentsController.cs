using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StorePos.Api.Contracts.Sales;
using StorePos.Application.Sales.Commands.AddDebtPayment;

namespace StorePos.Api.Controllers;

[ApiController]
[Route("api/sales/{saleId:long}/debt-payments")]
public sealed class SalePaymentsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AddDebtPaymentResult>> AddDebtPayment(
        [FromRoute, Range(1, long.MaxValue)] long saleId,
        AddDebtPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddDebtPaymentCommand(saleId, request.PaymentType, request.Amount),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
