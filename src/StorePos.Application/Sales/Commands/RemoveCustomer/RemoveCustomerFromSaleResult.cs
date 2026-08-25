namespace StorePos.Application.Sales.Commands.RemoveCustomer;

public sealed record RemoveCustomerFromSaleResult(
    long SaleId,
    long? CustomerId,
    string? CustomerName,
    string? CustomerIdentificationNumber,
    string? SaleComment);
