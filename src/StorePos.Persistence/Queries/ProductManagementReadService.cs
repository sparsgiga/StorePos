using Microsoft.EntityFrameworkCore;
using StorePos.Application.Common.Interfaces;
using StorePos.Application.Common.Models;
using StorePos.Application.Products.Queries.GetById;
using StorePos.Application.Products.Queries.GetList;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Queries;

public sealed class ProductManagementReadService(StorePosDbContext context)
    : IProductManagementReadService
{
    public async Task<PagedResult<ProductListItem>> GetListAsync(
        GetProductsQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = context.Products.AsNoTracking();
        var search = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : request.Search.Trim();

        query = request.Status switch
        {
            ProductStatusFilter.Active => query.Where(product => product.IsActive),
            ProductStatusFilter.Inactive => query.Where(product => !product.IsActive),
            _ => query
        };

        if (search is not null)
        {
            query = query.Where(product =>
                product.Name.Contains(search) ||
                product.Code.Contains(search) ||
                product.Barcode != null && product.Barcode.Contains(search) ||
                product.SupplierName != null && product.SupplierName.Contains(search) ||
                product.SupplierCode != null && product.SupplierCode.Contains(search));
        }

        if (request.MeasurementUnitId.HasValue)
        {
            query = query.Where(product =>
                product.MeasurementUnitId == request.MeasurementUnitId.Value);
        }

        if (request.PriceFrom.HasValue)
        {
            query = query.Where(product => product.Price >= request.PriceFrom.Value);
        }

        if (request.PriceTo.HasValue)
        {
            query = query.Where(product => product.Price <= request.PriceTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await (
                from product in query
                join unit in context.MeasurementUnits.AsNoTracking()
                    on product.MeasurementUnitId equals unit.Id
                orderby product.Id
                select new ProductListItem(
                    product.Id,
                    product.Code,
                    product.Barcode,
                    product.Name,
                    unit.Id,
                    unit.Name,
                    unit.ShortName,
                    product.Price,
                    product.IsActive,
                    product.SupplierName,
                    product.SupplierCode,
                    product.CostPrice))
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<ProductListItem>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }

    public Task<ProductDetailsResult?> GetByIdAsync(
        long productId,
        CancellationToken cancellationToken = default)
        => (
                from product in context.Products.AsNoTracking()
                join unit in context.MeasurementUnits.AsNoTracking()
                    on product.MeasurementUnitId equals unit.Id
                where product.Id == productId
                select new ProductDetailsResult(
                    product.Id,
                    product.Code,
                    product.Barcode,
                    product.Name,
                    unit.Id,
                    unit.Name,
                    unit.ShortName,
                    product.Price,
                    product.IsActive,
                    product.SupplierName,
                    product.SupplierCode,
                    product.CostPrice))
            .SingleOrDefaultAsync(cancellationToken);
}
