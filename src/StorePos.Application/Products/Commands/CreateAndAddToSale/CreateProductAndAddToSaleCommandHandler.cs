using MediatR;
using StorePos.Application.Common.Exceptions;
using StorePos.Application.Common.Interfaces;
using StorePos.Application.Sales.Commands.AddProductItem;
using StorePos.Domain.Aggregates.MeasurementUnit;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Products.Commands.CreateAndAddToSale;

public sealed class CreateProductAndAddToSaleCommandHandler(
    ISaleRepository saleRepository,
    IProductRepository productRepository,
    IMeasurementUnitRepository measurementUnitRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductAndAddToSaleCommand, AddProductSaleItemResult?>
{
    public async Task<AddProductSaleItemResult?> Handle(
        CreateProductAndAddToSaleCommand request,
        CancellationToken cancellationToken)
    {
        var sale = await saleRepository.GetDraftForUpdateAsync(
            request.SaleId,
            cancellationToken);
        if (sale is null)
        {
            return null;
        }

        var measurementUnit = await measurementUnitRepository.GetActiveByIdAsync(
            request.MeasurementUnitId,
            cancellationToken);
        if (measurementUnit is null)
        {
            return null;
        }

        var barcode = string.IsNullOrWhiteSpace(request.Barcode)
            ? null
            : request.Barcode.Trim();
        var productCode = request.ProductCode.Trim();

        if (await productRepository.GetByCodeAsync(productCode, cancellationToken) is not null)
        {
            throw new ProductCodeConflictException(productCode);
        }

        if (barcode is not null &&
            await productRepository.GetByBarcodeAsync(barcode, cancellationToken) is not null)
        {
            throw new ProductBarcodeConflictException(barcode);
        }

        var product = Product.Create(
            productCode,
            barcode,
            request.Name,
            measurementUnit.Id,
            request.UnitPrice);

        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var addition = sale.AddProductItem(
            product.Id,
            product.Code,
            product.Barcode,
            product.Name,
            measurementUnit.Id,
            measurementUnit.Name,
            request.Quantity,
            product.Price,
            request.Comment);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return AddProductSaleItemCommandHandler.CreateResult(sale, addition);
    }
}
