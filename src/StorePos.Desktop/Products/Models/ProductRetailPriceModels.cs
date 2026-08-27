namespace StorePos.Desktop.Products.Models;

public sealed record UpdateProductRetailPriceRequest(decimal Price);

public sealed record UpdateProductRetailPriceDto(long ProductId, decimal Price);
