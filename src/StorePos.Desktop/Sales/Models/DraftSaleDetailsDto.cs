namespace StorePos.Desktop.Sales.Models;

public sealed record DraftSaleDetailsDto(
    long Id,
    string SaleNumber,
    int CompletionVersion,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    bool HasDebt,
    DateTime DateCreated,
    long? CustomerId,
    string? CustomerName,
    string? CustomerIdentificationNumber,
    string? Comment,
    IReadOnlyList<DraftSaleItemDto> Items,
    PreviousCompletionPaymentStateDto? PreviousCompletionPaymentState);

public sealed record PreviousCompletionPaymentStateDto(
    int CompletionVersion,
    decimal CashAmount,
    decimal CardAmount,
    decimal BankTransferAmount,
    decimal OtherAmount);
