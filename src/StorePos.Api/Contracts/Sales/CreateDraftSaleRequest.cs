namespace StorePos.Api.Contracts.Sales;

public sealed record CreateDraftSaleRequest(
    long? CashierId = null,
    string? CustomerName = null,
    string? CustomerIdentificationNumber = null,
    string? Comment = null);
