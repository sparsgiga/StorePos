using StorePos.Domain.Enums;

namespace StorePos.Application.Sales.Commands.Cancel;

public sealed record CancelSaleResult(
    long SaleId,
    string SaleNumber,
    SaleStatus Status,
    DateTime DateCancelled);
