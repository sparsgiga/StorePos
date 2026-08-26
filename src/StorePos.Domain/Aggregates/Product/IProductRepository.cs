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

    Task<Product?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        string code,
        long? excludingProductId = null,
        CancellationToken cancellationToken = default);

    Task<bool> BarcodeExistsAsync(
        string barcode,
        long? excludingProductId = null,
        CancellationToken cancellationToken = default);
}
