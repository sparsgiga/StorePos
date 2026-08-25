using MediatR;
using StorePos.Domain.Aggregates.Customer;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Sales.Commands.AssignCustomer;

public sealed class AssignCustomerToSaleCommandHandler(
    ISaleRepository saleRepository,
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AssignCustomerToSaleCommand, AssignCustomerToSaleResult?>
{
    public async Task<AssignCustomerToSaleResult?> Handle(
        AssignCustomerToSaleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.SaleId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.CustomerId);

        var sale = await saleRepository.GetDraftForMetadataUpdateAsync(
            request.SaleId,
            cancellationToken);
        if (sale is null)
        {
            return null;
        }

        var customer = await customerRepository.GetByIdAsync(
            request.CustomerId,
            cancellationToken);
        if (customer is null)
        {
            return null;
        }

        sale.AssignCustomer(
            customer.Id,
            customer.Name,
            customer.IdentificationNumber);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AssignCustomerToSaleResult(
            sale.Id,
            customer.Id,
            sale.CustomerName!,
            sale.CustomerIdentificationNumber,
            sale.Comment);
    }
}
