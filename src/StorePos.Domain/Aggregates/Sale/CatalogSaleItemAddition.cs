namespace StorePos.Domain.Aggregates.Sale;

public sealed record CatalogSaleItemAddition(SaleItem Item, bool WasNewItem);
