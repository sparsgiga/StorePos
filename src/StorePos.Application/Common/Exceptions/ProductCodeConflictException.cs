namespace StorePos.Application.Common.Exceptions;

public sealed class ProductCodeConflictException(string code)
    : Exception($"პროდუქტი კოდით „{code}“ უკვე არსებობს.")
{
    public string Code { get; } = code;
}
