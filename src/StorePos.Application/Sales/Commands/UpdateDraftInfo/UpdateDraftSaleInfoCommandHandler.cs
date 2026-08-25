using MediatR;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Sales.Commands.UpdateDraftInfo;

public sealed class UpdateDraftSaleInfoCommandHandler(
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateDraftSaleInfoCommand, UpdateDraftSaleInfoResult?>
{
    public async Task<UpdateDraftSaleInfoResult?> Handle(
        UpdateDraftSaleInfoCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.SaleId);

        var sale = await saleRepository.GetDraftForInfoUpdateAsync(
            request.SaleId,
            cancellationToken);

        if (sale is null)
        {
            return null;
        }

        sale.UpdateInfo(
            request.CustomerName,
            request.CustomerIdentificationNumber,
            request.Comment);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateDraftSaleInfoResult(
            sale.Id,
            sale.CustomerName,
            sale.CustomerIdentificationNumber,
            sale.Comment);
    }
}
