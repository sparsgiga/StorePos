namespace StorePos.Application.Sales.Commands.UpdateFinancials;

public sealed record UpdateSaleItemFinancialsResult(
    long SaleId,
    long SaleItemId,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    decimal SaleSubtotal,
    decimal SaleDiscountAmount,
    decimal SaleTotalAmount,
    bool RequestedLineTotalAdjusted);
