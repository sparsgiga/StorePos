using StorePos.Domain.Enums;

namespace StorePos.Application.Sales.Queries.GetDetails;

public sealed record SaleDetailsModel(
    long Id,
    string SaleNumber,
    SaleStatus Status,
    int CompletionVersion,
    long? CustomerId,
    string? CustomerName,
    string? CustomerIdentificationNumber,
    string? Comment,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    bool HasDebt,
    DateTime DateCreated,
    DateTime? DateCompleted,
    DateTime? DateCancelled,
    IReadOnlyList<SaleDetailsItemModel> Items,
    IReadOnlyList<SaleDetailsPaymentModel> Payments);

public sealed record SaleDetailsItemModel(
    long Id,
    long? ProductId,
    string? ProductCode,
    string? Barcode,
    string ProductName,
    int? UnitId,
    string? UnitName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    bool IsManual,
    string? Comment);

public sealed record SaleDetailsPaymentModel(
    int CompletionVersion,
    PaymentType PaymentType,
    SalePaymentKind PaymentKind,
    decimal Amount,
    DateTime DateCreated);
