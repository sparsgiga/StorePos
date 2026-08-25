namespace StorePos.Application.Common.Exceptions;

public sealed class ProductCodeConflictException(string code)
    : Exception($"A product with code '{code}' already exists.")
{
    public string Code { get; } = code;
}
