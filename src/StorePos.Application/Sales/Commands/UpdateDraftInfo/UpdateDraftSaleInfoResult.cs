namespace StorePos.Application.Sales.Commands.UpdateDraftInfo;

public sealed record UpdateDraftSaleInfoResult(
    long SaleId,
    string? CustomerName,
    string? CustomerIdentificationNumber,
    string? Comment);
