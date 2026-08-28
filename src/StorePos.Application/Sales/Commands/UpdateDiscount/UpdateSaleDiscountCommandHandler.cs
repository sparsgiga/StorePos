using MediatR;
using StorePos.Application.Common.Exceptions;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Sales.Commands.UpdateDiscount;

public sealed class UpdateSaleDiscountCommandHandler(
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateSaleDiscountCommand, UpdateSaleDiscountResult?>
{
    public async Task<UpdateSaleDiscountResult?> Handle(
        UpdateSaleDiscountCommand request,
        CancellationToken cancellationToken)
    {
        var sale = await saleRepository.GetDraftForUpdateAsync(
            request.SaleId,
            cancellationToken);
        if (sale is null)
        {
            return null;
        }

        try
        {
            sale.UpdateDiscount(request.DiscountAmount);
        }
        catch (InvalidOperationException exception)
        {
            throw new SaleOperationConflictException(
                "ფასდაკლება ვერ იქნება პროდუქტების ჯამზე მეტი.",
                exception);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConcurrencyException exception)
        {
            throw new SaleOperationConflictException(
                "გაყიდვის მდგომარეობა შეიცვალა. განაახლეთ მონაცემები და სცადეთ თავიდან.",
                exception);
        }

        return new UpdateSaleDiscountResult(
            sale.Id,
            sale.Subtotal,
            sale.DiscountAmount,
            sale.TotalAmount);
    }
}
