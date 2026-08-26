using Microsoft.EntityFrameworkCore;
using StorePos.Domain.Aggregates.Product;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Repositories;

public sealed class ProductRepository(StorePosDbContext context)
    : Repository<Product, long>(context), IProductRepository
{
    public Task<Product?> GetActiveByIdAsync(
        long productId,
        CancellationToken cancellationToken = default)
        => Entities.SingleOrDefaultAsync(
            product => product.Id == productId && product.IsActive,
            cancellationToken);

    public Task<Product?> GetByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default)
        => Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.Barcode == barcode, cancellationToken);

    public Task<Product?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
        => Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.Code == code, cancellationToken);

    public Task<bool> CodeExistsAsync(
        string code,
        long? excludingProductId = null,
        CancellationToken cancellationToken = default)
        => Entities.AnyAsync(
            product => product.Code == code &&
                       (!excludingProductId.HasValue || product.Id != excludingProductId.Value),
            cancellationToken);

    public Task<bool> BarcodeExistsAsync(
        string barcode,
        long? excludingProductId = null,
        CancellationToken cancellationToken = default)
        => Entities.AnyAsync(
            product => product.Barcode == barcode &&
                       (!excludingProductId.HasValue || product.Id != excludingProductId.Value),
            cancellationToken);
}
