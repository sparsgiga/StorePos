using MediatR;
using StorePos.Domain.Aggregates.MeasurementUnit;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Sales.Commands.AddProductItem;

public sealed class AddProductSaleItemCommandHandler(
    ISaleRepository saleRepository,
    IProductRepository productRepository,
    IMeasurementUnitRepository measurementUnitRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddProductSaleItemCommand, AddProductSaleItemResult?>
{
    public async Task<AddProductSaleItemResult?> Handle(
        AddProductSaleItemCommand request,
        CancellationToken cancellationToken)
    {
        var sale = await saleRepository.GetDraftForUpdateAsync(
            request.SaleId,
            cancellationToken);
        if (sale is null)
        {
            return null;
        }

        var product = await productRepository.GetActiveByIdAsync(
            request.ProductId,
            cancellationToken);
        if (product is null)
        {
            return null;
        }

        var measurementUnit = await measurementUnitRepository.GetByIdAsync(
            product.MeasurementUnitId,
            cancellationToken);
        if (measurementUnit is null)
        {
            return null;
        }

        var addition = sale.AddProductItem(
            product.Id,
            product.Code,
            product.Barcode,
            product.Name,
            measurementUnit.Id,
            measurementUnit.Name,
            quantity: 1m,
            product.Price);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CreateResult(sale, addition);
    }

    internal static AddProductSaleItemResult CreateResult(
        Sale sale,
        CatalogSaleItemAddition addition)
    {
        var item = addition.Item;
        return new AddProductSaleItemResult(
            sale.Id,
            item.Id,
            item.ProductId!.Value,
            item.ProductCode!,
            item.Barcode,
            item.ProductName,
            item.MeasurementUnitId!.Value,
            item.MeasurementUnitName!,
            item.Quantity,
            item.UnitPrice,
            item.LineTotal,
            item.IsManual,
            sale.TotalAmount,
            addition.WasNewItem,
            item.Comment);
    }
}
