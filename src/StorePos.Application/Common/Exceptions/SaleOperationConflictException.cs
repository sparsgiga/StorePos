namespace StorePos.Application.Common.Exceptions;

public sealed class SaleOperationConflictException(
    string message,
    Exception? innerException = null)
    : Exception(message, innerException);
