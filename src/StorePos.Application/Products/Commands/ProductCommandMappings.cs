using StorePos.Domain.Aggregates.Product;

namespace StorePos.Application.Products.Commands;

internal static class ProductCommandMappings
{
    public static ProductCommandResult ToResult(this Product product)
        => new(
            product.Id,
            product.Code,
            product.Barcode,
            product.Name,
            product.MeasurementUnitId,
            product.Price,
            product.IsActive,
            product.SupplierName,
            product.SupplierCode,
            product.CostPrice);
}
