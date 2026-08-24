namespace StorePos.Desktop.Sales.Models;

public sealed record CreateDraftSaleRequest(
    long? CashierId = null,
    string? CustomerName = null,
    string? CustomerIdentificationNumber = null,
    string? Comment = null);
