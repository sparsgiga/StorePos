using StorePos.Domain.Enums;

namespace StorePos.Application.Sales.Commands.Complete;

public sealed record CompleteSaleResult(
    long SaleId,
    string SaleNumber,
    SaleStatus Status,
    decimal TotalAmount,
    DateTime DateCompleted);
