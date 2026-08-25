using MediatR;

namespace StorePos.Application.MeasurementUnits.Queries.GetActive;

public sealed record GetActiveMeasurementUnitsQuery
    : IRequest<IReadOnlyList<MeasurementUnitResult>>;
