namespace StorePos.Desktop.Products.Models;

public sealed record ProductCreationDefaultsDto(
    string SuggestedCode,
    int? DefaultMeasurementUnitId,
    string? DefaultMeasurementUnitName,
    string? DefaultMeasurementUnitShortName,
    string? ConfigurationMessage);
