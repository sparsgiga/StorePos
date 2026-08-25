using StorePos.Domain.Enums;

namespace StorePos.Application.Sales.Commands.Reopen;

public sealed record ReopenSaleResult(
    long SaleId,
    string SaleNumber,
    SaleStatus Status,
    decimal TotalAmount);
