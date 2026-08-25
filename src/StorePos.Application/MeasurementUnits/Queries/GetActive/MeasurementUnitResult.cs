namespace StorePos.Application.MeasurementUnits.Queries.GetActive;

public sealed record MeasurementUnitResult(
    int Id,
    string Name,
    string? ShortName,
    string? Code);
