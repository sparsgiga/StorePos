namespace StorePos.Desktop.Products.Models;

public sealed record MeasurementUnitDto(
    int Id,
    string Name,
    string? ShortName,
    string? Code)
{
    public string DisplayName => string.IsNullOrWhiteSpace(ShortName)
        ? Name
        : $"{Name} ({ShortName})";
}
