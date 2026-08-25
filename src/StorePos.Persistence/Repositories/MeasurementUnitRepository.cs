using Microsoft.EntityFrameworkCore;
using StorePos.Domain.Aggregates.MeasurementUnit;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Repositories;

public sealed class MeasurementUnitRepository(StorePosDbContext context)
    : Repository<MeasurementUnit, int>(context), IMeasurementUnitRepository
{
    public Task<MeasurementUnit?> GetActiveByIdAsync(
        int measurementUnitId,
        CancellationToken cancellationToken = default)
        => Entities.SingleOrDefaultAsync(
            unit => unit.Id == measurementUnitId && unit.IsActive,
            cancellationToken);
}
