using MediatR;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Products.Commands.UpdateRetailPrice;

public sealed class UpdateProductRetailPriceCommandHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductRetailPriceCommand, UpdateProductRetailPriceResult?>
{
    public async Task<UpdateProductRetailPriceResult?> Handle(
        UpdateProductRetailPriceCommand request,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetActiveByIdAsync(
            request.ProductId,
            cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.UpdateRetailPrice(request.Price);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateProductRetailPriceResult(product.Id, product.Price);
    }
}
