using MediatR;

namespace StorePos.Application.Products.Commands.UpdateRetailPrice;

public sealed record UpdateProductRetailPriceCommand(long ProductId, decimal Price)
    : IRequest<UpdateProductRetailPriceResult?>;
