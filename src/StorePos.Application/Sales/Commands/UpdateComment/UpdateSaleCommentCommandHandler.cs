using MediatR;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Sales.Commands.UpdateComment;

public sealed class UpdateSaleCommentCommandHandler(
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateSaleCommentCommand, UpdateSaleCommentResult?>
{
    public async Task<UpdateSaleCommentResult?> Handle(
        UpdateSaleCommentCommand request,
        CancellationToken cancellationToken)
    {
        var sale = await saleRepository.GetDraftForMetadataUpdateAsync(
            request.SaleId,
            cancellationToken);
        if (sale is null)
        {
            return null;
        }

        sale.UpdateComment(request.Comment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateSaleCommentResult(sale.Id, sale.Comment);
    }
}
