using StorePos.Domain.Base;

namespace StorePos.Domain.Aggregates.MeasurementUnit;

public sealed class MeasurementUnit : AuditableEntity<int>, IAggregateRoot
{
    private MeasurementUnit()
    {
    }

    private MeasurementUnit(string name, string? shortName, string? code)
    {
        Name = name;
        ShortName = shortName;
        Code = code;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public string? ShortName { get; private set; }

    public string? Code { get; private set; }

    public bool IsActive { get; private set; }

    public static MeasurementUnit Create(string name, string? shortName = null, string? code = null)
        => new(name, shortName, code);
}
