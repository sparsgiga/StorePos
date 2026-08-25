using MediatR;
using StorePos.Application.Common.Behaviors;
using StorePos.Application.Sales.Commands.AddProductItem;

namespace StorePos.Application.Products.Commands.CreateAndAddToSale;

public sealed record CreateProductAndAddToSaleCommand(
    long SaleId,
    string ProductCode,
    string Name,
    string? Barcode,
    int MeasurementUnitId,
    decimal Quantity,
    decimal UnitPrice,
    string? Comment = null)
    : IRequest<AddProductSaleItemResult?>, ITransactionalRequest;
