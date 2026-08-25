namespace StorePos.Desktop.Sales.Models;

public sealed record UpdateDraftSaleInfoRequest(
    string? CustomerName,
    string? CustomerIdentificationNumber,
    string? Comment);
