using StorePos.Domain.Enums;

namespace StorePos.Application.Sales.Queries.GetHistory;

public sealed record SalesHistoryItemModel(
    long Id,
    string SaleNumber,
    SaleStatus Status,
    string? CustomerName,
    string? CustomerIdentificationNumber,
    decimal TotalAmount,
    DateTime DateCreated,
    DateTime? DateCompleted,
    DateTime? DateCancelled,
    DateTime RelevantDate,
    decimal CashAmount,
    decimal CardAmount,
    decimal BankTransferAmount,
    decimal OtherAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    bool HasDebt);
