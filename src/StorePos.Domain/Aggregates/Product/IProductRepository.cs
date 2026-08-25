using StorePos.Domain.Interfaces;

namespace StorePos.Domain.Aggregates.Product;

public interface IProductRepository :
    IRepository<Product, long>,
    IQueryRepository<Product, long>
{
    Task<Product?> GetActiveByIdAsync(
        long productId,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default);
}
