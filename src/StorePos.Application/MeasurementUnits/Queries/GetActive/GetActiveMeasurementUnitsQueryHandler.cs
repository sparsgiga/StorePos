using MediatR;
using StorePos.Application.Common.Interfaces;

namespace StorePos.Application.MeasurementUnits.Queries.GetActive;

public sealed class GetActiveMeasurementUnitsQueryHandler(
    IMeasurementUnitReadService measurementUnitReadService)
    : IRequestHandler<GetActiveMeasurementUnitsQuery, IReadOnlyList<MeasurementUnitResult>>
{
    public Task<IReadOnlyList<MeasurementUnitResult>> Handle(
        GetActiveMeasurementUnitsQuery request,
        CancellationToken cancellationToken)
        => measurementUnitReadService.GetActiveAsync(cancellationToken);
}
