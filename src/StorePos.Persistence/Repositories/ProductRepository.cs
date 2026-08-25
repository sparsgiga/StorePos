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
            .FirstOrDefaultAsync(product => product.Barcode == barcode, cancellationToken);
}
