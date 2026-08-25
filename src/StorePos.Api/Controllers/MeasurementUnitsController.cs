using MediatR;
using Microsoft.AspNetCore.Mvc;
using StorePos.Application.MeasurementUnits.Queries.GetActive;

namespace StorePos.Api.Controllers;

[ApiController]
[Route("api/measurement-units")]
public sealed class MeasurementUnitsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MeasurementUnitResult>>> GetActive(
        CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new GetActiveMeasurementUnitsQuery(),
            cancellationToken));
}
