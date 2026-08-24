using StorePos.Domain.Aggregates.MeasurementUnit;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Repositories;

public sealed class MeasurementUnitRepository(StorePosDbContext context)
    : Repository<MeasurementUnit, int>(context), IMeasurementUnitRepository;
