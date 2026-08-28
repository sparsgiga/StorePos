namespace StorePos.Application.Sales.Queries.GetDraftDetails;

public sealed record DraftSaleDetailsModel(
    long Id,
    string SaleNumber,
    int CompletionVersion,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    bool HasDebt,
    DateTime DateCreated,
    long? CustomerId,
    string? CustomerName,
    string? CustomerIdentificationNumber,
    string? Comment,
    IReadOnlyList<DraftSaleItemModel> Items,
    PreviousCompletionPaymentStateModel? PreviousCompletionPaymentState);

public sealed record PreviousCompletionPaymentStateModel(
    int CompletionVersion,
    decimal CashAmount,
    decimal CardAmount,
    decimal BankTransferAmount,
    decimal OtherAmount);
