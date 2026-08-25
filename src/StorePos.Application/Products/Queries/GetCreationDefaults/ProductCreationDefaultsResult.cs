namespace StorePos.Application.Products.Queries.GetCreationDefaults;

public sealed record ProductCreationDefaultsResult(
    string SuggestedCode,
    int? DefaultMeasurementUnitId,
    string? DefaultMeasurementUnitName,
    string? DefaultMeasurementUnitShortName,
    string? ConfigurationMessage);
