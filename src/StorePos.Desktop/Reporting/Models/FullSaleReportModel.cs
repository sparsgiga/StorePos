namespace StorePos.Desktop.Reporting.Models;

public sealed record FullSaleReportModel(
    long SaleId,
    string SaleNumber,
    int Status,
    string? CustomerName,
    string? CustomerIdentificationNumber,
    string? Comment,
    DateTime DateCreated,
    DateTime? DateCompleted,
    DateTime? DateCancelled,
    DateTime PrintedAt,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    decimal CashAmount,
    decimal CardAmount,
    decimal BankTransferAmount,
    decimal OtherAmount,
    IReadOnlyList<FullSaleReportItemModel> Items);

public sealed record FullSaleReportItemModel(
    long SaleItemId,
    string? ProductCode,
    string? Barcode,
    string ProductName,
    string? MeasurementUnitName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    bool IsManual,
    string? Comment);
