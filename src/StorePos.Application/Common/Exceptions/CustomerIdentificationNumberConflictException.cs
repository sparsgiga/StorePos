namespace StorePos.Application.Common.Exceptions;

public sealed class CustomerIdentificationNumberConflictException(string identificationNumber)
    : Exception($"A customer with identification number '{identificationNumber}' already exists.")
{
    public string IdentificationNumber { get; } = identificationNumber;
}
