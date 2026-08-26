using MediatR;

namespace StorePos.Application.Products.Commands.Update;

public sealed record UpdateProductCommand(
    long ProductId,
    string Code,
    string Barcode,
    string Name,
    int MeasurementUnitId,
    decimal Price) : IRequest<ProductCommandResult?>;
