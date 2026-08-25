using StorePos.Domain.Interfaces;

namespace StorePos.Domain.Aggregates.MeasurementUnit;

public interface IMeasurementUnitRepository :
    IRepository<MeasurementUnit, int>,
    IQueryRepository<MeasurementUnit, int>
{
    Task<MeasurementUnit?> GetActiveByIdAsync(
        int measurementUnitId,
        CancellationToken cancellationToken = default);
}
