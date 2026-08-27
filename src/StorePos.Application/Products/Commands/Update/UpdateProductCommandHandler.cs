using MediatR;
using StorePos.Application.Common.Exceptions;
using StorePos.Domain.Aggregates.MeasurementUnit;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Products.Commands.Update;

public sealed class UpdateProductCommandHandler(
    IProductRepository productRepository,
    IMeasurementUnitRepository measurementUnitRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateProductCommand, ProductCommandResult?>
{
    public async Task<ProductCommandResult?> Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var code = request.Code.Trim();
        var barcode = string.IsNullOrWhiteSpace(request.Barcode)
            ? null
            : request.Barcode.Trim();
        if (await productRepository.CodeExistsAsync(
                code,
                product.Id,
                cancellationToken))
        {
            throw new ProductCodeConflictException(code);
        }

        if (barcode is not null &&
            await productRepository.BarcodeExistsAsync(
                barcode,
                product.Id,
                cancellationToken))
        {
            throw new ProductBarcodeConflictException(barcode);
        }

        if (await measurementUnitRepository.GetActiveByIdAsync(
                request.MeasurementUnitId,
                cancellationToken) is null)
        {
            throw new ProductMeasurementUnitNotAvailableException(request.MeasurementUnitId);
        }

        product.UpdateDetails(
            code,
            barcode,
            request.Name,
            request.MeasurementUnitId,
            request.Price,
            request.SupplierName,
            request.SupplierCode,
            request.CostPrice);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return product.ToResult();
    }
}
