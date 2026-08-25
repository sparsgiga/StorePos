using Microsoft.EntityFrameworkCore;
using StorePos.Application.Common.Interfaces;
using StorePos.Application.Products.Queries.Search;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Queries;

public sealed class ProductReadService(StorePosDbContext context) : IProductReadService
{
    public async Task<IReadOnlyList<ProductSearchResult>> SearchAsync(
        string query,
        int limit,
        bool exactOnly,
        CancellationToken cancellationToken = default)
    {
        var products = context.Products
            .AsNoTracking()
            .Where(product => product.IsActive);

        products = exactOnly
            ? products.Where(product =>
                product.Code == query || product.Barcode == query)
            : products.Where(product =>
                product.Code.Contains(query) ||
                product.Barcode != null && product.Barcode.Contains(query) ||
                product.Name.Contains(query));

        return await (
                from product in products
                join unit in context.MeasurementUnits.AsNoTracking()
                    on product.MeasurementUnitId equals unit.Id
                orderby product.Barcode == query descending,
                    product.Code == query descending,
                    product.Code.StartsWith(query) descending,
                    product.Name.StartsWith(query) descending,
                    product.Name,
                    product.Id
                select new ProductSearchResult(
                    product.Id,
                    product.Code,
                    product.Barcode,
                    product.Name,
                    unit.Id,
                    unit.Name,
                    unit.ShortName,
                    product.Price))
            .Take(limit)
            .ToArrayAsync(cancellationToken);
    }
}
