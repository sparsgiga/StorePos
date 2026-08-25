using Microsoft.EntityFrameworkCore;
using StorePos.Application.Common.Interfaces;
using StorePos.Application.MeasurementUnits.Queries.GetActive;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Queries;

public sealed class MeasurementUnitReadService(StorePosDbContext context)
    : IMeasurementUnitReadService
{
    public async Task<IReadOnlyList<MeasurementUnitResult>> GetActiveAsync(
        CancellationToken cancellationToken = default)
        => await context.MeasurementUnits
            .AsNoTracking()
            .Where(unit => unit.IsActive)
            .OrderBy(unit => unit.Id)
            .Select(unit => new MeasurementUnitResult(
                unit.Id,
                unit.Name,
                unit.ShortName,
                unit.Code))
            .ToArrayAsync(cancellationToken);
}
