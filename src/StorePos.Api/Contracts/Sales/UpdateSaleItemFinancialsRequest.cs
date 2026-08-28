namespace StorePos.Api.Contracts.Sales;

public sealed record UpdateSaleItemFinancialsRequest(
    decimal? Quantity,
    decimal? UnitPrice,
    decimal? LineTotal);
