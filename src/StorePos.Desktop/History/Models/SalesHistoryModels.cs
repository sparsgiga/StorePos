namespace StorePos.Desktop.History.Models;

public sealed record SalesHistoryFilter(
    DateTime? DateFrom,
    DateTime? DateTo,
    string? SaleNumber,
    string? CustomerName,
    int? Status,
    int PageNumber,
    int PageSize = 50);

public sealed record SalesHistoryItemDto(
    long Id,
    string SaleNumber,
    int Status,
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
    decimal OtherAmount)
{
    public string StatusName => Status switch
    {
        1 => "Draft",
        2 => "დასრულებული",
        3 => "გაუქმებული",
        _ => "უცნობი"
    };
}

public sealed record SaleStatusFilterOption(string Label, int? Value);
