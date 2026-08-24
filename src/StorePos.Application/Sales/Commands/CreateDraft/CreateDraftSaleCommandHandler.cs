using MediatR;
using StorePos.Application.Common.Interfaces;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Sales.Commands.CreateDraft;

public sealed class CreateDraftSaleCommandHandler(
    ISaleNumberGenerator saleNumberGenerator,
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateDraftSaleCommand, CreateDraftSaleResult>
{
    public async Task<CreateDraftSaleResult> Handle(
        CreateDraftSaleCommand request,
        CancellationToken cancellationToken)
    {
        var saleNumber = await saleNumberGenerator.GenerateAsync(cancellationToken);

        var sale = Sale.Create(
            saleNumber,
            request.CashierId,
            request.CustomerName,
            request.CustomerIdentificationNumber,
            request.Comment);

        await saleRepository.AddAsync(sale, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateDraftSaleResult(
            sale.Id,
            sale.SaleNumber,
            sale.TotalAmount,
            sale.DateCreated,
            sale.CustomerName);
    }
}
