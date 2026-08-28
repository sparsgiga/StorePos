using MediatR;
using StorePos.Application.Common.Exceptions;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Common;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Sales.Commands.UpdateFinancials;

public sealed class UpdateSaleItemFinancialsCommandHandler(
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateSaleItemFinancialsCommand, UpdateSaleItemFinancialsResult?>
{
    public async Task<UpdateSaleItemFinancialsResult?> Handle(
        UpdateSaleItemFinancialsCommand request,
        CancellationToken cancellationToken)
    {
        var sale = await saleRepository.GetDraftForUpdateAsync(
            request.SaleId,
            cancellationToken);
        if (sale is null)
        {
            return null;
        }

        SaleItem item;
        try
        {
            item = request switch
            {
                { Quantity: not null } => sale.UpdateItemQuantity(
                    request.SaleItemId, request.Quantity.Value),
                { UnitPrice: not null } => sale.UpdateItemUnitPrice(
                    request.SaleItemId, request.UnitPrice.Value),
                { LineTotal: not null } => sale.UpdateItemLineTotal(
                    request.SaleItemId, request.LineTotal.Value),
                _ => throw new ArgumentException("A financial value is required.")
            };
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
        catch (InvalidOperationException exception)
        {
            throw new SaleOperationConflictException(
                "ფასდაკლება ვერ იქნება პროდუქტების ჯამზე მეტი.",
                exception);
        }
        catch (ArgumentException exception)
        {
            throw new SaleOperationConflictException(
                "შეყვანილი ფინანსური მნიშვნელობა არასწორია ან მხარდაჭერილ სიზუსტეში ვერ გამოისახება.",
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

        var requestedAdjusted = request.LineTotal.HasValue &&
            item.LineTotal != FinancialPrecision.RoundMoney(request.LineTotal.Value);
        return new UpdateSaleItemFinancialsResult(
            sale.Id,
            item.Id,
            item.Quantity,
            item.UnitPrice,
            item.LineTotal,
            sale.Subtotal,
            sale.DiscountAmount,
            sale.TotalAmount,
            requestedAdjusted);
    }
}
