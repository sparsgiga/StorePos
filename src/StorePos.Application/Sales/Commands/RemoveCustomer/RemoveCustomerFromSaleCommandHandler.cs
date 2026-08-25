using MediatR;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Sales.Commands.RemoveCustomer;

public sealed class RemoveCustomerFromSaleCommandHandler(
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveCustomerFromSaleCommand, RemoveCustomerFromSaleResult?>
{
    public async Task<RemoveCustomerFromSaleResult?> Handle(
        RemoveCustomerFromSaleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.SaleId);

        var sale = await saleRepository.GetDraftForMetadataUpdateAsync(
            request.SaleId,
            cancellationToken);
        if (sale is null)
        {
            return null;
        }

        sale.RemoveCustomer();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RemoveCustomerFromSaleResult(
            sale.Id,
            sale.CustomerId,
            sale.CustomerName,
            sale.CustomerIdentificationNumber,
            sale.Comment);
    }
}
