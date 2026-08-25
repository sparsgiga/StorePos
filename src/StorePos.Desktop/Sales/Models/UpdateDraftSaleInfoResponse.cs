namespace StorePos.Desktop.Sales.Models;

public sealed record UpdateDraftSaleInfoResponse(
    long SaleId,
    string? CustomerName,
    string? CustomerIdentificationNumber,
    string? Comment);
