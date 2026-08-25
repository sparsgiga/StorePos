namespace StorePos.Application.Sales.Commands.AssignCustomer;

public sealed record AssignCustomerToSaleResult(
    long SaleId,
    long CustomerId,
    string CustomerName,
    string? CustomerIdentificationNumber,
    string? SaleComment);
