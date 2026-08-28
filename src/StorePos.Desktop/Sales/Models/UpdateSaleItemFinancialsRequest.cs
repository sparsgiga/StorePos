namespace StorePos.Desktop.Sales.Models;

public sealed record UpdateSaleItemFinancialsRequest(
    decimal? Quantity = null,
    decimal? UnitPrice = null,
    decimal? LineTotal = null);
