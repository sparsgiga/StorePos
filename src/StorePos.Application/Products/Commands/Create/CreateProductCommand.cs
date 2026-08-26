using MediatR;

namespace StorePos.Application.Products.Commands.Create;

public sealed record CreateProductCommand(
    string Code,
    string Barcode,
    string Name,
    int MeasurementUnitId,
    decimal Price) : IRequest<ProductCommandResult>;
