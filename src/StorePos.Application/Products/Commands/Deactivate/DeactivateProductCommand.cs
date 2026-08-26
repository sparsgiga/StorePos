using MediatR;

namespace StorePos.Application.Products.Commands.Deactivate;

public sealed record DeactivateProductCommand(long ProductId)
    : IRequest<ProductCommandResult?>;
