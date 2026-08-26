namespace StorePos.Desktop.Reporting.Models;

public sealed record LoadingListReportModel(
    long SaleId,
    string SaleNumber,
    int Status,
    string? CustomerName,
    DateTime PrintedAt,
    string? PrintComment,
    IReadOnlyList<LoadingListReportItemModel> Items);

public sealed record LoadingListReportItemModel(
    long SaleItemId,
    string? ProductCode,
    string? Barcode,
    string ProductName,
    string? MeasurementUnitName,
    decimal LoadingQuantity,
    bool IsManual,
    string? Comment);
