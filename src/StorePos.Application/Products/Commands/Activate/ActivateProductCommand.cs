using MediatR;

namespace StorePos.Application.Products.Commands.Activate;

public sealed record ActivateProductCommand(long ProductId)
    : IRequest<ProductCommandResult?>;
