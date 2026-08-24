using StorePos.Domain.Interfaces;

namespace StorePos.Domain.Aggregates.Product;

public interface IProductRepository :
    IRepository<Product, long>,
    IQueryRepository<Product, long>;
