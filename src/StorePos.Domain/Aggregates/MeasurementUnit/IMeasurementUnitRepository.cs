using StorePos.Domain.Interfaces;

namespace StorePos.Domain.Aggregates.MeasurementUnit;

public interface IMeasurementUnitRepository :
    IRepository<MeasurementUnit, int>,
    IQueryRepository<MeasurementUnit, int>;
