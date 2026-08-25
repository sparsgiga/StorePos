namespace StorePos.Desktop.Sales.Models;

public sealed record CompleteSaleResponse(
    long SaleId,
    string SaleNumber,
    int Status,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    bool HasDebt,
    DateTime DateCompleted);
