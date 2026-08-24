using StorePos.Domain.Aggregates.Product;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Repositories;

public sealed class ProductRepository(StorePosDbContext context)
    : Repository<Product, long>(context), IProductRepository;
