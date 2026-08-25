using StorePos.Domain.Base;

namespace StorePos.Domain.Aggregates.Customer;

public sealed class Customer : AuditableEntity<long>, IAggregateRoot
{
    public const int NameMaxLength = 300;
    public const int IdentificationNumberMaxLength = 50;
    public const int InformationMaxLength = 2000;

    private Customer()
    {
    }

    private Customer(
        string name,
        string? identificationNumber,
        string? information)
    {
        Update(name, identificationNumber, information);
    }

    public string Name { get; private set; } = string.Empty;

    public string? IdentificationNumber { get; private set; }

    public string? Information { get; private set; }

    public static Customer Create(
        string name,
        string? identificationNumber = null,
        string? information = null)
        => new(name, identificationNumber, information);

    public void Update(
        string name,
        string? identificationNumber,
        string? information)
    {
        Name = NormalizeRequiredName(name);
        IdentificationNumber = NormalizeOptionalText(
            identificationNumber,
            IdentificationNumberMaxLength,
            nameof(identificationNumber));
        Information = NormalizeOptionalText(
            information,
            InformationMaxLength,
            nameof(information));
    }

    private static string NormalizeRequiredName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Customer name is required.", nameof(value));
        }

        var normalizedValue = value.Trim();
        if (normalizedValue.Length > NameMaxLength)
        {
            throw new ArgumentException(
                $"Customer name cannot exceed {NameMaxLength} characters.",
                nameof(value));
        }

        return normalizedValue;
    }

    private static string? NormalizeOptionalText(
        string? value,
        int maxLength,
        string parameterName)
    {
        var normalizedValue = string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

        if (normalizedValue?.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalizedValue;
    }
}
