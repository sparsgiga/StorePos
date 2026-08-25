namespace StorePos.Desktop.Sales.Models;

public sealed record SaleCustomerResponse(
    long SaleId,
    long? CustomerId,
    string? CustomerName,
    string? CustomerIdentificationNumber,
    string? SaleComment);
