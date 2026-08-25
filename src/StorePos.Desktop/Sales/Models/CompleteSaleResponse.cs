namespace StorePos.Desktop.Sales.Models;

public sealed record CompleteSaleResponse(
    long SaleId,
    string SaleNumber,
    int Status,
    decimal TotalAmount,
    DateTime DateCompleted);
