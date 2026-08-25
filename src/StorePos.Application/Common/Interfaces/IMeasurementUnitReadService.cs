using StorePos.Application.MeasurementUnits.Queries.GetActive;

namespace StorePos.Application.Common.Interfaces;

public interface IMeasurementUnitReadService
{
    Task<IReadOnlyList<MeasurementUnitResult>> GetActiveAsync(
        CancellationToken cancellationToken = default);
}
