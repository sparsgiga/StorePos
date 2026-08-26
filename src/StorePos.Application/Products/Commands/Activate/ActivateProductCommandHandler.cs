using MediatR;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Products.Commands.Activate;

public sealed class ActivateProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<ActivateProductCommand, ProductCommandResult?>
{
    public async Task<ProductCommandResult?> Handle(
        ActivateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.Activate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return product.ToResult();
    }
}
