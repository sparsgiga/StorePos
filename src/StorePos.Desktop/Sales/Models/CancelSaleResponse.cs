namespace StorePos.Desktop.Sales.Models;

public sealed record CancelSaleResponse(
    long SaleId,
    string SaleNumber,
    int Status,
    DateTime DateCancelled);
