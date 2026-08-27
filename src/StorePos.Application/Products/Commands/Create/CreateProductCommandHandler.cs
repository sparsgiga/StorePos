using MediatR;
using StorePos.Application.Common.Exceptions;
using StorePos.Application.Common.Interfaces;
using StorePos.Domain.Aggregates.MeasurementUnit;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Products.Commands.Create;

public sealed class CreateProductCommandHandler(
    IProductRepository productRepository,
    IMeasurementUnitRepository measurementUnitRepository,
    IManualProductCodeSequenceService codeSequenceService,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateProductCommand, ProductCommandResult>
{
    public async Task<ProductCommandResult> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.Trim();
        var barcode = string.IsNullOrWhiteSpace(request.Barcode)
            ? null
            : request.Barcode.Trim();
        if (await productRepository.CodeExistsAsync(code, cancellationToken: cancellationToken))
        {
            throw new ProductCodeConflictException(code);
        }

        if (barcode is not null &&
            await productRepository.BarcodeExistsAsync(
                barcode,
                cancellationToken: cancellationToken))
        {
            throw new ProductBarcodeConflictException(barcode);
        }

        if (await measurementUnitRepository.GetActiveByIdAsync(
                request.MeasurementUnitId,
                cancellationToken) is null)
        {
            throw new ProductMeasurementUnitNotAvailableException(request.MeasurementUnitId);
        }

        var product = Product.Create(
            code,
            barcode,
            request.Name,
            request.MeasurementUnitId,
            request.Price,
            request.SupplierName,
            request.SupplierCode,
            request.CostPrice);
        await productRepository.AddAsync(product, cancellationToken);
        await codeSequenceService.AdvanceIfConsumedAsync(code, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return product.ToResult();
    }
}
