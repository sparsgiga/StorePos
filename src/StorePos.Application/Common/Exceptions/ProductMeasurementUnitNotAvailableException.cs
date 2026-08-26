namespace StorePos.Application.Common.Exceptions;

public sealed class ProductMeasurementUnitNotAvailableException(int measurementUnitId)
    : Exception($"Measurement unit with ID {measurementUnitId} does not exist or is inactive.");
