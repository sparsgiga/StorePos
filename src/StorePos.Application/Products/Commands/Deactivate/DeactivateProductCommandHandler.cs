using MediatR;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Products.Commands.Deactivate;

public sealed class DeactivateProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeactivateProductCommand, ProductCommandResult?>
{
    public async Task<ProductCommandResult?> Handle(
        DeactivateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return product.ToResult();
    }
}
