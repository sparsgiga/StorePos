namespace StorePos.Desktop.Sales.Models;

public sealed record UpdateSaleItemFinancialsResponse(
    long SaleId,
    long SaleItemId,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    decimal SaleSubtotal,
    decimal SaleDiscountAmount,
    decimal SaleTotalAmount,
    bool RequestedLineTotalAdjusted);
